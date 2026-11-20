using UnityEngine;

namespace LastLight
{
    public enum PickupType
    {
        OilFlask,
        RealKey,
        FakeKey
    }

    /// <summary>
    /// Auto-interact floor pickups (Oil Flask +20 oil, Real Key, or Fake Key trap).
    /// </summary>
    public class PickupItem : MonoBehaviour
    {
        public PickupType pickupType = PickupType.OilFlask;
        public float oilDelta = 20f;
        public bool onlyVisibleUnderPlayerLight = false;
        public float bobAmplitude = 0.12f;
        public float bobSpeed = 2.6f;
        public float rotateSpeed = 55f;

        [Header("Runtime References")]
        public FloorController ownerFloor;
        public LampController playerLamp;
        public Renderer[] visualRenderers;
        public Light subtleGlowLight;

        private Vector3 startLocalPos;
        private bool collected;
        private bool hasCachedPos;
        private Collider triggerCollider;

        private void Awake() => CacheReferences();

        private void OnEnable()
        {
            CacheReferences();
            UpdateLightVisibility();
        }

        private void CacheReferences()
        {
            if (!hasCachedPos)
            {
                startLocalPos = transform.localPosition;
                hasCachedPos = true;
            }
            if (triggerCollider == null) triggerCollider = GetComponent<Collider>();
            if (visualRenderers == null || visualRenderers.Length == 0)
            {
                visualRenderers = GetComponentsInChildren<Renderer>(true);
            }
            if (playerLamp == null) playerLamp = FindFirstObjectByType<LampController>();
        }

        private void Update()
        {
            if (collected) return;

            float yOffset = Mathf.Sin(Time.time * bobSpeed + startLocalPos.x) * bobAmplitude;
            transform.localPosition = startLocalPos + Vector3.up * yOffset;
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            if (onlyVisibleUnderPlayerLight)
            {
                UpdateLightVisibility();
            }
        }

        private void UpdateLightVisibility()
        {
            if (!onlyVisibleUnderPlayerLight || collected) return;
            if (playerLamp == null) playerLamp = FindFirstObjectByType<LampController>();

            bool visible = false;
            if (playerLamp != null && playerLamp.CurrentLightRange > 0.25f)
            {
                Vector3 lampPos = playerLamp.LightWorldPosition;
                Vector3 myPos = transform.position;
                Vector2 diff = new Vector2(lampPos.x - myPos.x, lampPos.z - myPos.z);
                visible = diff.magnitude <= playerLamp.CurrentLightRange * 0.92f;
            }

            SetVisualsActive(visible);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (!other.CompareTag("Player") && other.GetComponent<PlayerController>() == null) return;
            Collect(other.gameObject);
        }

        private void Collect(GameObject playerObj)
        {
            collected = true;
            SetVisualsActive(false);
            if (subtleGlowLight != null) subtleGlowLight.enabled = false;
            if (triggerCollider != null) triggerCollider.enabled = false;

            LampController lamp = playerLamp != null ? playerLamp : playerObj.GetComponent<LampController>();
            GameManager gm = GameManager.Instance;

            switch (pickupType)
            {
                case PickupType.OilFlask:
                    if (lamp != null) lamp.AddOil(Mathf.Abs(oilDelta));
                    AudioManager.Instance?.PlayOilPickup();
                    gm?.NotifyPickup($"+{Mathf.RoundToInt(Mathf.Abs(oilDelta))} OIL", new Color(0.55f, 1f, 0.55f, 1f));
                    break;

                case PickupType.RealKey:
                    AudioManager.Instance?.PlayPuzzleUnlock();
                    ownerFloor?.OnChestExitKeyAwarded();
                    break;

                case PickupType.FakeKey:
                    AudioManager.Instance?.PlayPuzzleError();
                    float drain = Mathf.Abs(oilDelta) > 0f ? Mathf.Abs(oilDelta) : 15f;
                    if (lamp != null) lamp.DrainOilImmediate(drain);
                    gm?.OnFakeKeyTriggered(drain);
                    break;
            }
        }

        public void ResetState()
        {
            CacheReferences();
            collected = false;
            transform.localPosition = startLocalPos;
            if (triggerCollider != null) triggerCollider.enabled = true;

            if (onlyVisibleUnderPlayerLight) UpdateLightVisibility();
            else SetVisualsActive(true);

            if (subtleGlowLight != null) subtleGlowLight.enabled = !onlyVisibleUnderPlayerLight;
        }

        private void SetVisualsActive(bool active)
        {
            if (visualRenderers == null) return;
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                if (visualRenderers[i] != null) visualRenderers[i].enabled = active;
            }
        }
    }
}
