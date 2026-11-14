using SuperBot.Core;
using SuperBot.Player;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class LevelPortal : MonoBehaviour
    {
        [SerializeField] private string targetScene = SceneNames.Island01;
        [SerializeField] private string requiredCoreId;
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private GameObject openVisual;

        private bool used;

        private bool Unlocked =>
            string.IsNullOrEmpty(requiredCoreId) ||
            (ServiceLocator.TryGet(out SaveService save) && save.Data.collectedCores.Contains(requiredCoreId));

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void Start()
        {
            bool open = Unlocked;
            if (lockedVisual != null) lockedVisual.SetActive(!open);
            if (openVisual != null) openVisual.SetActive(open);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (used || !Unlocked || !other.TryGetComponent(out PlayerHealth player) || player.IsDead) return;
            used = true;
            player.Motor.InputEnabled = false;
            if (ServiceLocator.TryGet(out HapticManager h)) h.Medium();
            SceneLoader.LoadScene(targetScene);
        }
    }
}
