using SuperBot.Core;
using SuperBot.Player;
using UnityEngine;

namespace SuperBot.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private string checkpointId;
        [SerializeField] private Transform flagVisual;
        [SerializeField] private float loweredOffset = -3f;
        [SerializeField] private float raiseSpeed = 6f;
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.2f, 2f);

        public string CheckpointId => checkpointId;
        public Vector3 SpawnPosition => transform.TransformPoint(spawnOffset);

        private bool active;
        private Vector3 flagRaisedLocal;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (flagVisual != null)
            {
                flagRaisedLocal = flagVisual.localPosition;
                flagVisual.localPosition = flagRaisedLocal + Vector3.up * loweredOffset;
            }
            if (ServiceLocator.TryGet(out RespawnService respawn)) respawn.Register(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (active || !other.TryGetComponent(out PlayerHealth _)) return;
            active = true;
            if (ServiceLocator.TryGet(out RespawnService respawn)) respawn.SetCheckpoint(this);
            GameEvents.RaiseCheckpointReached(checkpointId);
            if (ServiceLocator.TryGet(out HapticManager h)) h.Medium();
        }

        public void ActivateSilently()
        {
            active = true;
            if (flagVisual != null) flagVisual.localPosition = flagRaisedLocal;
        }

        public void Deactivate() => active = false;

        private void Update()
        {
            if (flagVisual == null) return;
            Vector3 target = active ? flagRaisedLocal : flagRaisedLocal + Vector3.up * loweredOffset;
            if (flagVisual.localPosition != target)
                flagVisual.localPosition = Vector3.MoveTowards(flagVisual.localPosition, target, raiseSpeed * Time.deltaTime);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(SpawnPosition, 0.3f);
        }
#endif
    }
}
