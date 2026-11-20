using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastLight
{
    /// <summary>
    /// On-screen Floating Virtual Joystick for mobile 3D top-down movement.
    /// Place this component on the touch-capture area in the bottom-left corner.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("UI References")]
        public RectTransform touchArea;
        public RectTransform background;
        public RectTransform handle;
        public CanvasGroup visualGroup;

        [Header("Settings")]
        public float handleRange = 85f;
        public float deadZone = 0.08f;
        public float idleAlpha = 0.45f;
        public float activeAlpha = 0.95f;

        private Vector2 inputVector = Vector2.zero;
        private Vector2 defaultBgPosition;
        private bool hasDefaultPosition;
        private int activePointerId = -999;

        public Vector2 Direction => inputVector;
        public float Horizontal => inputVector.x;
        public float Vertical => inputVector.y;

        private void Awake()
        {
            if (touchArea == null)
            {
                touchArea = GetComponent<RectTransform>();
            }
            if (background != null)
            {
                defaultBgPosition = background.anchoredPosition;
                hasDefaultPosition = true;
            }
            SetVisualAlpha(idleAlpha);
        }

        private void OnDisable()
        {
            ResetJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != -999) return;
            activePointerId = eventData.pointerId;

            if (background != null && touchArea != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    touchArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                {
                    background.anchoredPosition = ClampBackgroundToTouchArea(localPoint);
                }
            }

            SetVisualAlpha(activeAlpha);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            if (background == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                Vector2 raw = localPoint / Mathf.Max(1f, handleRange);
                if (raw.magnitude > 1f)
                {
                    raw = raw.normalized;
                }

                inputVector = raw.magnitude < deadZone ? Vector2.zero : raw;

                if (handle != null)
                {
                    handle.anchoredPosition = raw * handleRange;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            ResetJoystick();
        }

        public void ResetJoystick()
        {
            activePointerId = -999;
            inputVector = Vector2.zero;

            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
            if (background != null && hasDefaultPosition)
            {
                background.anchoredPosition = defaultBgPosition;
            }
            SetVisualAlpha(idleAlpha);
        }

        private Vector2 ClampBackgroundToTouchArea(Vector2 localPoint)
        {
            Rect r = touchArea.rect;
            float pad = handleRange * 0.65f;
            float x = Mathf.Clamp(localPoint.x, r.xMin + pad, r.xMax - pad);
            float y = Mathf.Clamp(localPoint.y, r.yMin + pad, r.yMax - pad);
            return new Vector2(x, y);
        }

        private void SetVisualAlpha(float alpha)
        {
            if (visualGroup != null)
            {
                visualGroup.alpha = alpha;
            }
        }
    }
}
