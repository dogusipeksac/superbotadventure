using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperBot.Controls
{
    /// <summary>Transparent Image on the right half (behind the buttons). Finger drag rotates the camera.</summary>
    public sealed class LookTouchZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float ReferenceHeight = 1080f;
        private int activePointer = int.MinValue;

        private void OnDisable() => activePointer = int.MinValue;

        public void OnPointerDown(PointerEventData e)
        {
            if (activePointer == int.MinValue) activePointer = e.pointerId;
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != activePointer || MobileInputRouter.Instance == null) return;
            // Normalize to a 1080p-tall screen so sensitivity is the same on every device.
            float scale = ReferenceHeight / Mathf.Max(1, Screen.height);
            MobileInputRouter.Instance.AddLook(e.delta * scale);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == activePointer) activePointer = int.MinValue;
        }
    }
}
