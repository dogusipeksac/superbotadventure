using System;

namespace SuperBot.Monetization
{
    public enum RewardedPlacement
    {
        Revive,
        DoubleBolts
    }

    /// <summary>
    /// Single integration point for ads, IAP and iOS ATT. v1.0 ships with <see cref="MonetizationServiceStub"/>;
    /// v1.1 swaps in an AdMob + Unity IAP implementation without touching gameplay code.
    /// </summary>
    public interface IMonetizationService
    {
        bool IsInitialized { get; }
        void Initialize(Action onComplete);

        /// <summary>iOS App Tracking Transparency prompt. Result: tracking authorized.</summary>
        void RequestTrackingConsent(Action<bool> onResult);

        bool IsRewardedReady(RewardedPlacement placement);
        /// <summary>Callback argument: true if the reward must be granted.</summary>
        void ShowRewarded(RewardedPlacement placement, Action<bool> onFinished);

        void Purchase(string productId, Action<bool> onResult);
        bool IsOwned(string productId);
        void RestorePurchases(Action<bool> onResult);
    }
}
