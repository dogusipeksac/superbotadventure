using System.Collections.Generic;
using UnityEngine;

namespace SuperBot.Core
{
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Stack<T> inactive = new Stack<T>();

        public T Prefab => prefab;

        public ObjectPool(T prefab, Transform parent, int prewarm)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                var obj = Object.Instantiate(prefab, parent);
                obj.gameObject.SetActive(false);
                inactive.Push(obj);
            }
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = null;
            while (inactive.Count > 0 && obj == null) obj = inactive.Pop();
            if (obj == null) obj = Object.Instantiate(prefab, position, rotation, parent);
            else obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
            if (obj.transform.parent != parent) obj.transform.SetParent(parent, false);
            inactive.Push(obj);
        }
    }
}
