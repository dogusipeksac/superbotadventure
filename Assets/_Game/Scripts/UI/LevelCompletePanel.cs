using System.Collections;
using SuperBot.Core;
using SuperBot.Monetization;
using SuperBot.World;
using UnityEngine;
using UnityEngine.UI;

namespace SuperBot.UI
{
    public sealed class LevelCompletePanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text boltsText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button doubleButton;
        [SerializeField] private float showDelay = 1.2f;
        [SerializeField] private string nextScene = SceneNames.Hub;

        private int sessionBolts;
        private bool done;

        private void Awake()
        {
            UiSfx.Bind(collectButton, Finish);
            UiSfx.Bind(doubleButton, Double);
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable() => GameEvents.OnLevelCompleted += OnCompleted;
        private void OnDisable() => GameEvents.OnLevelCompleted -= OnCompleted;

        private void OnCompleted(string levelId) => StartCoroutine(Show());

        private IEnumerator Show()
        {
            yield return new WaitForSecondsRealtime(showDelay);
            sessionBolts = ServiceLocator.TryGet(out LevelSession session) ? session.SessionBolts : 0;
            if (boltsText != null) boltsText.text = "+" + sessionBolts;
            bool adReady = sessionBolts > 0 && ServiceLocator.TryGet(out IMonetizationService m) && m.IsRewardedReady(RewardedPlacement.DoubleBolts);
            if (doubleButton != null) doubleButton.gameObject.SetActive(adReady);
            panel.SetActive(true);
        }

        private void Double()
        {
            if (done || !ServiceLocator.TryGet(out IMonetizationService m)) return;
            done = true;
            m.ShowRewarded(RewardedPlacement.DoubleBolts, granted =>
            {
                if (granted && ServiceLocator.TryGet(out EconomyService economy)) economy.AddBolts(sessionBolts);
                Leave();
            });
        }

        private void Finish()
        {
            if (done) return;
            done = true;
            Leave();
        }

        private void Leave() => SceneLoader.LoadScene(nextScene);
    }
}
