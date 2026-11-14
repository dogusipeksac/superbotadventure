using System.Collections;
using SuperBot.Audio;
using SuperBot.Core;
using SuperBot.World;
using UnityEngine;

namespace SuperBot.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 3;
        [SerializeField] private float invulnerableTime = 1f;
        [SerializeField] private float respawnDelay = 1f;
        [SerializeField] private Transform visualRoot;

        public int Hp { get; private set; }
        public int MaxHp => maxHp;
        public bool IsDead { get; private set; }
        public PlayerMotor Motor { get; private set; }

        private float invulnerableUntil;
        private Renderer[] renderers;
        private bool blinking;

        private void Awake()
        {
            Motor = GetComponent<PlayerMotor>();
            Hp = maxHp;
            renderers = (visualRoot != null ? visualRoot : transform).GetComponentsInChildren<Renderer>(true);
            ServiceLocator.Register(this);
        }

        private void OnDestroy() => ServiceLocator.Unregister(this);

        private void Start() => GameEvents.RaisePlayerHealthChanged(Hp, maxHp);

        public void TakeHit(Vector3 source)
        {
            if (IsDead || Time.time < invulnerableUntil || Motor.IsDashing) return;
            Hp--;
            invulnerableUntil = Time.time + invulnerableTime;
            GameEvents.RaisePlayerHealthChanged(Hp, maxHp);
            Motor.Knockback(transform.position - source, 6f, 7f);
            Haptics()?.Medium();
            AudioService.Play(SfxId.Hurt);
            if (Hp <= 0) Kill();
            else StartCoroutine(Blink());
        }

        public void Kill()
        {
            if (IsDead) return;
            IsDead = true;
            Hp = 0;
            Motor.InputEnabled = false;
            GameEvents.RaisePlayerHealthChanged(Hp, maxHp);
            GameEvents.RaisePlayerDied();
            Haptics()?.Heavy();
            AudioService.Play(SfxId.Die);
            if (ServiceLocator.TryGet(out IDeathHandler handler)) handler.HandlePlayerDeath();
            else StartCoroutine(RespawnAfterDelay());
        }

        /// <summary>Respawn at the active checkpoint (or default spawn).</summary>
        public void RespawnAtCheckpoint()
        {
            if (ServiceLocator.TryGet(out RespawnService respawn)) respawn.Respawn(this);
            else Revive(transform.position, transform.rotation);
        }

        /// <summary>Rewarded-ad revive: back on the last safe ground with full HP.</summary>
        public void ReviveInPlace() => Revive(Motor.LastSafePosition + Vector3.up * 0.2f, transform.rotation);

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            RespawnAtCheckpoint();
        }

        /// <summary>Called by RespawnService.</summary>
        public void Revive(Vector3 position, Quaternion rotation)
        {
            Motor.Teleport(position, rotation);
            Hp = maxHp;
            IsDead = false;
            Motor.InputEnabled = true;
            invulnerableUntil = Time.time + invulnerableTime;
            GameEvents.RaisePlayerHealthChanged(Hp, maxHp);
            GameEvents.RaisePlayerRespawned();
            StartCoroutine(Blink());
        }

        private IEnumerator Blink()
        {
            if (blinking) yield break;
            blinking = true;
            bool visible = true;
            while (Time.time < invulnerableUntil)
            {
                visible = !visible;
                SetVisible(visible);
                yield return new WaitForSeconds(0.08f);
            }
            SetVisible(true);
            blinking = false;
        }

        private void SetVisible(bool visible)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = visible;
        }

        private static HapticManager Haptics() => ServiceLocator.TryGet(out HapticManager h) ? h : null;
    }
}
