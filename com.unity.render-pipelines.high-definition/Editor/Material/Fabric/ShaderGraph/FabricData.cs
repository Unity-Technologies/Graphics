using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class FabricData : HDTargetData
    {
        public enum MaterialType
        {
            CottonWool,
            Silk
        }

        [SerializeField]
        MaterialType m_MaterialType;
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }
    }
}
