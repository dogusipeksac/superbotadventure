using SuperBot.Save;
using UnityEngine;

namespace SuperBot.Core
{
    public sealed class HapticManager
    {
        private readonly SaveService save;
        private float lastHeavyTime = -10f;

        public HapticManager(SaveService save) => this.save = save;

        private bool Enabled => save == null || save.Data.hapticsEnabled;

        // TODO v1.1: native light/medium impact haptics (iOS UIImpactFeedbackGenerator, Android VibrationEffect).
        public void Light() { if (Enabled) Log("Light"); }

        public void Medium() { if (Enabled) Log("Medium"); }

        public void Heavy()
        {
            if (!Enabled) return;
            // Handheld.Vibrate is long (~400ms); throttle it.
            if (Time.unscaledTime - lastHeavyTime < 0.5f) return;
            lastHeavyTime = Time.unscaledTime;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#else
            Log("Heavy");
#endif
        }

        [System.Diagnostics.Conditional("SUPERBOT_HAPTIC_LOG")]
        private static void Log(string strength) => Debug.Log($"[Haptic] {strength}");
    }
}
