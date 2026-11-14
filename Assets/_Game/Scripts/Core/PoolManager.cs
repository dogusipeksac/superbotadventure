using System.Collections.Generic;
using UnityEngine;

namespace SuperBot.Core
{
    /// <summary>Prefab-keyed pools. Spawn/Despawn instead of Instantiate/Destroy for frequent objects.</summary>
    public sealed class PoolManager : MonoBehaviour
    {
        private readonly Dictionary<int, ObjectPool<Component>> poolsByPrefab = new Dictionary<int, ObjectPool<Component>>();
        private readonly Dictionary<int, ObjectPool<Component>> poolsByInstance = new Dictionary<int, ObjectPool<Component>>();

        private void Awake() => ServiceLocator.Register(this);
        private void OnDestroy() => ServiceLocator.Unregister(this);

        public void Prewarm(Component prefab, int count)
        {
            if (prefab == null) return;
            int id = prefab.GetInstanceID();
            if (!poolsByPrefab.ContainsKey(id))
                poolsByPrefab[id] = new ObjectPool<Component>(prefab, transform, count);
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null) return null;
            int id = prefab.GetInstanceID();
            if (!poolsByPrefab.TryGetValue(id, out var pool))
            {
                pool = new ObjectPool<Component>(prefab, transform, 0);
                poolsByPrefab[id] = pool;
            }
            var obj = (T)pool.Get(position, rotation);
            poolsByInstance[obj.GetInstanceID()] = pool;
            return obj;
        }

        /// <returns>false if the object did not come from a pool (caller should disable/destroy it).</returns>
        public bool Despawn(Component obj)
        {
            if (obj == null) return false;
            if (!poolsByInstance.TryGetValue(obj.GetInstanceID(), out var pool)) return false;
            pool.Release(obj);
            return true;
        }

        /// <summary>Despawns via the registered PoolManager, or disables the object when it is not pooled.</summary>
        public static void DespawnOrDisable(Component obj)
        {
            if (obj == null) return;
            if (ServiceLocator.TryGet(out PoolManager pm) && pm.Despawn(obj)) return;
            obj.gameObject.SetActive(false);
        }

        public static T SpawnStatic<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null) return null;
            if (ServiceLocator.TryGet(out PoolManager pm)) return pm.Spawn(prefab, position, rotation);
            return Instantiate(prefab, position, rotation);
        }
    }
}
