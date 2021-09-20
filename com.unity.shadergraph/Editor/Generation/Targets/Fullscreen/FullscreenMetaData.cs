using System;
using UnityEngine;
using UnityEditor.Rendering.BuiltIn;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    [Serializable]
    sealed class FullscreenMetaData : ScriptableObject
    {
        [SerializeField]
        FullscreenTarget.FullscreenCompatibility _FullscreenCompatibility;

        public FullscreenTarget.FullscreenCompatibility fullscreenCompatibility
        {
            get => _FullscreenCompatibility;
            set => _FullscreenCompatibility = value;
        }
    }
}
