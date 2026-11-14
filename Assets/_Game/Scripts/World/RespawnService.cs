using System.Collections.Generic;
using SuperBot.Core;
using SuperBot.Player;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.World
{
    /// <summary>Scene-level: tracks the active checkpoint and respawns the player there.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class RespawnService : MonoBehaviour
    {
        [SerializeField] private Transform defaultSpawn;
        [SerializeField] private PlayerHealth player;

        private readonly Dictionary<string, Checkpoint> checkpoints = new Dictionary<string, Checkpoint>();
        private Checkpoint current;

        private void Awake() => ServiceLocator.Register(this);
        private void OnDestroy() => ServiceLocator.Unregister(this);

        private void Start()
        {
            // Resume at the last saved checkpoint of this scene, if any.
            if (ServiceLocator.TryGet(out SaveService save) &&
                checkpoints.TryGetValue(save.Data.lastCheckpointId, out var saved))
            {
                saved.ActivateSilently();
                current = saved;
                if (player != null) player.Motor.Teleport(saved.SpawnPosition, saved.transform.rotation);
            }
        }

        public void Register(Checkpoint checkpoint)
        {
            if (checkpoint != null && !string.IsNullOrEmpty(checkpoint.CheckpointId))
                checkpoints[checkpoint.CheckpointId] = checkpoint;
        }

        public void SetCheckpoint(Checkpoint checkpoint)
        {
            if (current != null && current != checkpoint) current.Deactivate();
            current = checkpoint;
            if (ServiceLocator.TryGet(out SaveService save))
            {
                save.Data.lastCheckpointId = checkpoint.CheckpointId;
                save.MarkDirty();
            }
        }

        public void Respawn(PlayerHealth target)
        {
            Vector3 pos;
            Quaternion rot;
            if (current != null)
            {
                pos = current.SpawnPosition;
                rot = current.transform.rotation;
            }
            else if (defaultSpawn != null)
            {
                pos = defaultSpawn.position;
                rot = defaultSpawn.rotation;
            }
            else
            {
                pos = Vector3.up;
                rot = Quaternion.identity;
            }
            target.Revive(pos, rot);
        }
    }
}
