using System;
using UnityEngine;
using UnityEditor.Rendering.BuiltIn;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    [Serializable]
    sealed class FullscreenMetaData : ScriptableObject
    {
        [SerializeField]
        FullscreenMode m_FullscreenMode;

        public FullscreenMode fullscreenMode
        {
            get => m_FullscreenMode;
            set => m_FullscreenMode = value;
        }
    }
}
