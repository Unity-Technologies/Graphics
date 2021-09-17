using System;
using UnityEngine;
using UnityEditor.Rendering.BuiltIn;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    [Serializable]
    sealed class FullscreenMetaData : ScriptableObject
    {
        [SerializeField]
        FullscreenTarget.MaterialType m_MaterialType;

        public FullscreenTarget.MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }
    }
}
