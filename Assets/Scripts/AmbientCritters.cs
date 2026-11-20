using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Animated low-poly floor rat that scurries along floorboards and walls across the Haveli.
    /// Plays a high-pitched rat squeak SFX via AudioManager whenever it darts within 4.5 units of the player.
    /// </summary>
    public class ScurryingRat : MonoBehaviour
    {
        public Vector3[] localWaypoints;
        public Transform playerTransform;
        public Transform tailPivot;
        public float moveSpeed = 3.2f;
        public float turnSpeed = 12f;
        public float squeakRadius = 4.5f;
        public float squeakCooldown = 4.5f;

        private int targetIndex;
        private float pauseTimer;
        private float nextSqueakAllowedTime;
        private float scurryPhase;

        private void OnEnable()
        {
            targetIndex = 0;
            pauseTimer = Random.Range(0.2f, 1.4f);
            nextSqueakAllowedTime = Time.time + Random.Range(1.0f, 3.0f);
        }

        private void Update()
        {
            if (localWaypoints == null || localWaypoints.Length < 2) return;

            if (playerTransform == null && GameManager.Instance != null && GameManager.Instance.playerController != null)
            {
                playerTransform = GameManager.Instance.playerController.transform;
            }

            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.deltaTime;
                AnimateTail(0.25f);
                return;
            }

            Vector3 targetLocal = localWaypoints[targetIndex];
            Vector3 currentLocal = transform.localPosition;
            Vector3 diff = targetLocal - currentLocal;
            diff.y = 0f;

            if (diff.magnitude < 0.25f)
            {
                targetIndex = (targetIndex + 1) % localWaypoints.Length;
                pauseTimer = Random.Range(0.35f, 1.5f);
                return;
            }

            Vector3 dir = diff.normalized;
            transform.localPosition = currentLocal + dir * (moveSpeed * Time.deltaTime);

            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, turnSpeed * Time.deltaTime);

            AnimateTail(1.0f);
            CheckPlayerProximitySqueak();
        }

        private void AnimateTail(float intensity)
        {
            scurryPhase += Time.deltaTime * (intensity > 0.5f ? 18f : 4f);
            if (tailPivot != null)
            {
                tailPivot.localRotation = Quaternion.Euler(0f, Mathf.Sin(scurryPhase) * (intensity > 0.5f ? 26f : 8f), 0f);
            }
        }

        private void CheckPlayerProximitySqueak()
        {
            if (playerTransform == null || Time.time < nextSqueakAllowedTime) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= squeakRadius)
            {
                nextSqueakAllowedTime = Time.time + squeakCooldown + Random.Range(0f, 2.5f);
                AudioManager.Instance?.PlayRatSqueak();
            }
        }
    }

    /// <summary>
    /// Dark 8-legged ceiling/cobweb spider with tiny unlit red eyes (zero scene light emitted).
    /// Gently drops/bobs on a silk thread or twitches along cobwebs in corners and chandeliers.
    /// </summary>
    public class CeilingSpider : MonoBehaviour
    {
        public float bobAmplitude = 0.28f;
        public float bobSpeed = 1.15f;
        public float twitchSpeed = 7.0f;

        private Vector3 baseLocalPos;
        private Quaternion baseLocalRot;
        private float seed;
        private bool cached;

        private void Awake()
        {
            baseLocalPos = transform.localPosition;
            baseLocalRot = transform.localRotation;
            seed = Random.Range(0f, 100f);
            cached = true;
        }

        private void Update()
        {
            if (!cached) return;
            float t = Time.time;
            float drop = Mathf.Sin(t * bobSpeed + seed) * bobAmplitude;
            float yawTwitch = (Mathf.PerlinNoise(seed, t * twitchSpeed * 0.25f) - 0.5f) * 32f;

            transform.localPosition = baseLocalPos + Vector3.up * drop;
            transform.localRotation = baseLocalRot * Quaternion.Euler(0f, yawTwitch, 0f);
        }
    }
}
