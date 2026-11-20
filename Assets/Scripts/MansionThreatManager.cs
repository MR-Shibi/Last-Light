using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastLight
{
    /// <summary>
    /// Attached to glowing cyan Spirit Flame wall clues/arrows (unlit cyan, zero scene light emitted).
    /// Revealed ONLY when the player activates Blue Spirit Flame mode while the lamp is ON.
    /// Automatically hides once its linked puzzle container has been collected.
    /// </summary>
    public class SpiritClueMarker : MonoBehaviour
    {
        public InteractableContainer linkedContainer;
        public bool pulseScale = true;

        private Vector3 baseScale;
        private bool cachedScale;

        private void Awake()
        {
            baseScale = transform.localScale;
            cachedScale = true;
            gameObject.SetActive(false);
        }

        public void SetSpiritVisible(bool spiritActive)
        {
            bool shouldShow = spiritActive && (linkedContainer == null || !linkedContainer.HasGrantedReward);
            if (gameObject.activeSelf != shouldShow)
            {
                gameObject.SetActive(shouldShow);
            }
        }

        private void Update()
        {
            if (!pulseScale || !cachedScale) return;
            float s = 1f + Mathf.Sin(Time.time * 4.5f) * 0.08f;
            transform.localScale = baseScale * s;
        }
    }

    /// <summary>
    /// High-Tension Horror Threat & Spirit Vision Coordinator for "Last Light":
    /// 1. Bandit Patrol ("Hide in the Dark"):
    ///    - Every 35 seconds during gameplay, triggers a 5-second "Bandit Nearby" event with approaching
    ///      heavy footsteps SFX, pulsing red warning vignette, and "EXTINGUISH LAMP! HE SEES THE LIGHT!".
    ///    - Turning the lamp OFF before 5s finishes keeps the player safely hidden.
    ///    - Leaving the lamp ON when 5s ends triggers a sharp stinger, camera shake, and -15% oil.
    /// 2. Cursed Drawers:
    ///    - Marks 2 random non-essential drawers per floor as Cursed.
    ///    - Opening a cursed drawer unleashes a leaping red-eyed spider that drains 10% oil unless
    ///      the player manually taps CLOSE before the drawer auto-closes (2.5s).
    /// 3. Blue Spirit Flame Clue Reveal:
    ///    - Reveals hidden glowing cyan wall clues/arrows pointing to uncollected puzzle containers,
    ///      and reveals faint red warning marks on cursed/empty drawers.
    /// </summary>
    public class MansionThreatManager : MonoBehaviour
    {
        public static MansionThreatManager Instance { get; private set; }

        [Header("Core References")]
        public LampController lampController;
        public CameraController cameraController;
        public UIManager uiManager;

        [Header("Bandit Patrol Settings")]
        public float patrolInterval = 35f;
        public float warningDuration = 5f;
        public float banditSpottedOilPenalty = 15f;
        public int cursedDrawersPerFloor = 2;

        [Header("Bandit Warning UI (Auto-Wired)")]
        public CanvasGroup banditWarningVignetteGroup;
        public Text banditWarningText;

        private float patrolCountdown;
        private bool isBanditWarningActive;
        private float warningTimeRemaining;
        private float footstepTimer;
        private bool spiritVisionCurrentlyActive;

        public bool IsBanditWarningActive => isBanditWarningActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ResetPatrolState();
        }

        private void OnEnable()
        {
            if (lampController != null)
            {
                lampController.OnSpiritVisionChanged -= HandleSpiritVisionChanged;
                lampController.OnSpiritVisionChanged += HandleSpiritVisionChanged;
            }
        }

        private void OnDisable()
        {
            if (lampController != null)
            {
                lampController.OnSpiritVisionChanged -= HandleSpiritVisionChanged;
            }
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameState.Playing)
            {
                if (isBanditWarningActive && gm != null &&
                    (gm.CurrentState == GameState.GameOver || gm.CurrentState == GameState.Victory || gm.CurrentState == GameState.StartScreen))
                {
                    ResetPatrolState();
                }
                else if (banditWarningVignetteGroup != null && gm != null && gm.CurrentState != GameState.Playing)
                {
                    // Pause pulsing cleanly while in modals/options
                    banditWarningVignetteGroup.alpha = 0f;
                }
                return;
            }

            if (!isBanditWarningActive)
            {
                patrolCountdown -= Time.deltaTime;
                if (patrolCountdown <= 0f)
                {
                    BeginBanditPatrolEvent();
                }
            }
            else
            {
                UpdateBanditPatrolEvent(Time.deltaTime);
            }
        }

        public void ResetPatrolState()
        {
            patrolCountdown = patrolInterval;
            isBanditWarningActive = false;
            warningTimeRemaining = 0f;
            footstepTimer = 0f;

            if (banditWarningVignetteGroup != null)
            {
                banditWarningVignetteGroup.alpha = 0f;
                banditWarningVignetteGroup.blocksRaycasts = false;
            }
            if (banditWarningText != null)
            {
                banditWarningText.text = string.Empty;
            }
        }

        private void BeginBanditPatrolEvent()
        {
            isBanditWarningActive = true;
            warningTimeRemaining = warningDuration;
            footstepTimer = 0.05f;
            AudioManager.Instance?.PlayHeavyFootstep(0.50f);
        }

        private void UpdateBanditPatrolEvent(float dt)
        {
            warningTimeRemaining -= dt;
            float progress = 1f - Mathf.Clamp01(warningTimeRemaining / Mathf.Max(0.1f, warningDuration));
            bool lampIsOff = lampController != null && lampController.IsExtinguished;

            // Approaching heavy footsteps accelerate and grow louder across the 5-second patrol window
            footstepTimer -= dt;
            if (footstepTimer <= 0f)
            {
                float vol = Mathf.Lerp(0.45f, 1.0f, progress);
                AudioManager.Instance?.PlayHeavyFootstep(vol);
                footstepTimer = Mathf.Lerp(0.68f, 0.34f, progress);
            }

            // Pulse red warning vignette & urgent prompt text
            if (banditWarningVignetteGroup != null)
            {
                float pulseSpeed = lampIsOff ? 4.0f : 11.0f;
                float minAlpha = lampIsOff ? 0.14f : 0.38f;
                float maxAlpha = lampIsOff ? 0.30f : 0.88f;
                banditWarningVignetteGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
            }

            if (banditWarningText != null)
            {
                if (lampIsOff)
                {
                    banditWarningText.color = new Color(0.66f, 0.92f, 0.72f, 1f);
                    banditWarningText.text = $"HIDDEN IN THE DARK... STAY QUIET ({Mathf.Max(0f, warningTimeRemaining):0.0}s)";
                }
                else
                {
                    banditWarningText.color = new Color(1f, 0.24f, 0.20f, 1f);
                    banditWarningText.text = $"EXTINGUISH LAMP! HE SEES THE LIGHT! ({Mathf.Max(0f, warningTimeRemaining):0.0}s)";
                }
            }

            if (warningTimeRemaining <= 0f)
            {
                ResolveBanditPatrolEvent(lampIsOff);
            }
        }

        private void ResolveBanditPatrolEvent(bool lampWasOff)
        {
            isBanditWarningActive = false;
            patrolCountdown = patrolInterval;

            if (banditWarningVignetteGroup != null) banditWarningVignetteGroup.alpha = 0f;
            if (banditWarningText != null) banditWarningText.text = string.Empty;

            if (lampWasOff)
            {
                AudioManager.Instance?.PlayPuzzleUnlock();
                uiManager?.ShowNotification("THE BANDIT PASSED BY... YOU STAYED HIDDEN!", new Color(0.62f, 0.95f, 0.70f, 1f), 2.5f);
            }
            else
            {
                AudioManager.Instance?.PlayAmbushStinger();
                cameraController?.Shake(0.68f, 0.46f);
                lampController?.DrainOilImmediate(banditSpottedOilPenalty);
                uiManager?.ShowNotification($"BANDIT SPOTTED YOUR LIGHT!  -{Mathf.RoundToInt(banditSpottedOilPenalty)}% OIL!", new Color(1f, 0.24f, 0.20f, 1f), 3.0f);
            }
        }

        // =========================================================================
        // CURSED DRAWERS (2 RANDOM NON-ESSENTIAL DRAWERS PER FLOOR)
        // =========================================================================

        public void AssignCursedDrawersForFloor(FloorController floor)
        {
            if (floor == null || floor.containers == null) return;

            List<InteractableContainer> candidates = new List<InteractableContainer>();
            for (int i = 0; i < floor.containers.Length; i++)
            {
                InteractableContainer c = floor.containers[i];
                if (c == null) continue;
                c.SetCursedState(false);

                // Non-essential drawers only (never curse PuzzlePiece or StoryNote clue containers!)
                if (c.rewardType == ContainerRewardType.OilFlask || c.rewardType == ContainerRewardType.Empty)
                {
                    candidates.Add(c);
                }
            }

            // Fisher-Yates shuffle
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                InteractableContainer tmp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = tmp;
            }

            int toCurse = Mathf.Min(cursedDrawersPerFloor, candidates.Count);
            for (int i = 0; i < toCurse; i++)
            {
                candidates[i].SetCursedState(true);
            }

            RefreshSpiritCluesForActiveFloor();
        }

        public void OnCursedDrawerOpened(InteractableContainer container)
        {
            AudioManager.Instance?.PlayRatSqueak();
            AudioManager.Instance?.PlayDoorLocked();
            cameraController?.Shake(0.22f, 0.14f);
            uiManager?.ShowNotification("CURSED SPIDER! TAP CLOSE BEFORE IT STRIKES!", new Color(1f, 0.34f, 0.20f, 1f), 2.5f);
        }

        public void OnCursedDrawerClosedInTime(InteractableContainer container)
        {
            AudioManager.Instance?.PlayPuzzleUnlock();
            uiManager?.ShowNotification("SPIDER TRAPPED SHUT! OIL SAVED!", new Color(0.68f, 0.96f, 0.66f, 1f), 2.0f);
            RefreshSpiritCluesForActiveFloor();
        }

        public void OnCursedSpiderBite(float oilDrainAmount)
        {
            AudioManager.Instance?.PlayAmbushStinger();
            cameraController?.Shake(0.52f, 0.38f);
            lampController?.DrainOilImmediate(oilDrainAmount);
            uiManager?.ShowNotification($"CURSED SPIDER BIT YOU!  -{Mathf.RoundToInt(oilDrainAmount)}% OIL!", new Color(1f, 0.22f, 0.18f, 1f), 2.8f);
            RefreshSpiritCluesForActiveFloor();
        }

        // =========================================================================
        // BLUE SPIRIT FLAME REVEAL (CYAN PUZZLE ARROWS + RED CURSED/EMPTY MARKS)
        // =========================================================================

        public void HandleSpiritVisionChanged(bool active)
        {
            spiritVisionCurrentlyActive = active;
            RefreshSpiritCluesForActiveFloor();
        }

        public void RefreshSpiritCluesForActiveFloor()
        {
            FloorController activeFloor = GameManager.Instance != null ? GameManager.Instance.ActiveFloor : null;
            if (activeFloor == null) return;

            bool showSpirit = spiritVisionCurrentlyActive && lampController != null && lampController.IsSpiritVisionActive;

            if (activeFloor.containers != null)
            {
                for (int i = 0; i < activeFloor.containers.Length; i++)
                {
                    if (activeFloor.containers[i] != null)
                    {
                        activeFloor.containers[i].UpdateSpiritIndicators(showSpirit);
                    }
                }
            }

            if (activeFloor.spiritClueMarkers != null)
            {
                for (int i = 0; i < activeFloor.spiritClueMarkers.Length; i++)
                {
                    if (activeFloor.spiritClueMarkers[i] != null)
                    {
                        activeFloor.spiritClueMarkers[i].SetSpiritVisible(showSpirit);
                    }
                }
            }
        }
    }
}
