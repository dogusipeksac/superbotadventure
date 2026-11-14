using SuperBot.Core;
using SuperBot.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SuperBot.UI
{
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private Text boltText;
        [SerializeField] private Image[] coreIcons;
        [SerializeField] private Image[] hpIcons;
        [SerializeField] private Color coreOn = new Color(1f, 0.8f, 0.1f);
        [SerializeField] private Color coreOff = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color hpOn = new Color(0.3f, 0.9f, 1f);
        [SerializeField] private Color hpOff = new Color(1f, 1f, 1f, 0.2f);

        private int shownBolts = -1;

        private void OnEnable()
        {
            GameEvents.OnBoltsChanged += SetBolts;
            GameEvents.OnGoldenCoreCollected += OnCore;
            GameEvents.OnPlayerHealthChanged += SetHealth;
        }

        private void OnDisable()
        {
            GameEvents.OnBoltsChanged -= SetBolts;
            GameEvents.OnGoldenCoreCollected -= OnCore;
            GameEvents.OnPlayerHealthChanged -= SetHealth;
        }

        private void Start()
        {
            if (ServiceLocator.TryGet(out EconomyService economy)) SetBolts(economy.Bolts);
            RefreshCores();
        }

        private void SetBolts(int total)
        {
            if (boltText == null || total == shownBolts) return;
            shownBolts = total;
            boltText.text = total.ToString();
        }

        private void OnCore(string _) => RefreshCores();

        private void RefreshCores()
        {
            if (coreIcons == null) return;
            int count = ServiceLocator.TryGet(out SaveService save) ? save.Data.collectedCores.Count : 0;
            for (int i = 0; i < coreIcons.Length; i++)
                if (coreIcons[i] != null) coreIcons[i].color = i < count ? coreOn : coreOff;
        }

        private void SetHealth(int current, int max)
        {
            if (hpIcons == null) return;
            for (int i = 0; i < hpIcons.Length; i++)
            {
                if (hpIcons[i] == null) continue;
                hpIcons[i].gameObject.SetActive(i < max);
                hpIcons[i].color = i < current ? hpOn : hpOff;
            }
        }
    }
}
