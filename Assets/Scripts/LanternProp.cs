using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Multi-part vintage antique brass & glass oil lantern prop.
    /// Drives subtle flame sway and warm flickering glow intensity on the glass core
    /// (without emitting light into the environment beyond the player's single Lamp Point Light).
    /// </summary>
    public class LanternProp : MonoBehaviour
    {
        public Transform swayPivot;
        public Renderer glassCoreRenderer;
        public Light optionalAttachedLight;

        public float idleSwaySpeed = 2.0f;
        public float idleSwayAngle = 2.4f;
        public float walkSwaySpeed = 9.5f;
        public float walkSwayAngle = 7.5f;

        private float phase;
        private Quaternion basePivotRot = Quaternion.identity;
        private bool cachedRot;

        private void Awake()
        {
            if (swayPivot != null)
            {
                basePivotRot = swayPivot.localRotation;
                cachedRot = true;
            }
        }

        public void UpdateLanternMotion(float normalizedSpeed, float oilNormalized, bool spiritMode = false, bool extinguished = false)
        {
            if (swayPivot == null) return;
            if (!cachedRot)
            {
                basePivotRot = swayPivot.localRotation;
                cachedRot = true;
            }

            float spd = normalizedSpeed > 0.08f ? walkSwaySpeed : idleSwaySpeed;
            float ang = normalizedSpeed > 0.08f ? walkSwayAngle : idleSwayAngle;
            phase += Time.deltaTime * spd;

            float roll = Mathf.Sin(phase) * ang;
            float pitch = Mathf.Cos(phase * 0.5f) * (ang * 0.55f);
            swayPivot.localRotation = basePivotRot * Quaternion.Euler(pitch, 0f, roll);

            // Subtle non-emissive glass tint reflecting Warm / Spirit Cyan / Extinguished state
            if (glassCoreRenderer != null)
            {
                Material mat = glassCoreRenderer.material;
                if (mat != null)
                {
                    Color litTint;
                    if (extinguished || oilNormalized <= 0f)
                    {
                        litTint = new Color(0.10f, 0.10f, 0.12f, 1f);
                    }
                    else if (spiritMode)
                    {
                        litTint = Color.Lerp(new Color(0.12f, 0.34f, 0.44f, 1f), new Color(0.24f, 0.88f, 1.0f, 1f), Mathf.Clamp01(oilNormalized));
                    }
                    else
                    {
                        litTint = Color.Lerp(new Color(0.28f, 0.20f, 0.10f, 1f), new Color(0.98f, 0.78f, 0.34f, 1f), Mathf.Clamp01(oilNormalized));
                    }

                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", litTint);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", litTint);
                }
            }
        }
    }
}
