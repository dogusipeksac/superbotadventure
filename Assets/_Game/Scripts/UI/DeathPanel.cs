using System.Collections;
using SuperBot.Core;
using SuperBot.Monetization;
using SuperBot.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SuperBot.UI
{
    /// <summary>After death: revive in place with a rewarded ad (once per checkpoint) or go back to the checkpoint.</summary>
    public sealed class DeathPanel : MonoBehaviour, IDeathHandler
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button reviveAdButton;
        [SerializeField] private Button checkpointButton;
        [SerializeField] private float showDelay = 0.6f;

        private bool adReviveUsed;
        private bool busy;

        private void Awake()
        {
            ServiceLocator.Register<IDeathHandler>(this);
            UiSfx.Bind(reviveAdButton, ReviveWithAd);
            UiSfx.Bind(checkpointButton, ToCheckpoint);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy() => ServiceLocator.Unregister<IDeathHandler>(this);
        private void OnEnable() => GameEvents.OnCheckpointReached += ResetAd;
        private void OnDisable() => GameEvents.OnCheckpointReached -= ResetAd;

        private void ResetAd(string _) => adReviveUsed = false;

        public void HandlePlayerDeath() => StartCoroutine(ShowAfterDelay());

        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSecondsRealtime(showDelay);
            bool adReady = !adReviveUsed && ServiceLocator.TryGet(out IMonetizationService m) && m.IsRewardedReady(RewardedPlacement.Revive);
            if (reviveAdButton != null) reviveAdButton.gameObject.SetActive(adReady);
            busy = false;
            panel.SetActive(true);
        }

        private void ReviveWithAd()
        {
            if (busy || !ServiceLocator.TryGet(out IMonetizationService m)) return;
            busy = true;
            m.ShowRewarded(RewardedPlacement.Revive, granted =>
            {
                busy = false;
                if (!granted) return;
                adReviveUsed = true;
                panel.SetActive(false);
                if (ServiceLocator.TryGet(out PlayerHealth player)) player.ReviveInPlace();
            });
        }

        private void ToCheckpoint()
        {
            if (busy) return;
            panel.SetActive(false);
            adReviveUsed = false;
            if (ServiceLocator.TryGet(out PlayerHealth player)) player.RespawnAtCheckpoint();
        }
    }
}
