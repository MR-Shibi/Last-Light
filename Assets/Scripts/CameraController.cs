using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LastLight
{
    public enum CameraViewMode
    {
        FirstPerson,
        ThirdPersonShoulder
    }

    /// <summary>
    /// First-Person Default Camera & Over-the-Shoulder Third-Person Toggle Camera.
    /// - Default: First-Person eye-level view with the antique brass oil lantern visible in the lower-right corner.
    /// - Toggle ("1P / 3P" HUD button): Over-the-Shoulder Third-Person (close behind & right shoulder)
    ///   with SphereCast wall-collision prevention so it never clips through walls.
    /// - Supports CutsceneOverride mode for cinematic movie sweeps during the opening cutscene.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target & Visuals")]
        public Transform target;
        public PlayerCharacterVisual characterVisual;
        public Transform firstPersonLanternRoot;

        [Header("Over-the-Shoulder 3P Settings")]
        public Vector3 shoulderOffset = new Vector3(0.58f, 1.62f, -2.15f);
        public float pivotHeight = 1.52f;
        public float collisionRadius = 0.24f;
        public float minWallDistance = 0.42f;
        public LayerMask collisionMask = ~0;

        [Header("First-Person 1P Settings (Default)")]
        public float eyeHeight = 1.58f;
        public Vector3 fpLanternLocalPos = new Vector3(0.35f, -0.30f, 0.56f);

        [Header("Look & Smoothing")]
        public float touchLookSensitivity = 0.22f;
        public float mouseLookSensitivity = 0.25f;
        public float keyboardTurnSpeed = 110f;
        public float minPitch = -28f;
        public float maxPitch = 34f;
        public float positionSmoothSpeed = 14f;
        public float rotationSmoothSpeed = 16f;

        private CameraViewMode currentMode = CameraViewMode.FirstPerson;
        private float yaw;
        private float pitch = 4f;
        private bool lookInputEnabled;
        private bool cutsceneOverride;
        private bool isDraggingLook;
        private Vector2 lastLookPos;
        private int activeLookTouchId = -1;

        private float shakeTimeRemaining;
        private float shakeDuration;
        private float shakeMagnitude;
        private float fpBobPhase;

        public CameraViewMode CurrentMode => currentMode;
        public float CurrentYaw => yaw;
        public bool IsFirstPerson => currentMode == CameraViewMode.FirstPerson;

        private void Start()
        {
            ApplyModeVisuals();
            if (!cutsceneOverride) SnapToTarget();
        }

        private void Update()
        {
            if (cutsceneOverride || !lookInputEnabled)
            {
                isDraggingLook = false;
                activeLookTouchId = -1;
                return;
            }
            ReadLookInput();
        }

        private void LateUpdate()
        {
            if (cutsceneOverride || target == null) return;

            Vector3 shakeOffset = ComputeShakeOffset();

            if (currentMode == CameraViewMode.FirstPerson)
            {
                UpdateFirstPersonCamera(shakeOffset);
            }
            else
            {
                UpdateOverTheShoulderCamera(shakeOffset);
            }
        }

        public void SetCutsceneOverride(bool active)
        {
            cutsceneOverride = active;
            if (active)
            {
                if (firstPersonLanternRoot != null) firstPersonLanternRoot.gameObject.SetActive(false);
                if (characterVisual != null) characterVisual.SetFirstPersonBodyHidden(false);
            }
            else
            {
                ApplyModeVisuals();
            }
        }

        public void SetLookInputEnabled(bool enabled)
        {
            lookInputEnabled = enabled;
            if (!enabled)
            {
                isDraggingLook = false;
                activeLookTouchId = -1;
            }
        }

        public void ToggleViewMode()
        {
            currentMode = currentMode == CameraViewMode.FirstPerson
                ? CameraViewMode.ThirdPersonShoulder
                : CameraViewMode.FirstPerson;

            AudioManager.Instance?.PlayPuzzleClick();
            ApplyModeVisuals();
            SnapToTarget();
        }

        public void SetViewMode(CameraViewMode mode)
        {
            currentMode = mode;
            if (!cutsceneOverride)
            {
                ApplyModeVisuals();
                SnapToTarget();
            }
        }

        public void SetYaw(float newYaw, float newPitch = 4f)
        {
            yaw = newYaw;
            pitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
        }

        public void SnapToTarget()
        {
            if (target == null || cutsceneOverride) return;
            Quaternion lookRot = Quaternion.Euler(pitch, yaw, 0f);

            if (currentMode == CameraViewMode.FirstPerson)
            {
                transform.position = target.position + Vector3.up * eyeHeight;
                transform.rotation = lookRot;
            }
            else
            {
                Vector3 desiredPos = ComputeSafeShoulderPosition(lookRot);
                transform.position = desiredPos;
                Vector3 lookTarget = target.position + Vector3.up * pivotHeight + (lookRot * Vector3.forward) * 6f;
                transform.LookAt(lookTarget);
            }
        }

        public void Shake(float duration = 0.45f, float magnitude = 0.35f)
        {
            shakeDuration = Mathf.Max(0.05f, duration);
            shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, shakeDuration);
            shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
        }

        public void UpdateFirstPersonLanternSway(float normalizedMoveSpeed)
        {
            if (firstPersonLanternRoot == null || !firstPersonLanternRoot.gameObject.activeSelf) return;

            fpBobPhase += Time.deltaTime * (normalizedMoveSpeed > 0.08f ? 9.5f : 1.9f);
            float amp = normalizedMoveSpeed > 0.08f ? 0.022f : 0.006f;

            Vector3 bob = new Vector3(
                Mathf.Cos(fpBobPhase * 0.5f) * amp,
                Mathf.Sin(fpBobPhase) * amp,
                0f
            );
            firstPersonLanternRoot.localPosition = Vector3.Lerp(
                firstPersonLanternRoot.localPosition,
                fpLanternLocalPos + bob,
                1f - Mathf.Exp(-12f * Time.deltaTime)
            );
        }

        private void ApplyModeVisuals()
        {
            bool fp = !cutsceneOverride && currentMode == CameraViewMode.FirstPerson;
            if (characterVisual != null) characterVisual.SetFirstPersonBodyHidden(fp);
            if (firstPersonLanternRoot != null)
            {
                firstPersonLanternRoot.gameObject.SetActive(fp);
                firstPersonLanternRoot.localPosition = fpLanternLocalPos;
            }
        }

        private void UpdateOverTheShoulderCamera(Vector3 shakeOffset)
        {
            Quaternion lookRot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPos = ComputeSafeShoulderPosition(lookRot);

            float posLerp = 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPos, posLerp) + shakeOffset;

            Vector3 shoulderAimPoint = target.position + Vector3.up * pivotHeight +
                                       (lookRot * Vector3.right) * (shoulderOffset.x * 0.45f) +
                                       (lookRot * Vector3.forward) * 6.0f;
            Quaternion targetRot = Quaternion.LookRotation((shoulderAimPoint - transform.position).normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime));
        }

        private void UpdateFirstPersonCamera(Vector3 shakeOffset)
        {
            Vector3 eyePos = target.position + Vector3.up * eyeHeight;
            transform.position = eyePos + shakeOffset;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private Vector3 ComputeSafeShoulderPosition(Quaternion lookRot)
        {
            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            Vector3 yawRight = Quaternion.Euler(0f, yaw, 0f) * new Vector3(shoulderOffset.x, 0f, 0f);
            Vector3 fullOffset = lookRot * new Vector3(0f, shoulderOffset.y - pivotHeight, shoulderOffset.z) + yawRight;
            Vector3 desiredPos = pivot + fullOffset;

            Vector3 dir = desiredPos - pivot;
            float dist = dir.magnitude;
            if (dist < 0.05f) return desiredPos;
            dir /= dist;

            RaycastHit[] hits = Physics.SphereCastAll(pivot, collisionRadius, dir, dist, collisionMask, QueryTriggerInteraction.Ignore);
            float closestDist = dist;

            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTf = hits[i].collider.transform;
                if (hitTf == target || hitTf.IsChildOf(target)) continue;

                float hitDist = Mathf.Max(minWallDistance, hits[i].distance - 0.08f);
                if (hitDist < closestDist) closestDist = hitDist;
            }

            return pivot + dir * closestDist;
        }

        private void ReadLookInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.qKey.isPressed) yaw -= keyboardTurnSpeed * Time.deltaTime;
                if (kb.rKey.isPressed) yaw += keyboardTurnSpeed * Time.deltaTime;
            }

            Touchscreen touch = Touchscreen.current;
            if (touch != null)
            {
                var touches = touch.touches;
                bool foundActive = false;
                for (int i = 0; i < touches.Count; i++)
                {
                    var t = touches[i];
                    int id = t.touchId.ReadValue();
                    Vector2 pos = t.position.ReadValue();

                    if (activeLookTouchId == -1)
                    {
                        if (t.press.wasPressedThisFrame && pos.x > Screen.width * 0.36f)
                        {
                            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id);
                            if (!overUI)
                            {
                                activeLookTouchId = id;
                                lastLookPos = pos;
                                foundActive = true;
                                break;
                            }
                        }
                    }
                    else if (id == activeLookTouchId)
                    {
                        if (t.press.isPressed)
                        {
                            Vector2 delta = pos - lastLookPos;
                            lastLookPos = pos;
                            yaw += delta.x * touchLookSensitivity;
                            pitch = Mathf.Clamp(pitch - delta.y * touchLookSensitivity * 0.7f, minPitch, maxPitch);
                            foundActive = true;
                        }
                        break;
                    }
                }
                if (!foundActive) activeLookTouchId = -1;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && activeLookTouchId == -1)
            {
                Vector2 mPos = mouse.position.ReadValue();
                bool startDrag = mouse.rightButton.wasPressedThisFrame ||
                                 (mouse.leftButton.wasPressedThisFrame && mPos.x > Screen.width * 0.35f &&
                                  (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()));

                if (startDrag)
                {
                    isDraggingLook = true;
                    lastLookPos = mPos;
                }
                else if (!mouse.rightButton.isPressed && !mouse.leftButton.isPressed)
                {
                    isDraggingLook = false;
                }

                if (isDraggingLook)
                {
                    Vector2 delta = mPos - lastLookPos;
                    lastLookPos = mPos;
                    yaw += delta.x * mouseLookSensitivity;
                    pitch = Mathf.Clamp(pitch - delta.y * mouseLookSensitivity * 0.7f, minPitch, maxPitch);
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.Q)) yaw -= keyboardTurnSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.R)) yaw += keyboardTurnSpeed * Time.deltaTime;

            if (Input.GetMouseButtonDown(1) || (Input.GetMouseButtonDown(0) && Input.mousePosition.x > Screen.width * 0.35f &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())))
            {
                isDraggingLook = true;
                lastLookPos = Input.mousePosition;
            }
            else if (!Input.GetMouseButton(1) && !Input.GetMouseButton(0))
            {
                isDraggingLook = false;
            }

            if (isDraggingLook)
            {
                Vector2 cur = Input.mousePosition;
                Vector2 delta = cur - lastLookPos;
                lastLookPos = cur;
                yaw += delta.x * mouseLookSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * mouseLookSensitivity * 0.7f, minPitch, maxPitch);
            }
#endif
        }

        private Vector3 ComputeShakeOffset()
        {
            if (shakeTimeRemaining <= 0f) return Vector3.zero;
            shakeTimeRemaining -= Time.deltaTime;
            float damper = Mathf.Clamp01(shakeTimeRemaining / Mathf.Max(0.001f, shakeDuration));
            float mag = shakeMagnitude * damper;
            return new Vector3(
                Random.Range(-1f, 1f) * mag,
                Random.Range(-0.5f, 0.5f) * mag,
                Random.Range(-1f, 1f) * mag
            );
        }
    }
}
