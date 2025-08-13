using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class TerrainLitData : HDTargetData
    {
        [SerializeField]
        private bool m_EnableHeightBlened;
        public bool enableHeightBlend
        {
            get => m_EnableHeightBlened;
            set => m_EnableHeightBlened = value;
        }

        [SerializeField]
        private float m_HeightTransition;
        public float heightTransition
        {
            get => m_HeightTransition;
            set => m_HeightTransition = value;
        }

        [SerializeField]
        private bool m_EnableInstancedPerPixelNormal = true;
        public bool enableInstancedPerPixelNormal
        {
            get => m_EnableInstancedPerPixelNormal;
            set => m_EnableInstancedPerPixelNormal = value;
        }

    }
}
