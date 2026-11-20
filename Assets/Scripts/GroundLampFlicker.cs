using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Softly flickers the GroundLamp's warm yellow Point Light during the Floor 1 intro sequence.
    /// </summary>
    public class GroundLampFlicker : MonoBehaviour
    {
        public Light lampLight;
        public float baseIntensity = 18f;
        public float baseRange = 6.5f;
        public float flickerAmount = 0.22f;
        public float flickerSpeed = 6.5f;

        private float seed;

        private void Awake()
        {
            seed = Random.Range(0f, 100f);
            if (lampLight == null)
            {
                lampLight = GetComponentInChildren<Light>();
            }
        }

        private void Update()
        {
            if (lampLight == null) return;
            float n = Mathf.PerlinNoise(seed, Time.time * flickerSpeed) - 0.5f;
            float m = 1f + n * 2f * flickerAmount;
            lampLight.intensity = baseIntensity * m;
            lampLight.range = baseRange * (1f + n * 0.12f);
        }
    }
}
