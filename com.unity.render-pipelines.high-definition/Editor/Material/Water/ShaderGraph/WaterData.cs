using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class WaterData : HDTargetData
    {
        public enum MaterialType
        {
            Water,
            WaterCinematic
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
