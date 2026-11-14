using SuperBot.Audio;
using SuperBot.Core;
using SuperBot.Player;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.Collectibles
{
    /// <summary>
    /// Currency pickup. Level-placed bolts may have a persistentId (collected once forever);
    /// pooled bolts (from crates/enemies) leave it empty and can pop out with <see cref="Pop"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Bolt : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private string persistentId;
        [SerializeField] private float spinSpeed = 180f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private PooledParticle collectFx;
        [SerializeField] private LayerMask groundMask = 1;
        [SerializeField] private float popGravity = -25f;
        [SerializeField] private float popPickupDelay = 0.35f;

        private Vector3 basePosition;
        private float phase;
        private bool popping;
        private Vector3 popVelocity;
        private float collectableAt;

        public string PersistentId { get => persistentId; set => persistentId = value; }

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnEnable()
        {
            basePosition = transform.position;
            phase = Random.value * 10f;
            popping = false;
            collectableAt = 0f;
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(persistentId) && ServiceLocator.TryGet(out SaveService save) &&
                save.Data.collectedBoltIds.Contains(persistentId))
                gameObject.SetActive(false);
        }

        /// <summary>Launch out of a crate/enemy; falls with gravity until it hits the ground.</summary>
        public void Pop(Vector3 velocity)
        {
            popping = true;
            popVelocity = velocity;
            collectableAt = Time.time + popPickupDelay;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.Rotate(0f, spinSpeed * dt, 0f, Space.World);

            if (popping)
            {
                popVelocity.y += popGravity * dt;
                Vector3 pos = transform.position;
                Vector3 next = pos + popVelocity * dt;
                if (popVelocity.y < 0f &&
                    Physics.Raycast(pos + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.1f - popVelocity.y * dt + 0.35f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    next = hit.point + Vector3.up * 0.35f;
                    popping = false;
                    basePosition = next;
                }
                transform.position = next;
                if (next.y < -50f) PoolManager.DespawnOrDisable(this);
                return;
            }

            transform.position = basePosition + Vector3.up * (Mathf.Sin((Time.time + phase) * bobSpeed) * bobHeight);
        }

        private void OnTriggerEnter(Collider other) => TryCollect(other);
        private void OnTriggerStay(Collider other) => TryCollect(other);

        private void TryCollect(Collider other)
        {
            if (Time.time < collectableAt || !other.TryGetComponent(out PlayerHealth player) || player.IsDead) return;

            if (ServiceLocator.TryGet(out EconomyService economy)) economy.AddBolts(value);
            if (!string.IsNullOrEmpty(persistentId) && ServiceLocator.TryGet(out SaveService save) &&
                !save.Data.collectedBoltIds.Contains(persistentId))
            {
                save.Data.collectedBoltIds.Add(persistentId);
                save.MarkDirty();
            }
            if (collectFx != null) PoolManager.SpawnStatic(collectFx, transform.position, Quaternion.identity);
            if (ServiceLocator.TryGet(out HapticManager h)) h.Light();
            AudioService.Play(SfxId.Bolt);
            PoolManager.DespawnOrDisable(this);
        }
    }
}
