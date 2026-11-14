using SuperBot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SuperBot.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private string exitScene = SceneNames.Hub;
        [SerializeField] private SettingsPanel settings;

        private void Awake()
        {
            UiSfx.Bind(pauseButton, () => SetPaused(true));
            UiSfx.Bind(resumeButton, () => SetPaused(false));
            UiSfx.Bind(settingsButton, () =>
            {
                if (settings == null) return;
                panel.SetActive(false);
                settings.Open();
            });
            UiSfx.Bind(exitButton, () => SceneLoader.LoadScene(exitScene));
            if (settings != null) settings.Closed += () => { if (IsPaused) panel.SetActive(true); };
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable() => GameEvents.OnGamePaused += OnPaused;
        private void OnDisable() => GameEvents.OnGamePaused -= OnPaused;

        private static bool IsPaused => ServiceLocator.TryGet(out PauseService p) && p.IsPaused;

        private static void SetPaused(bool paused)
        {
            if (ServiceLocator.TryGet(out PauseService p)) p.SetPaused(paused);
        }

        private void OnPaused(bool paused)
        {
            if (panel != null) panel.SetActive(paused);
            if (!paused && settings != null) settings.Close();
            if (pauseButton != null) pauseButton.gameObject.SetActive(!paused);
        }
    }
}
