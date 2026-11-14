using SuperBot.Collectibles;
using SuperBot.Core;
using UnityEngine;

namespace SuperBot.World
{
    /// <summary>Breaks when spun or stomped and pops out bolts.</summary>
    public sealed class BreakableCrate : MonoBehaviour, IDamageable, IStompable
    {
        [SerializeField] private int boltCount = 3;
        [SerializeField] private Bolt boltPrefab;
        [SerializeField] private PooledParticle debrisPrefab;
        [SerializeField] private float stompBounce = 9f;

        private bool broken;

        public void TakeHit(Vector3 source) => Break();

        public float OnStomped(Vector3 stomperPosition)
        {
            if (broken) return 0f;
            Break();
            return stompBounce;
        }

        private void Break()
        {
            if (broken) return;
            broken = true;
            Vector3 center = transform.position + Vector3.up * 0.4f;

            if (debrisPrefab != null) PoolManager.SpawnStatic(debrisPrefab, center, Quaternion.identity);
            if (boltPrefab != null)
            {
                for (int i = 0; i < boltCount; i++)
                {
                    float angle = (i / (float)boltCount) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                    Vector3 v = new Vector3(Mathf.Cos(angle) * 2.5f, Random.Range(6f, 8f), Mathf.Sin(angle) * 2.5f);
                    Bolt bolt = PoolManager.SpawnStatic(boltPrefab, center, Quaternion.identity);
                    bolt.Pop(v);
                }
            }
            if (ServiceLocator.TryGet(out HapticManager h)) h.Light();
            gameObject.SetActive(false);
        }
    }
}
