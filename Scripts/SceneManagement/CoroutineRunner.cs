using System.Collections;
using System.Collections.Generic;
using JG.Tools;
using UnityEngine;


namespace JG.Tools { 
    /// <summary>
    /// Helper class to run coroutines on classes that do not derive from MonoBehaviour
    /// </summary>
    public class CoroutineRunner : Singleton<CoroutineRunner>
    {
        public static Coroutine StartCoroutine(IEnumerator coroutine)
        {
            if (Instance == null)
            {
                Logger.LogError("CoroutineRunner is not initialized. Ensure it's being called after scene load.");
                return null;
            }
            return ((MonoBehaviour)Instance).StartCoroutine(coroutine);
        }

        public static void StopCoroutine(Coroutine coroutine)
        {
            if (Instance == null)
            {
                Logger.LogError("CoroutineRunner is not initialized. Ensure it's being called after scene load.");
                return;
            }
            ((MonoBehaviour)Instance).StopCoroutine(coroutine);
        }

        public static void StopAllCoroutines()
        {
            if (Instance == null)
            {
                Logger.LogError("CoroutineRunner is not initialized. Ensure it's being called after scene load.");
                return;
            }
            ((MonoBehaviour)Instance).StopAllCoroutines();
        }
    }
}