using System.Collections.Generic;
using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Disorienting "Lost" Effect:
    /// Periodically randomizes the positions & rotations of distant decorative props
    /// (extra crates, pillars, chairs, debris) when they are outside the player's current
    /// light radius (distance > lightRange + 3f), choosing safe non-blocking floor spots.
    ///
    /// CRITICAL: Key items, cupboards/drawers, treasure chests, and exit doors are NEVER registered here.
    /// </summary>
    public class DisorientationManager : MonoBehaviour
    {
        [Header("References")]
        public LampController playerLamp;
        public Transform playerTransform;

        [Header("Disorientation Settings")]
        public float checkInterval = 2.6f;
        public float extraSafeBuffer = 3.0f;
        [Range(0.1f, 1f)] public float shiftProbability = 0.65f;

        [Header("Registered Decorative Props (Never Key Items or Containers!)")]
        public Transform[] decorativeProps;

        [Header("Pre-Validated Safe Non-Blocking Anchor Spots (Local to Floor)")]
        public Vector3[] safeLocalSpots;

        private float nextCheckTime;
        private readonly Dictionary<int, int> propToSpotIndex = new Dictionary<int, int>();
        private Vector3[] initialLocalPositions;
        private Quaternion[] initialLocalRotations;
        private bool cachedInitials;

        private void Awake()
        {
            CacheInitials();
        }

        private void OnEnable()
        {
            nextCheckTime = Time.time + checkInterval;
        }

        private void CacheInitials()
        {
            if (cachedInitials || decorativeProps == null) return;
            initialLocalPositions = new Vector3[decorativeProps.Length];
            initialLocalRotations = new Quaternion[decorativeProps.Length];
            for (int i = 0; i < decorativeProps.Length; i++)
            {
                if (decorativeProps[i] != null)
                {
                    initialLocalPositions[i] = decorativeProps[i].localPosition;
                    initialLocalRotations[i] = decorativeProps[i].localRotation;
                }
            }
            cachedInitials = true;
        }

        private void Update()
        {
            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + checkInterval;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            TryShiftDistantDecorativeProps();
        }

        private void TryShiftDistantDecorativeProps()
        {
            if (decorativeProps == null || decorativeProps.Length == 0) return;
            if (safeLocalSpots == null || safeLocalSpots.Length == 0) return;

            if (playerLamp == null) playerLamp = FindFirstObjectByType<LampController>();
            if (playerTransform == null && playerLamp != null) playerTransform = playerLamp.transform;
            if (playerTransform == null) return;

            float lightRange = playerLamp != null ? playerLamp.CurrentLightRange : 7f;
            float darkThreshold = lightRange + extraSafeBuffer;
            Vector3 playerPos = playerTransform.position;

            // Track currently occupied safe spots
            HashSet<int> occupiedSpots = new HashSet<int>();
            foreach (var kvp in propToSpotIndex)
            {
                occupiedSpots.Add(kvp.Value);
            }

            List<int> validDarkSpots = new List<int>();
            for (int s = 0; s < safeLocalSpots.Length; s++)
            {
                if (occupiedSpots.Contains(s)) continue;
                Vector3 worldSpot = transform.TransformPoint(safeLocalSpots[s]);
                float distToPlayer = FlatDistance(worldSpot, playerPos);
                if (distToPlayer > darkThreshold)
                {
                    validDarkSpots.Add(s);
                }
            }

            if (validDarkSpots.Count == 0) return;

            for (int i = 0; i < decorativeProps.Length; i++)
            {
                Transform prop = decorativeProps[i];
                if (prop == null) continue;

                // Only shift props that are currently OUTSIDE lightRange + 3f
                float currentDist = FlatDistance(prop.position, playerPos);
                if (currentDist <= darkThreshold) continue;
                if (Random.value > shiftProbability) continue;
                if (validDarkSpots.Count == 0) break;

                int pickListIdx = Random.Range(0, validDarkSpots.Count);
                int chosenSpotIdx = validDarkSpots[pickListIdx];
                validDarkSpots.RemoveAt(pickListIdx);

                // Free the old spot if this prop held one
                if (propToSpotIndex.TryGetValue(i, out int oldSpot))
                {
                    occupiedSpots.Remove(oldSpot);
                }

                propToSpotIndex[i] = chosenSpotIdx;
                occupiedSpots.Add(chosenSpotIdx);

                Vector3 targetLocal = safeLocalSpots[chosenSpotIdx];
                // Preserve original prop height Y so crates/chairs/pillars always rest cleanly on the floor
                float keepY = (initialLocalPositions != null && i < initialLocalPositions.Length)
                    ? initialLocalPositions[i].y
                    : prop.localPosition.y;

                prop.localPosition = new Vector3(targetLocal.x, keepY, targetLocal.z);
                prop.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }

        public void ResetProps()
        {
            CacheInitials();
            propToSpotIndex.Clear();
            if (decorativeProps == null || initialLocalPositions == null) return;

            for (int i = 0; i < decorativeProps.Length; i++)
            {
                if (decorativeProps[i] != null)
                {
                    decorativeProps[i].localPosition = initialLocalPositions[i];
                    decorativeProps[i].localRotation = initialLocalRotations[i];
                }
            }
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
