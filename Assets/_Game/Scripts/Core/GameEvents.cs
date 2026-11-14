using System;

namespace SuperBot.Core
{
    /// <summary>Global gameplay events. Subscribers must unsubscribe in OnDisable/OnDestroy.</summary>
    public static class GameEvents
    {
        public static event Action<int> OnBoltsChanged;
        public static event Action<string> OnGoldenCoreCollected;
        public static event Action OnPlayerDied;
        public static event Action OnPlayerRespawned;
        public static event Action<string> OnCheckpointReached;
        public static event Action<float> OnPlayerLanded;
        public static event Action<int, int> OnPlayerHealthChanged;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnBoltsChanged = null;
            OnGoldenCoreCollected = null;
            OnPlayerDied = null;
            OnPlayerRespawned = null;
            OnCheckpointReached = null;
            OnPlayerLanded = null;
            OnPlayerHealthChanged = null;
        }

        public static void RaiseBoltsChanged(int total) => OnBoltsChanged?.Invoke(total);
        public static void RaiseGoldenCoreCollected(string coreId) => OnGoldenCoreCollected?.Invoke(coreId);
        public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
        public static void RaisePlayerRespawned() => OnPlayerRespawned?.Invoke();
        public static void RaiseCheckpointReached(string checkpointId) => OnCheckpointReached?.Invoke(checkpointId);
        public static void RaisePlayerLanded(float impactSpeed) => OnPlayerLanded?.Invoke(impactSpeed);
        public static void RaisePlayerHealthChanged(int current, int max) => OnPlayerHealthChanged?.Invoke(current, max);
    }
}
