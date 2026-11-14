using System;
using SuperBot.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SuperBot.UI
{
    public sealed class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Image swatch;
        [SerializeField] private Image icon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button button;
        [SerializeField] private Image background;

        private Action onClick;

        private void Awake() => UiSfx.Bind(button, () => onClick?.Invoke());

        public void Bind(CosmeticItem item, string status, bool interactable, bool highlighted, Action click)
        {
            onClick = click;
            if (swatch != null) swatch.color = item.SwatchColor;
            if (icon != null)
            {
                icon.sprite = item.icon;
                icon.enabled = item.icon != null;
            }
            if (nameText != null) nameText.text = item.displayName;
            if (statusText != null) statusText.text = status;
            if (button != null) button.interactable = interactable;
            if (background != null) background.color = highlighted ? new Color(0.3f, 0.85f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.9f);
        }
    }
}
