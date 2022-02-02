using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    [ExecuteAlways]
    public class CapsuleIndirectShadowSettings : MonoBehaviour
    {
        [Range(0.1f, 90.0f)]
        public float angle = 45.0f;

        private static CapsuleIndirectShadowSettings s_Instance = null;

        public static CapsuleIndirectShadowSettings instance {  get { return s_Instance; } }

        private void OnEnable()
        {
            if (s_Instance == null)
                s_Instance = this;
            else
                Debug.Log("Multiple CapsuleIndirectShadowSettings instances, some will be ignored!");
        }

        private void OnDisable()
        {
            if (s_Instance == this)
                s_Instance = null;
        }
    }
}
