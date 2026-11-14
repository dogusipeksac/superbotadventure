using System;
using SuperBot.Core;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.Monetization
{
    /// <summary>Test/MVP implementation: no SDKs, every ad "succeeds", purchases unlock locally.</summary>
    public sealed class MonetizationServiceStub : IMonetizationService
    {
        private readonly SaveService save;

        public MonetizationServiceStub(SaveService save) => this.save = save;

        public bool IsInitialized { get; private set; }

        public void Initialize(Action onComplete)
        {
            IsInitialized = true;
            onComplete?.Invoke();
        }

        public void RequestTrackingConsent(Action<bool> onResult)
        {
            save.Data.attConsentAsked = true;
            save.MarkDirty();
            Debug.Log("[MonetizationStub] ATT consent requested -> not authorized (stub)");
            onResult?.Invoke(false);
        }

        public bool IsRewardedReady(RewardedPlacement placement) => IsInitialized;

        public void ShowRewarded(RewardedPlacement placement, Action<bool> onFinished)
        {
            Debug.Log($"[MonetizationStub] Rewarded '{placement}' shown -> reward granted");
            CoroutineRunner.NextFrame(() => onFinished?.Invoke(true));
        }

        public void Purchase(string productId, Action<bool> onResult)
        {
            if (string.IsNullOrEmpty(productId))
            {
                onResult?.Invoke(false);
                return;
            }
            if (!save.Data.unlockedCosmetics.Contains(productId))
            {
                save.Data.unlockedCosmetics.Add(productId);
                save.MarkDirty();
            }
            Debug.Log($"[MonetizationStub] Purchase '{productId}' -> success");
            CoroutineRunner.NextFrame(() => onResult?.Invoke(true));
        }

        public bool IsOwned(string productId) => save.Data.unlockedCosmetics.Contains(productId);

        public void RestorePurchases(Action<bool> onResult)
        {
            Debug.Log("[MonetizationStub] Restore purchases -> nothing to restore");
            onResult?.Invoke(true);
        }
    }
}
