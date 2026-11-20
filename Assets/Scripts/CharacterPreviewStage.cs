using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Live rotating 3D preview stage for the Dark Character Select screen.
    /// Automatically activates ONLY while Character Select is open and disables completely
    /// during gameplay so zero extra lights exist outside the mansion.
    /// </summary>
    public class CharacterPreviewStage : MonoBehaviour
    {
        public GameObject stageRoot;
        public Transform malePreviewPivot;
        public Transform femalePreviewPivot;
        public float rotationSpeed = 28f;

        private bool isActive;

        public void SetPreviewActive(bool active)
        {
            isActive = active;
            if (stageRoot != null)
            {
                stageRoot.SetActive(active);
            }
        }

        private void Update()
        {
            if (!isActive) return;

            if (malePreviewPivot != null)
            {
                malePreviewPivot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
            if (femalePreviewPivot != null)
            {
                femalePreviewPivot.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}
