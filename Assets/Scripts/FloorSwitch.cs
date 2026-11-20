using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Optional pressure plate switch component (preserved for clean project compatibility).
    /// </summary>
    public class FloorSwitch : MonoBehaviour
    {
        public bool IsActivated { get; private set; }

        public void ResetState()
        {
            IsActivated = false;
        }
    }
}
