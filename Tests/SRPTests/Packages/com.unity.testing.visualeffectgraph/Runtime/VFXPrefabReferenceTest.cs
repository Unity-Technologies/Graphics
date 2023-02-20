using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.VFX;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Testing.VisualEffectGraph
{
    public class VFXPrefabReferenceTest : MonoBehaviour
    {
        public GameObject PrefabReference;
        public VisualEffectAsset VfxReference;
    }
}
