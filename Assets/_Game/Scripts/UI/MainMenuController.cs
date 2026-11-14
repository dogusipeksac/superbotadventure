using SuperBot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SuperBot.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsPanel settings;
        [SerializeField] private Text versionText;
        [SerializeField] private GameObject mainButtons;

        private void Awake()
        {
            UiSfx.Bind(playButton, () => SceneLoader.LoadScene(SceneNames.Hub));
            UiSfx.Bind(settingsButton, () =>
            {
                if (settings == null) return;
                if (mainButtons != null) mainButtons.SetActive(false);
                settings.Open();
            });
            if (settings != null) settings.Closed += () => { if (mainButtons != null) mainButtons.SetActive(true); };
            if (versionText != null) versionText.text = "v" + Application.version;
        }
    }
}
