using SuperBot.Controls;
using UnityEngine;

namespace SuperBot.Core
{
    /// <summary>Global pause (timeScale, audio, input). Auto-pauses gameplay when the app goes to background.</summary>
    public sealed class PauseService : MonoBehaviour
    {
        public bool IsPaused { get; private set; }

        /// <summary>Set by gameplay scenes (LevelSession / Hub) so menus are not auto-paused.</summary>
        public static bool GameplayActive { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => GameplayActive = false;

        private void Awake() => ServiceLocator.Register(this);
        private void OnDestroy() => ServiceLocator.Unregister(this);

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (MobileInputRouter.Instance != null) MobileInputRouter.Instance.enabled = !paused;
            GameEvents.RaiseGamePaused(paused);
        }

        public void Toggle() => SetPaused(!IsPaused);

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && GameplayActive) SetPaused(true);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if !UNITY_EDITOR
            if (!hasFocus && GameplayActive) SetPaused(true);
#endif
        }
    }
}
