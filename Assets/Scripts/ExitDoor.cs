using System.Collections;
using UnityEngine;

namespace LastLight
{
    public enum DoorLockType
    {
        KeyLocked,             // Floor 1: Treasure Chest Key
        CodeLocked,            // Floor 2: Randomized 3-Digit Math Riddle Keypad
        SymbolSequenceLocked   // Floor 3: Randomized 4-Step Gothic Symbol Ancient Stone Lock
    }

    /// <summary>
    /// Escape-room Exit Door.
    /// Strict lighting: Lit ONLY by a subtle dim Red Point Light above/at the Exit Door (zero emission).
    /// Swings smoothly open on its hinge when unlocked to reveal a descending 3D staircase.
    /// </summary>
    public class ExitDoor : MonoBehaviour, IInteractable
    {
        [Header("Lock Configuration")]
        public FloorController ownerFloor;
        public DoorLockType lockType = DoorLockType.KeyLocked;
        public string requiredCode = "942";
        public GothicSymbol[] requiredSymbolSequence = { GothicSymbol.Skull, GothicSymbol.Moon, GothicSymbol.Key, GothicSymbol.Crown };
        public string lockModalTitle = "EXIT DOOR LOCK";
        public string lockModalSubtitle = "Solve the clues hidden in this floor";

        [Header("3D Door Swing Parts")]
        public Transform doorHingePivot;
        public Collider doorBlockingCollider;
        public Light redDoorLight;
        public float openSwingAngle = 98f;
        public float swingDuration = 0.85f;

        private bool isUnlocked;
        private Quaternion closedHingeRot;
        private bool cachedHinge;
        private Coroutine swingRoutine;

        public bool IsUnlocked => isUnlocked;
        public bool CanInteract => !isUnlocked;

        public string InteractionPrompt
        {
            get
            {
                if (lockType == DoorLockType.CodeLocked) return "ENTER CODE";
                if (lockType == DoorLockType.SymbolSequenceLocked) return "STONE LOCK";
                if (ownerFloor != null && ownerFloor.HasExitKey) return "UNLOCK DOOR";
                return "DOOR LOCKED";
            }
        }

        public Vector3 InteractionWorldPosition => transform.position + new Vector3(0f, 1.3f, -0.7f);

        private void Awake()
        {
            CacheHinge();
            ConfigureDimRedLight();
        }

        private void CacheHinge()
        {
            if (cachedHinge) return;
            if (doorHingePivot != null) closedHingeRot = doorHingePivot.localRotation;
            cachedHinge = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isUnlocked) return;
            if (!other.CompareTag("Player") && other.GetComponent<PlayerController>() == null) return;

            if (lockType == DoorLockType.KeyLocked && ownerFloor != null && ownerFloor.HasExitKey)
            {
                UnlockAndSwingOpen();
            }
        }

        public void Interact(PlayerController player)
        {
            if (isUnlocked) return;

            switch (lockType)
            {
                case DoorLockType.KeyLocked:
                    if (ownerFloor != null && ownerFloor.HasExitKey)
                    {
                        UnlockAndSwingOpen();
                    }
                    else
                    {
                        AudioManager.Instance?.PlayDoorLocked();
                        ownerFloor?.OnLockedDoorTouched();
                    }
                    break;

                case DoorLockType.CodeLocked:
                    AudioManager.Instance?.PlayPuzzleClick();
                    GameManager.Instance?.OpenKeypadPuzzle(lockModalTitle, lockModalSubtitle, requiredCode, UnlockAndSwingOpen);
                    break;

                case DoorLockType.SymbolSequenceLocked:
                    AudioManager.Instance?.PlayPuzzleClick();
                    GameManager.Instance?.OpenSymbolSequencePuzzle(lockModalTitle, lockModalSubtitle, requiredSymbolSequence, UnlockAndSwingOpen);
                    break;
            }
        }

        public void UnlockAndSwingOpen()
        {
            if (isUnlocked) return;
            isUnlocked = true;

            if (doorBlockingCollider != null) doorBlockingCollider.enabled = false;

            AudioManager.Instance?.PlayDoorSwing();
            if (swingRoutine != null) StopCoroutine(swingRoutine);
            swingRoutine = StartCoroutine(SwingOpenRoutine());

            ownerFloor?.OnExitDoorUnlocked();
        }

        private IEnumerator SwingOpenRoutine()
        {
            CacheHinge();
            if (doorHingePivot == null) yield break;

            Quaternion targetRot = closedHingeRot * Quaternion.Euler(0f, openSwingAngle, 0f);
            float elapsed = 0f;
            while (elapsed < swingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, swingDuration));
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                doorHingePivot.localRotation = Quaternion.Slerp(closedHingeRot, targetRot, ease);
                yield return null;
            }
            doorHingePivot.localRotation = targetRot;
        }

        public void ResetState()
        {
            CacheHinge();
            if (swingRoutine != null)
            {
                StopCoroutine(swingRoutine);
                swingRoutine = null;
            }

            isUnlocked = false;
            if (doorHingePivot != null) doorHingePivot.localRotation = closedHingeRot;
            if (doorBlockingCollider != null) doorBlockingCollider.enabled = true;
            ConfigureDimRedLight();
        }

        private void ConfigureDimRedLight()
        {
            if (redDoorLight == null) return;
            redDoorLight.type = LightType.Point;
            redDoorLight.color = new Color(0.85f, 0.08f, 0.06f, 1f);
            redDoorLight.range = 3.2f;
            redDoorLight.intensity = 3.2f;
        }
    }
}
