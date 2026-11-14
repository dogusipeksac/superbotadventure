using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperBot.Controls
{
    /// <summary>
    /// Floating joystick. Put this on a transparent full-height Image covering the left part of the screen
    /// (pivot 0.5,0.5). <see cref="background"/> must be a direct child anchored at the zone center.
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform knob;
        [SerializeField] private float radius = 90f;
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;

        private RectTransform zone;
        private Vector2 restPosition;
        private int activePointer = int.MinValue;

        private void Awake()
        {
            zone = (RectTransform)transform;
            if (background != null) restPosition = background.anchoredPosition;
        }

        private void OnDisable() => ResetStick();

        public void OnPointerDown(PointerEventData e)
        {
            if (activePointer != int.MinValue) return;
            activePointer = e.pointerId;
            if (background != null && ToLocal(e, out var local)) background.anchoredPosition = local;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != activePointer || background == null) return;
            if (!ToLocal(e, out var local)) return;
            Vector2 offset = Vector2.ClampMagnitude(local - background.anchoredPosition, radius);
            if (knob != null) knob.anchoredPosition = offset;
            Vector2 value = offset / radius;
            float mag = value.magnitude;
            value = mag < deadZone ? Vector2.zero : value * ((mag - deadZone) / (1f - deadZone) / mag);
            if (MobileInputRouter.Instance != null) MobileInputRouter.Instance.SetJoystick(value);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != activePointer) return;
            ResetStick();
        }

        private void ResetStick()
        {
            activePointer = int.MinValue;
            if (background != null) background.anchoredPosition = restPosition;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (MobileInputRouter.Instance != null) MobileInputRouter.Instance.SetJoystick(Vector2.zero);
        }

        private bool ToLocal(PointerEventData e, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out local);
    }
}
