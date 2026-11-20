using System.Collections;
using UnityEngine;

namespace LastLight
{
    public enum ContainerKind
    {
        Drawer,
        Cupboard
    }

    public enum ContainerRewardType
    {
        Empty,
        OilFlask,
        PuzzlePiece,
        StoryNote
    }

    /// <summary>
    /// Interactive 3D cupboard, wardrobe, 3-drawer chest, nightstand, writing desk, or apothecary chest:
    /// - Smoothly opens on tap, stays open for 2.5 seconds (or closes immediately if player taps CLOSE),
    ///   then automatically animates smoothly closing back shut.
    /// - Supports Cursed Drawer traps (2 random non-essential drawers per floor): a red-eyed spider leaps out
    ///   and drains 10% oil unless the player taps CLOSE before the 2.5s auto-close finishes!
    /// - Supports Blue Spirit Flame visual indicators: cyan glow mark on uncollected puzzle containers,
    ///   faint red warning mark on cursed/empty containers (strictly unlit, zero scene light emitted).
    /// </summary>
    public class InteractableContainer : MonoBehaviour, IInteractable
    {
        [Header("Container Configuration")]
        public ContainerKind containerKind = ContainerKind.Drawer;
        public string containerDisplayName = "DRAWER";
        public ContainerRewardType rewardType = ContainerRewardType.OilFlask;
        public float oilRestoreAmount = 20f;
        public int puzzlePieceNumber = 1;

        [Header("Story Note Content (Dynamically Updated for Randomized Puzzles)")]
        public string noteTitle = "Torn Page";
        [TextArea(3, 6)]
        public string noteBody = "The mansion shifts in the dark...";

        [Header("Visual Parts (Auto-Wired)")]
        public FloorController ownerFloor;
        public Transform movingPart;
        public GameObject rewardVisual;
        public GameObject cursedSpiderVisual;
        public GameObject spiritCyanMarkVisual;
        public GameObject spiritRedMarkVisual;

        [Header("Open / Auto-Close Animation & Cursed Trap")]
        public Vector3 drawerSlideOffset = new Vector3(0f, 0f, 0.54f);
        public float cupboardOpenAngle = -105f;
        public float openDuration = 0.40f;
        public float autoCloseDelay = 2.5f;
        public bool isCursed;
        public float cursedOilDrain = 10f;

        private Vector3 closedLocalPos;
        private Quaternion closedLocalRot;
        private Vector3 spiderBaseLocalPos;
        private bool cachedClosedTransform;
        private bool isOpen;
        private bool hasGrantedReward;
        private bool cursedTrapResolved;
        private bool cursedAttackInProgress;
        private bool isAnimating;
        private Coroutine animRoutine;
        private Coroutine autoCloseRoutine;

        public bool IsOpen => isOpen;
        public bool HasGrantedReward => hasGrantedReward;
        public bool IsCursedActive => isCursed && !cursedTrapResolved;

        public bool CanInteract => !isAnimating;

        public string InteractionPrompt
        {
            get
            {
                string label = string.IsNullOrEmpty(containerDisplayName)
                    ? (containerKind == ContainerKind.Drawer ? "DRAWER" : "CUPBOARD")
                    : containerDisplayName;

                if (cursedAttackInProgress && isOpen)
                {
                    return $"CLOSE {label}!";
                }
                return isOpen ? $"CLOSE {label}" : $"OPEN {label}";
            }
        }

        public Vector3 InteractionWorldPosition => transform.position + Vector3.up * 0.9f;

        private void Awake()
        {
            CacheInitialTransform();
            if (cursedSpiderVisual != null) cursedSpiderVisual.SetActive(false);
            if (spiritCyanMarkVisual != null) spiritCyanMarkVisual.SetActive(false);
            if (spiritRedMarkVisual != null) spiritRedMarkVisual.SetActive(false);
        }

        private void CacheInitialTransform()
        {
            if (cachedClosedTransform) return;
            if (movingPart != null)
            {
                closedLocalPos = movingPart.localPosition;
                closedLocalRot = movingPart.localRotation;
            }
            if (cursedSpiderVisual != null)
            {
                spiderBaseLocalPos = cursedSpiderVisual.transform.localPosition;
            }
            cachedClosedTransform = true;
        }

        public void SetCursedState(bool cursed)
        {
            isCursed = cursed;
            cursedTrapResolved = false;
            cursedAttackInProgress = false;
            if (cursedSpiderVisual != null)
            {
                cursedSpiderVisual.SetActive(false);
                if (cachedClosedTransform) cursedSpiderVisual.transform.localPosition = spiderBaseLocalPos;
            }
        }

        public void UpdateSpiritIndicators(bool spiritVisionActive)
        {
            if (spiritCyanMarkVisual != null)
            {
                bool isPuzzleContainer = !isCursed && !hasGrantedReward &&
                    (rewardType == ContainerRewardType.PuzzlePiece || rewardType == ContainerRewardType.StoryNote);
                spiritCyanMarkVisual.SetActive(spiritVisionActive && isPuzzleContainer);
            }

            if (spiritRedMarkVisual != null)
            {
                bool isTrapOrEmpty = isCursed || rewardType == ContainerRewardType.Empty;
                spiritRedMarkVisual.SetActive(spiritVisionActive && isTrapOrEmpty);
            }
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract) return;

            // Manual CLOSE before auto-close finishes!
            if (isOpen)
            {
                CancelAutoCloseTimer();

                if (cursedAttackInProgress)
                {
                    cursedAttackInProgress = false;
                    cursedTrapResolved = true;
                    hasGrantedReward = true;
                    if (cursedSpiderVisual != null) cursedSpiderVisual.SetActive(false);
                    MansionThreatManager.Instance?.OnCursedDrawerClosedInTime(this);
                }

                StartAnimateOpenClose(player, opening: false);
                return;
            }

            StartAnimateOpenClose(player, opening: true);
        }

        private void StartAnimateOpenClose(PlayerController player, bool opening)
        {
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(ToggleOpenCloseRoutine(player, opening));
        }

        private void CancelAutoCloseTimer()
        {
            if (autoCloseRoutine != null)
            {
                StopCoroutine(autoCloseRoutine);
                autoCloseRoutine = null;
            }
        }

        private IEnumerator ToggleOpenCloseRoutine(PlayerController player, bool opening)
        {
            CacheInitialTransform();
            isAnimating = true;

            if (opening) AudioManager.Instance?.PlayDrawerOpen();
            else AudioManager.Instance?.PlayDrawerClose();

            bool triggerCurseNow = opening && isCursed && !cursedTrapResolved;

            if (opening && !triggerCurseNow && !hasGrantedReward && rewardVisual != null && rewardType != ContainerRewardType.Empty)
            {
                rewardVisual.SetActive(true);
            }

            if (movingPart != null)
            {
                Vector3 startPos = movingPart.localPosition;
                Quaternion startRot = movingPart.localRotation;

                Vector3 targetPos = opening && containerKind == ContainerKind.Drawer
                    ? closedLocalPos + drawerSlideOffset
                    : closedLocalPos;
                Quaternion targetRot = opening && containerKind == ContainerKind.Cupboard
                    ? closedLocalRot * Quaternion.Euler(0f, cupboardOpenAngle, 0f)
                    : closedLocalRot;

                float elapsed = 0f;
                while (elapsed < openDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, openDuration));
                    float ease = 1f - Mathf.Pow(1f - t, 3f);
                    movingPart.localPosition = Vector3.Lerp(startPos, targetPos, ease);
                    movingPart.localRotation = Quaternion.Slerp(startRot, targetRot, ease);
                    yield return null;
                }

                movingPart.localPosition = targetPos;
                movingPart.localRotation = targetRot;
            }

            isOpen = opening;
            isAnimating = false;
            animRoutine = null;

            if (opening)
            {
                if (triggerCurseNow)
                {
                    cursedAttackInProgress = true;
                    if (cursedSpiderVisual != null)
                    {
                        cursedSpiderVisual.SetActive(true);
                        cursedSpiderVisual.transform.localPosition = spiderBaseLocalPos;
                    }
                    MansionThreatManager.Instance?.OnCursedDrawerOpened(this);
                }
                else if (!hasGrantedReward)
                {
                    hasGrantedReward = true;
                    GrantReward(player);
                    MansionThreatManager.Instance?.RefreshSpiritCluesForActiveFloor();
                }

                // Keep open for 2.5 seconds (pauses if a Story Note modal is open), then auto-close smoothly!
                CancelAutoCloseTimer();
                autoCloseRoutine = StartCoroutine(AutoCloseCountdownRoutine(player));
            }
            else
            {
                if (cursedSpiderVisual != null) cursedSpiderVisual.SetActive(false);
            }
        }

        private IEnumerator AutoCloseCountdownRoutine(PlayerController player)
        {
            float elapsed = 0f;
            while (elapsed < autoCloseDelay)
            {
                // Pause auto-close countdown while player is reading a Story Note / modal
                GameManager gm = GameManager.Instance;
                if (gm == null || gm.CurrentState == GameState.Playing)
                {
                    elapsed += Time.deltaTime;

                    // Animate leaping cursed spider lunging forward out of the drawer during the 2.5s window
                    if (cursedAttackInProgress && cursedSpiderVisual != null && cursedSpiderVisual.activeSelf)
                    {
                        float p = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, autoCloseDelay));
                        float lungeZ = Mathf.Lerp(0.05f, 0.72f, p);
                        float hopY = Mathf.Abs(Mathf.Sin(p * Mathf.PI * 3.5f)) * 0.24f + p * 0.16f;
                        cursedSpiderVisual.transform.localPosition = spiderBaseLocalPos + new Vector3(0f, hopY, lungeZ);
                    }
                }
                yield return null;
            }

            autoCloseRoutine = null;

            // If player failed to manually tap CLOSE on a cursed drawer before 2.5s elapsed -> Spider strikes!
            if (cursedAttackInProgress)
            {
                cursedAttackInProgress = false;
                cursedTrapResolved = true;
                hasGrantedReward = true;
                if (cursedSpiderVisual != null) cursedSpiderVisual.SetActive(false);
                MansionThreatManager.Instance?.OnCursedSpiderBite(cursedOilDrain);
            }

            if (isOpen && !isAnimating)
            {
                StartAnimateOpenClose(player, opening: false);
            }
        }

        private void GrantReward(PlayerController player)
        {
            GameManager gm = GameManager.Instance;
            LampController lamp = player != null ? player.lampController : null;

            switch (rewardType)
            {
                case ContainerRewardType.OilFlask:
                    if (lamp != null) lamp.AddOil(oilRestoreAmount);
                    if (rewardVisual != null) rewardVisual.SetActive(false);
                    AudioManager.Instance?.PlayOilPickup();
                    gm?.NotifyPickup($"OIL FLASK FOUND!  +{Mathf.RoundToInt(oilRestoreAmount)}% OIL (MAX 100%)", new Color(0.80f, 0.96f, 0.56f, 1f));
                    break;

                case ContainerRewardType.PuzzlePiece:
                    if (rewardVisual != null) rewardVisual.SetActive(false);
                    AudioManager.Instance?.PlayItemPickup();
                    ownerFloor?.OnPuzzlePieceCollected(puzzlePieceNumber);
                    break;

                case ContainerRewardType.StoryNote:
                    if (rewardVisual != null) rewardVisual.SetActive(false);
                    AudioManager.Instance?.PlayNoteOpen();
                    ownerFloor?.OnStoryNoteDiscovered(noteTitle);
                    gm?.OpenStoryNote(noteTitle, noteBody, saveToInventory: true);
                    break;

                case ContainerRewardType.Empty:
                    gm?.NotifyPickup("EMPTY... ONLY DUST AND COBWEBS.", new Color(0.75f, 0.72f, 0.68f, 1f));
                    break;
            }
        }

        public void ResetState()
        {
            CacheInitialTransform();
            CancelAutoCloseTimer();
            if (animRoutine != null)
            {
                StopCoroutine(animRoutine);
                animRoutine = null;
            }

            isOpen = false;
            hasGrantedReward = false;
            cursedTrapResolved = false;
            cursedAttackInProgress = false;
            isAnimating = false;

            if (movingPart != null)
            {
                movingPart.localPosition = closedLocalPos;
                movingPart.localRotation = closedLocalRot;
            }
            if (rewardVisual != null)
            {
                rewardVisual.SetActive(false);
            }
            if (cursedSpiderVisual != null)
            {
                cursedSpiderVisual.SetActive(false);
                cursedSpiderVisual.transform.localPosition = spiderBaseLocalPos;
            }
            if (spiritCyanMarkVisual != null) spiritCyanMarkVisual.SetActive(false);
            if (spiritRedMarkVisual != null) spiritRedMarkVisual.SetActive(false);
        }
    }
}
