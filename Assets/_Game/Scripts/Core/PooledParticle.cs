using UnityEngine;

namespace SuperBot.Core
{
    /// <summary>Plays on enable and returns itself to the pool when the particle system stops.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledParticle : MonoBehaviour
    {
        private ParticleSystem ps;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Callback;
            main.playOnAwake = false;
            main.loop = false;
        }

        private void OnEnable()
        {
            ps.Clear(true);
            ps.Play(true);
        }

        private void OnParticleSystemStopped()
        {
            if (ServiceLocator.TryGet(out PoolManager pm) && pm.Despawn(this)) return;
            Destroy(gameObject);
        }
    }
}
