using System;
using Unity.Collections;

namespace UnityEngine.Rendering
{
    internal class GPUResidentContext : IDisposable
    {
        private InstanceDataSystem m_InstanceDataSystem;
        private LODGroupDataSystem m_LODGroupDataSystem;
        private InstanceCuller m_Culler;
        private OcclusionCullingCommon m_OcclusionCullingCommon;
        private InstanceCullingBatcher m_InstanceCullingBatcher;
        private GPUResidentDrawerResources m_Resources;
        private DebugRendererBatcherStats m_DebugStats;

        public InstanceDataSystem instanceDataSystem => m_InstanceDataSystem;
        public LODGroupDataSystem lodGroupDataSystem => m_LODGroupDataSystem;
        public InstanceCuller culler => m_Culler;
        internal OcclusionCullingCommon occlusionCullingCommon => m_OcclusionCullingCommon;
        public InstanceCullingBatcher batcher => m_InstanceCullingBatcher;
        public GPUResidentDrawerResources resources => m_Resources;
        internal DebugRendererBatcherStats debugStats => m_DebugStats;

        public SphericalHarmonicsL2 cachedAmbientProbe;
        public readonly float smallMeshScreenPercentage;

        public GPUResidentContext(in GPUResidentDrawerSettings settings,
            InstanceDataSystem instanceDataSystem,
            LODGroupDataSystem lodGroupDataSystem,
            InstanceCuller culler,
            OcclusionCullingCommon occlusionCullingCommon,
            InstanceCullingBatcher instanceCullingBatcher,
            GPUResidentDrawerResources resources)
        {
            m_InstanceDataSystem = instanceDataSystem;
            m_LODGroupDataSystem = lodGroupDataSystem;
            m_Culler = culler;
            m_OcclusionCullingCommon = occlusionCullingCommon;
            m_InstanceCullingBatcher = instanceCullingBatcher;
            m_Resources = resources;
            m_DebugStats = new DebugRendererBatcherStats(); // for now, always allow the possibility of reading counter stats from the cullers.
            cachedAmbientProbe = RenderSettings.ambientProbe;
            smallMeshScreenPercentage = settings.smallMeshScreenPercentage;
        }

        public void Dispose()
        {
            m_DebugStats?.Dispose();
            m_DebugStats = null;
        }
    }
}
