using UnityEngine;
using UnityEngine.UI;

namespace LastLight
{
    /// <summary>
    /// World-space floor hint text (e.g., "The needle lies" on Floor 2)
    /// that reveals smoothly when illuminated by the player's lamp light.
    /// </summary>
    public class WorldHintText : MonoBehaviour
    {
        public Text hintText;
        public LampController playerLamp;
        public Color baseColor = new Color(0.92f, 0.78f, 0.45f, 1f);
        public float minAmbientAlpha = 0.03f;

        private void Awake()
        {
            if (hintText == null)
            {
                hintText = GetComponentInChildren<Text>(true);
            }
        }

        private void Update()
        {
            if (hintText == null) return;
            if (playerLamp == null)
            {
                playerLamp = FindFirstObjectByType<LampController>();
            }

            float targetAlpha = minAmbientAlpha;
            if (playerLamp != null && playerLamp.CurrentLightRange > 0.25f)
            {
                Vector3 lp = playerLamp.LightWorldPosition;
                Vector3 myPos = transform.position;
                float dist = Vector2.Distance(new Vector2(lp.x, lp.z), new Vector2(myPos.x, myPos.z));
                float range = playerLamp.CurrentLightRange;
                float lit = Mathf.Clamp01(1f - (dist / Mathf.Max(0.5f, range * 0.95f)));
                targetAlpha = Mathf.Max(minAmbientAlpha, Mathf.Pow(lit, 0.6f) * 0.95f);
            }

            Color c = baseColor;
            c.a = Mathf.Lerp(hintText.color.a, targetAlpha, Time.deltaTime * 8f);
            hintText.color = c;
        }
    }
}
