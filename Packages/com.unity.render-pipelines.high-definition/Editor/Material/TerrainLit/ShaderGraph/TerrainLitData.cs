using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class TerrainLitData : HDTargetData
    {
        [SerializeField]
        private bool m_EnableInstancedPerPixelNormal = true;
        public bool enableInstancedPerPixelNormal
        {
            get => m_EnableInstancedPerPixelNormal;
            set => m_EnableInstancedPerPixelNormal = value;
        }

    }
}
