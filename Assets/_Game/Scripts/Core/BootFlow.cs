using SuperBot.Monetization;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.Core
{
    /// <summary>Boot scene: first-launch iOS tracking consent (through IMonetizationService), then the main menu.</summary>
    public sealed class BootFlow : MonoBehaviour
    {
        private void Start()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (ServiceLocator.TryGet(out SaveService save) && !save.Data.attConsentAsked &&
                ServiceLocator.TryGet(out IMonetizationService monetization))
            {
                monetization.RequestTrackingConsent(_ => Continue());
                return;
            }
#endif
            Continue();
        }

        private static void Continue() => SceneLoader.LoadScene(SceneNames.MainMenu);
    }
}
