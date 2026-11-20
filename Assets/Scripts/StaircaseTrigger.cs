using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Placed along the descending 3D staircase behind each unlocked Exit Door.
    /// When the player walks down the stairs, triggers a smooth fade + level banner transition.
    /// </summary>
    public class StaircaseTrigger : MonoBehaviour
    {
        public FloorController ownerFloor;
        private bool triggered;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (!other.CompareTag("Player") && other.GetComponent<PlayerController>() == null) return;

            if (ownerFloor != null && ownerFloor.exitDoor != null && !ownerFloor.exitDoor.IsUnlocked)
            {
                return;
            }

            triggered = true;
            if (ownerFloor != null)
            {
                ownerFloor.OnStaircaseDescended();
            }
            else
            {
                GameManager.Instance?.OnFloorCompleted();
            }
        }

        public void ResetState()
        {
            triggered = false;
        }
    }
}
