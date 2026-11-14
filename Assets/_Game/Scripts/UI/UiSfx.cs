using SuperBot.Audio;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SuperBot.UI
{
    public static class UiSfx
    {
        /// <summary>Adds a click sound + the given action to a button.</summary>
        public static void Bind(Button button, UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(() =>
            {
                AudioService.Play(SfxId.UiClick);
                action?.Invoke();
            });
        }
    }
}
