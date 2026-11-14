using SuperBot.Monetization;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.Core
{
    /// <summary>Persistent root. Place one in every playable scene; duplicates destroy themselves.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class Bootstrapper : MonoBehaviour
    {
        private static Bootstrapper instance;

        public static Bootstrapper Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            var save = new SaveService();
            save.Load();
            ServiceLocator.Register(save);
            ServiceLocator.Register(new EconomyService(save));
            ServiceLocator.Register(new HapticManager(save));

            IMonetizationService monetization = new MonetizationServiceStub(save);
            ServiceLocator.Register(monetization);
            monetization.Initialize(null);

            if (GetComponent<SaveAutosaver>() == null) gameObject.AddComponent<SaveAutosaver>();
            if (GetComponent<PoolManager>() == null) gameObject.AddComponent<PoolManager>();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
