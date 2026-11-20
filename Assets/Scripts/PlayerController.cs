using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LastLight
{
    /// <summary>
    /// Camera-relative 3D player movement for First-Person (Default) & Over-the-Shoulder 3P views.
    /// Boost is completely removed. Drives humanoid walk bob, footsteps, handheld antique lantern sway,
    /// contextual OPEN/CLOSE interaction detection, and tap-to-interact raycasting.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 4.9f;
        public float rotationSpeed = 12f;
        public float standingHeightY = 0.0f;
        public float interactRange = 2.85f;

        [Header("References")]
        public VirtualJoystick virtualJoystick;
        public LampController lampController;
        public PlayerCharacterVisual characterVisual;
        public CameraController cameraController;
        public UIManager uiManager;

        private Rigidbody rb;
        private Collider playerCollider;
        private bool inputEnabled;
        private Vector3 pendingWorldMove;
        private IInteractable nearestInteractable;

        public bool InputEnabled => inputEnabled;
        public IInteractable NearestInteractable => nearestInteractable;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            playerCollider = GetComponent<Collider>();

            if (lampController == null) lampController = GetComponent<LampController>();
            if (characterVisual == null) characterVisual = GetComponent<PlayerCharacterVisual>();

            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }

        private void Update()
        {
            if (!inputEnabled)
            {
                pendingWorldMove = Vector3.zero;
                nearestInteractable = null;
                if (uiManager != null) uiManager.SetInteractPrompt(false, string.Empty);
                return;
            }

            Vector2 move2D = ReadMoveInput();

            float camYaw = cameraController != null ? cameraController.CurrentYaw : 0f;
            Quaternion yawRot = Quaternion.Euler(0f, camYaw, 0f);
            Vector3 forward = yawRot * Vector3.forward;
            Vector3 right = yawRot * Vector3.right;
            pendingWorldMove = forward * move2D.y + right * move2D.x;
            if (pendingWorldMove.sqrMagnitude > 1f) pendingWorldMove.Normalize();

            float speedNorm = pendingWorldMove.magnitude;
            characterVisual?.UpdateWalkAnimation(speedNorm, Time.deltaTime, playFootsteps: true);
            cameraController?.UpdateFirstPersonLanternSway(speedNorm);
            lampController?.UpdateLanternPropsMotion(speedNorm);

            ScanForNearestInteractable();
            CheckDirectTapOrKeyInteract();
        }

        private void FixedUpdate()
        {
            if (rb == null || rb.isKinematic) return;

            if (!inputEnabled)
            {
                SetVelocity(Vector3.zero);
                return;
            }

            SetVelocity(pendingWorldMove * moveSpeed);

            if (cameraController != null && cameraController.IsFirstPerson)
            {
                transform.rotation = Quaternion.Euler(0f, cameraController.CurrentYaw, 0f);
            }
            else if (pendingWorldMove.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(pendingWorldMove.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        public void SetSelectedGender(CharacterGender gender)
        {
            characterVisual?.SetGender(gender);
        }

        public void InteractWithNearest()
        {
            if (!inputEnabled || nearestInteractable == null) return;
            if (!nearestInteractable.CanInteract) return;
            nearestInteractable.Interact(this);
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            cameraController?.SetLookInputEnabled(enabled);

            if (!enabled)
            {
                pendingWorldMove = Vector3.zero;
                nearestInteractable = null;
                if (rb != null && !rb.isKinematic) SetVelocity(Vector3.zero);
                virtualJoystick?.ResetJoystick();
                uiManager?.SetInteractPrompt(false, string.Empty);
                characterVisual?.ResetPose();
            }
        }

        public void SetIntroPhysicsMode(bool introMode)
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (playerCollider == null) playerCollider = GetComponent<Collider>();

            if (introMode)
            {
                if (!rb.isKinematic) SetVelocity(Vector3.zero);
                rb.isKinematic = true;
                if (playerCollider != null) playerCollider.enabled = false;
            }
            else
            {
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                SetVelocity(Vector3.zero);
                if (playerCollider != null) playerCollider.enabled = true;
            }
        }

        public void TeleportUpright(Vector3 worldPos, float facingYaw = 0f)
        {
            if (rb != null && !rb.isKinematic) SetVelocity(Vector3.zero);

            Vector3 pos = new Vector3(worldPos.x, standingHeightY, worldPos.z);
            Quaternion rot = Quaternion.Euler(0f, facingYaw, 0f);
            transform.position = pos;
            transform.rotation = rot;

            if (rb != null)
            {
                rb.position = pos;
                rb.rotation = rot;
            }
            cameraController?.SetYaw(facingYaw, 4f);
        }

        private void ScanForNearestInteractable()
        {
            nearestInteractable = null;
            float bestDistSq = interactRange * interactRange;

            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.9f, interactRange);
            for (int i = 0; i < hits.Length; i++)
            {
                IInteractable candidate = hits[i].GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract) continue;

                Vector3 diff = candidate.InteractionWorldPosition - transform.position;
                diff.y = 0f;
                float dSq = diff.sqrMagnitude;
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    nearestInteractable = candidate;
                }
            }

            if (uiManager != null)
            {
                if (nearestInteractable != null)
                    uiManager.SetInteractPrompt(true, nearestInteractable.InteractionPrompt);
                else
                    uiManager.SetInteractPrompt(false, string.Empty);
            }
        }

        private void CheckDirectTapOrKeyInteract()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.qKey.wasPressedThisFrame) GameManager.Instance?.ToggleFlameMode();
                if (kb.fKey.wasPressedThisFrame) GameManager.Instance?.ToggleLampPower();
                if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                {
                    InteractWithNearest();
                    return;
                }
            }

            Vector2 tapPos = Vector2.zero;
            bool tapped = false;
            int pointerId = -1;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                tapPos = Touchscreen.current.primaryTouch.position.ReadValue();
                pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                tapped = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                tapPos = Mouse.current.position.ReadValue();
                tapped = true;
            }

            if (tapped)
            {
                bool overUI = EventSystem.current != null &&
                              (pointerId >= 0
                                  ? EventSystem.current.IsPointerOverGameObject(pointerId)
                                  : EventSystem.current.IsPointerOverGameObject());
                if (!overUI) TryTapInteractAtScreenPos(tapPos);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                InteractWithNearest();
                return;
            }
            if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                TryTapInteractAtScreenPos(Input.mousePosition);
            }
#endif
        }

        private void TryTapInteractAtScreenPos(Vector2 screenPos)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 12f))
            {
                IInteractable target = hit.collider.GetComponentInParent<IInteractable>();
                if (target != null && target.CanInteract)
                {
                    Vector3 diff = target.InteractionWorldPosition - transform.position;
                    diff.y = 0f;
                    if (diff.magnitude <= interactRange + 1.0f) target.Interact(this);
                }
            }
        }

        private Vector2 ReadMoveInput()
        {
            Vector2 move = Vector2.zero;
            if (virtualJoystick != null) move += virtualJoystick.Direction;

            Vector2 kb = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) kb.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) kb.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) kb.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) kb.y += 1f;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            kb.x = Input.GetAxisRaw("Horizontal");
            kb.y = Input.GetAxisRaw("Vertical");
#endif
            move += kb;
            if (move.sqrMagnitude > 1f) move.Normalize();
            return move;
        }

        private void SetVelocity(Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
