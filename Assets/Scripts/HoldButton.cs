using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastLight
{
    /// <summary>
    /// Hold-to-Boost mobile button with visual press feedback.
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Image backgroundImage;
        public Text labelText;

        public Color idleColor = new Color(0.18f, 0.14f, 0.10f, 0.82f);
        public Color pressedColor = new Color(0.95f, 0.68f, 0.18f, 0.96f);
        public Color idleTextColor = new Color(1f, 0.85f, 0.45f, 1f);
        public Color pressedTextColor = new Color(0.08f, 0.05f, 0.02f, 1f);

        private int activePointerId = -999;
        private Vector3 baseScale = Vector3.one;

        public bool IsPressed { get; private set; }

        private void Awake()
        {
            baseScale = transform.localScale;
            ApplyVisuals(false);
        }

        private void OnDisable()
        {
            ResetState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != -999) return;
            activePointerId = eventData.pointerId;
            IsPressed = true;
            ApplyVisuals(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            ResetState();
        }

        public void ResetState()
        {
            activePointerId = -999;
            IsPressed = false;
            ApplyVisuals(false);
        }

        private void ApplyVisuals(bool pressed)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = pressed ? pressedColor : idleColor;
            }
            if (labelText != null)
            {
                labelText.color = pressed ? pressedTextColor : idleTextColor;
            }
            transform.localScale = pressed ? baseScale * 0.93f : baseScale;
        }
    }
}
