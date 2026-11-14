using System;
using SuperBot.Audio;
using SuperBot.Core;
using SuperBot.Monetization;
using SuperBot.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SuperBot.UI
{
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Button restoreButton;
        [SerializeField] private Button privacyButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private string privacyPolicyUrl = "";

        public event Action Closed;

        private bool syncing;

        private void Awake()
        {
            if (root == null) root = gameObject;
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(v => Write(d => d.musicVolume = v));
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(v => Write(d => d.sfxVolume = v));
            if (hapticsToggle != null) hapticsToggle.onValueChanged.AddListener(v => Write(d => d.hapticsEnabled = v));
            UiSfx.Bind(restoreButton, () =>
            {
                if (ServiceLocator.TryGet(out IMonetizationService m)) m.RestorePurchases(null);
            });
            UiSfx.Bind(privacyButton, () => Application.OpenURL(privacyPolicyUrl));
            UiSfx.Bind(closeButton, Close);
            if (privacyButton != null) privacyButton.gameObject.SetActive(!string.IsNullOrEmpty(privacyPolicyUrl));
            root.SetActive(false);
        }

        public void Open()
        {
            syncing = true;
            if (ServiceLocator.TryGet(out SaveService save))
            {
                if (musicSlider != null) musicSlider.value = save.Data.musicVolume;
                if (sfxSlider != null) sfxSlider.value = save.Data.sfxVolume;
                if (hapticsToggle != null) hapticsToggle.isOn = save.Data.hapticsEnabled;
            }
            syncing = false;
            root.SetActive(true);
        }

        public void Close()
        {
            root.SetActive(false);
            Closed?.Invoke();
        }

        private void Write(Action<SaveData> change)
        {
            if (syncing || !ServiceLocator.TryGet(out SaveService save)) return;
            change(save.Data);
            save.MarkDirty();
            if (ServiceLocator.TryGet(out AudioService audio)) audio.ApplyVolumes();
        }
    }
}
