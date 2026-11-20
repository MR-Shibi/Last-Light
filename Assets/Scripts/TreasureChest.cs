using System.Collections;
using UnityEngine;

namespace LastLight
{
    public enum ChestRewardKind
    {
        FloorExitKey,
        FinalCodeNote
    }

    /// <summary>
    /// Escape-room Treasure Chest locked behind the 4-Piece Tile-Sort Minigame.
    /// Strict lighting: NO extra Point Light and NO emissive glow.
    /// </summary>
    public class TreasureChest : MonoBehaviour, IInteractable
    {
        public FloorController ownerFloor;
        public Transform lidPivot;
        public GameObject innerTreasureVisual;

        public ChestRewardKind rewardKind = ChestRewardKind.FloorExitKey;
        public string puzzleTitle = "RELIC CHEST LOCK";
        public string rewardNoteTitle = "THE FINAL SEAL";
        [TextArea(3, 5)]
        public string rewardNoteBody = "Inside the ancient chest lies a brass tablet:\n\n\"THE FINAL EXIT CODE IS  7 - 1 - 8.\"\n\nEnter it at the North Vault Door to escape the Dark Tower.";

        private bool isSolved;
        private Quaternion closedLidRot;
        private bool cachedLid;
        private Coroutine lidRoutine;

        public bool IsSolved => isSolved;
        public bool CanInteract => !isSolved || rewardKind == ChestRewardKind.FinalCodeNote;

        public string InteractionPrompt
        {
            get
            {
                if (isSolved && rewardKind == ChestRewardKind.FinalCodeNote) return "READ TABLET";
                if (ownerFloor != null && ownerFloor.HasAllPuzzlePieces) return "SOLVE CHEST";
                return "EXAMINE CHEST";
            }
        }

        public Vector3 InteractionWorldPosition => transform.position + Vector3.up * 0.85f;

        private void Awake() => CacheLid();

        private void CacheLid()
        {
            if (cachedLid) return;
            if (lidPivot != null) closedLidRot = lidPivot.localRotation;
            cachedLid = true;
        }

        public void Interact(PlayerController player)
        {
            if (isSolved)
            {
                if (rewardKind == ChestRewardKind.FinalCodeNote)
                {
                    AudioManager.Instance?.PlayNoteOpen();
                    GameManager.Instance?.OpenStoryNote(rewardNoteTitle, rewardNoteBody, saveToInventory: true);
                }
                return;
            }

            if (ownerFloor != null && !ownerFloor.HasAllPuzzlePieces)
            {
                AudioManager.Instance?.PlayDoorLocked();
                GameManager.Instance?.NotifyPickup(
                    $"CHEST LOCKED — FIND ALL {ownerFloor.requiredPuzzlePieces} PUZZLE PIECES ({ownerFloor.CollectedPuzzlePieces}/{ownerFloor.requiredPuzzlePieces})",
                    new Color(1f, 0.65f, 0.35f, 1f)
                );
                return;
            }

            AudioManager.Instance?.PlayPuzzleClick();
            GameManager.Instance?.OpenTileSortPuzzle(puzzleTitle, OnPuzzleSolved);
        }

        private void OnPuzzleSolved()
        {
            isSolved = true;
            AudioManager.Instance?.PlayPuzzleUnlock();
            if (lidRoutine != null) StopCoroutine(lidRoutine);
            lidRoutine = StartCoroutine(OpenLidRoutine());

            if (rewardKind == ChestRewardKind.FloorExitKey)
            {
                ownerFloor?.OnChestExitKeyAwarded();
            }
            else
            {
                ownerFloor?.OnChestCodeTabletAwarded();
                GameManager.Instance?.OpenStoryNote(rewardNoteTitle, rewardNoteBody, saveToInventory: true);
            }
        }

        private IEnumerator OpenLidRoutine()
        {
            CacheLid();
            if (innerTreasureVisual != null) innerTreasureVisual.SetActive(true);

            if (lidPivot != null)
            {
                Quaternion openRot = closedLidRot * Quaternion.Euler(-105f, 0f, 0f);
                float elapsed = 0f;
                while (elapsed < 0.6f)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / 0.6f);
                    float ease = 1f - Mathf.Pow(1f - t, 3f);
                    lidPivot.localRotation = Quaternion.Slerp(closedLidRot, openRot, ease);
                    yield return null;
                }
                lidPivot.localRotation = openRot;
            }
        }

        public void ResetState()
        {
            CacheLid();
            if (lidRoutine != null)
            {
                StopCoroutine(lidRoutine);
                lidRoutine = null;
            }
            isSolved = false;
            if (lidPivot != null) lidPivot.localRotation = closedLidRot;
            if (innerTreasureVisual != null) innerTreasureVisual.SetActive(false);
        }
    }
}
