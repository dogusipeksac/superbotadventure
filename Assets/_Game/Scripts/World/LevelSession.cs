using SuperBot.Core;
using SuperBot.Player;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.World
{
    /// <summary>Per gameplay scene: counts bolts earned this visit and completes the level when all its cores are collected.</summary>
    public sealed class LevelSession : MonoBehaviour
    {
        [SerializeField] private string levelId = "island01";
        [SerializeField] private string[] coreIds = new string[0];
        [SerializeField] private bool isHub;

        public string LevelId => levelId;
        public int SessionBolts { get; private set; }
        public bool Completed { get; private set; }
        public bool IsHub => isHub;
        public string[] CoreIds => coreIds;

        private int lastTotal = -1;

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
            PauseService.GameplayActive = false;
        }

        private void OnEnable()
        {
            GameEvents.OnBoltsChanged += OnBolts;
            GameEvents.OnGoldenCoreCollected += OnCore;
        }

        private void OnDisable()
        {
            GameEvents.OnBoltsChanged -= OnBolts;
            GameEvents.OnGoldenCoreCollected -= OnCore;
        }

        private void Start()
        {
            PauseService.GameplayActive = true;
            if (ServiceLocator.TryGet(out EconomyService economy)) lastTotal = economy.Bolts;
        }

        private void OnBolts(int total)
        {
            if (lastTotal >= 0 && total > lastTotal && !Completed) SessionBolts += total - lastTotal;
            lastTotal = total;
        }

        private void OnCore(string coreId)
        {
            if (Completed || isHub || coreIds == null || coreIds.Length == 0) return;
            if (!ServiceLocator.TryGet(out SaveService save)) return;
            foreach (var id in coreIds)
                if (!save.Data.collectedCores.Contains(id)) return;

            Completed = true;
            if (ServiceLocator.TryGet(out PlayerHealth player)) player.Motor.InputEnabled = false;
            GameEvents.RaiseLevelCompleted(levelId);
        }

        public int CollectedCoreCount()
        {
            if (coreIds == null || !ServiceLocator.TryGet(out SaveService save)) return 0;
            int n = 0;
            foreach (var id in coreIds)
                if (save.Data.collectedCores.Contains(id)) n++;
            return n;
        }
    }
}
