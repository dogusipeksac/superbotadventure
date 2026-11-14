using SuperBot.Audio;
using SuperBot.Core;
using SuperBot.Player;
using UnityEngine;

namespace SuperBot.World
{
    /// <summary>Trigger volume on top of a bouncy prop. Launches the player upward.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class JumpPad : MonoBehaviour
    {
        [SerializeField] private float launchVelocity = 18f;
        [SerializeField] private Transform visual;
        [SerializeField] private float squashTime = 0.25f;

        private Vector3 visualBaseScale = Vector3.one;
        private float squashStart = -10f;
        private float lastLaunch = -10f;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (visual != null) visualBaseScale = visual.localScale;
        }

        private void OnTriggerEnter(Collider other) => TryLaunch(other);
        private void OnTriggerStay(Collider other) => TryLaunch(other);

        private void TryLaunch(Collider other)
        {
            if (Time.time - lastLaunch < 0.2f) return;
            if (!other.TryGetComponent(out PlayerMotor motor) || motor.Velocity.y > 1f) return;
            lastLaunch = Time.time;
            motor.Launch(launchVelocity);
            squashStart = Time.time;
            if (ServiceLocator.TryGet(out HapticManager h)) h.Medium();
            AudioService.Play(SfxId.JumpPad);
        }

        private void Update()
        {
            if (visual == null) return;
            float t = (Time.time - squashStart) / squashTime;
            if (t >= 1f)
            {
                if (visual.localScale != visualBaseScale) visual.localScale = visualBaseScale;
                return;
            }
            float s = Mathf.Sin(t * Mathf.PI) * (1f - t) * 0.35f;
            visual.localScale = Vector3.Scale(visualBaseScale, new Vector3(1f + s * 0.5f, 1f - s, 1f + s * 0.5f));
        }
    }
}
