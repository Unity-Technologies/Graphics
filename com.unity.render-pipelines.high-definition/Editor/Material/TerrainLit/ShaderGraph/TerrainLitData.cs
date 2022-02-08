using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class TerrainLitData : HDTargetData
    {
        [SerializeField]
        TerrainSurfaceType m_TerrainSurfaceType;
        public TerrainSurfaceType terrainSurfaceType
        {
            get => m_TerrainSurfaceType;
            set => m_TerrainSurfaceType = value;
        }

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace;
        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }

        [SerializeField]
        bool m_ReceiveDecals = true;
        public bool receiveDecals
        {
            get => m_ReceiveDecals;
            set => m_ReceiveDecals = value;
        }

        [SerializeField]
        bool m_ReceiveSSR = true;
        public bool receiveSSR
        {
            get => m_ReceiveSSR;
            set => m_ReceiveSSR = value;
        }

        [SerializeField]
        private bool m_RayTracing;
        public bool rayTracing
        {
            get => m_RayTracing;
            set => m_RayTracing = value;
        }

        [SerializeField]
        SpecularOcclusionMode m_SpecularOcclusionMode = SpecularOcclusionMode.FromAO;
        public SpecularOcclusionMode specularOcclusionMode
        {
            get => m_SpecularOcclusionMode;
            set => m_SpecularOcclusionMode = value;
        }
    }
}
