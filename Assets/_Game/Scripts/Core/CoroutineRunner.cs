using System;
using System.Collections;
using UnityEngine;

namespace SuperBot.Core
{
    /// <summary>Hidden persistent MonoBehaviour that lets plain C# services run coroutines.</summary>
    public sealed class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner instance;

        public static CoroutineRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[CoroutineRunner]") { hideFlags = HideFlags.HideInHierarchy };
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<CoroutineRunner>();
                }
                return instance;
            }
        }

        public static Coroutine Run(IEnumerator routine) => Instance.StartCoroutine(routine);

        public static void NextFrame(Action action) => Instance.StartCoroutine(NextFrameRoutine(action));

        public static void After(float seconds, Action action) => Instance.StartCoroutine(AfterRoutine(seconds, action));

        private static IEnumerator NextFrameRoutine(Action action)
        {
            yield return null;
            action?.Invoke();
        }

        private static IEnumerator AfterRoutine(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }
    }
}
