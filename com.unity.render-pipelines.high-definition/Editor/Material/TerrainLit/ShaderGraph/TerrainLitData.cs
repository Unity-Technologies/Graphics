using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class TerrainLitData : HDTargetData
    {
        [SerializeField]
        private bool m_RayTracing;
        public bool rayTracing
        {
            get => m_RayTracing;
            set => m_RayTracing = value;
        }
    }
}
