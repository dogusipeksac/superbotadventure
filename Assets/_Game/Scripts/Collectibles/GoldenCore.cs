using SuperBot.Core;
using SuperBot.Player;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.Collectibles
{
    [RequireComponent(typeof(Collider))]
    public sealed class GoldenCore : MonoBehaviour
    {
        [SerializeField] private string coreId;
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private float bobHeight = 0.25f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private PooledParticle collectFx;

        public string CoreId { get => coreId; set => coreId = value; }

        private Vector3 basePosition;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            basePosition = transform.position;
            if (string.IsNullOrEmpty(coreId)) Debug.LogWarning($"[GoldenCore] '{name}' has no coreId", this);
        }

        private void Start()
        {
            if (ServiceLocator.TryGet(out SaveService save) && save.Data.collectedCores.Contains(coreId))
                gameObject.SetActive(false);
        }

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out PlayerHealth player) || player.IsDead) return;

            if (ServiceLocator.TryGet(out SaveService save))
            {
                if (!save.Data.collectedCores.Contains(coreId)) save.Data.collectedCores.Add(coreId);
                save.Save();
            }
            GameEvents.RaiseGoldenCoreCollected(coreId);
            if (collectFx != null) PoolManager.SpawnStatic(collectFx, transform.position, Quaternion.identity);
            if (ServiceLocator.TryGet(out HapticManager h)) h.Heavy();
            gameObject.SetActive(false);
        }
    }
}
