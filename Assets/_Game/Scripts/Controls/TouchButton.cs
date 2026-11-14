using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperBot.Controls
{
    public sealed class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private ButtonAction action;
        [SerializeField] private float pressedScale = 0.9f;

        private Vector3 baseScale;
        private int activePointer = int.MinValue;

        public ButtonAction Action { get => action; set => action = value; }

        private void Awake() => baseScale = transform.localScale;

        private void OnDisable()
        {
            if (activePointer == int.MinValue) return;
            activePointer = int.MinValue;
            transform.localScale = baseScale;
            if (MobileInputRouter.Instance != null) MobileInputRouter.Instance.Release(action);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (activePointer != int.MinValue) return;
            activePointer = e.pointerId;
            transform.localScale = baseScale * pressedScale;
            if (MobileInputRouter.Instance != null) MobileInputRouter.Instance.Press(action);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != activePointer) return;
            activePointer = int.MinValue;
            transform.localScale = baseScale;
            if (MobileInputRouter.Instance != null) MobileInputRouter.Instance.Release(action);
        }
    }
}
