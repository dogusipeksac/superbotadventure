using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperBot.Core
{
    /// <summary>Minimal global service registry. Cleared on every play-mode start.</summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Services.Clear();

        public static void Register<T>(T service) where T : class => Services[typeof(T)] = service;

        public static void Unregister<T>(T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var current) && ReferenceEquals(current, service))
                Services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (TryGet(out T service)) return service;
            Debug.LogError($"[ServiceLocator] Service not registered: {typeof(T).Name}");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var obj) && obj is T typed)
            {
                // Destroyed UnityEngine.Objects compare equal to null.
                if (!(typed is UnityEngine.Object uo) || uo != null)
                {
                    service = typed;
                    return true;
                }
            }
            service = null;
            return false;
        }

        public static void Clear() => Services.Clear();
    }
}
