using SuperBot.Player;
using UnityEngine;

namespace SuperBot.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class KillZone : MonoBehaviour
    {
        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerHealth health)) health.Kill();
        }
    }
}
