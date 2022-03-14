using System;

namespace UnityEngine.Rendering.HighDefinition
{
    struct ProbeVolumeHandle
    {
        IProbeVolumeList m_List;
        int m_Index;

        public ProbeVolumeHandle(IProbeVolumeList list, int index)
        {
            m_List = list;
            m_Index = index;
        }

        public bool IsAssetCompatible() => m_List.IsAssetCompatible(m_Index);
        public bool IsDataAssigned() => m_List.IsDataAssigned(m_Index);
        public int GetDataVersion() => m_List.GetDataVersion(m_Index);
        public void IncrementDataVersion() => m_List.IncrementDataVersion(m_Index);

        public Vector3 position => m_List.GetPosition(m_Index);
        public Quaternion rotation => m_List.GetRotation(m_Index);

        public ProbeVolumeArtistParameters parameters => m_List.GetParameters(m_Index);

        public ProbeVolume.ProbeVolumeAtlasKey ComputeProbeVolumeAtlasKey() => m_List.ComputeProbeVolumeAtlasKey(m_Index);

        public int DataSHL01Length => m_List.GetDataSHL01Length(m_Index);
        public int DataSHL2Length => m_List.GetDataSHL2Length(m_Index);
        public int DataOctahedralDepthLength  => m_List.GetDataOctahedralDepthLength(m_Index);
        public void SetDataOctahedralDepth(ComputeBuffer buffer) => m_List.SetDataOctahedralDepth(m_Index, buffer);
        public void EnsureVolumeBuffers() => m_List.EnsureVolumeBuffers(m_Index);
        public ref ProbeVolumePipelineData GetPipelineData() => ref m_List.GetPipelineData(m_Index);

        // Dynamic GI
        public ref ProbeVolumePropagationPipelineData GetPropagationPipelineData() => ref m_List.GetPropagationPipelineData(m_Index);
        public bool HasNeighbors() => m_List.HasNeighbors(m_Index);

        public int HitNeighborAxisLength => m_List.GetHitNeighborAxisLength(m_Index);
        public int NeighborAxisLength => m_List.GetNeighborAxisLength(m_Index);
        public void SetHitNeighborAxis(ComputeBuffer buffer) => m_List.SetHitNeighborAxis(m_Index, buffer);
        public void SetNeighborAxis(ComputeBuffer buffer) => m_List.SetNeighborAxis(m_Index, buffer);

        public bool AbleToSimulateDynamicGI()
        {
            return parameters.supportDynamicGI
                   && IsDataAssigned()
                   && HasNeighbors()
                   && GetPipelineData().EngineDataIndex >= 0;
        }

        public OrientedBBox ConstructOBBEngineData(Vector3 cameraOffset)
        {
            var obb = new OrientedBBox(Matrix4x4.TRS(position, rotation, parameters.size));

            // Handle camera-relative rendering.
            obb.center -= cameraOffset;

            return obb;
        }

#if UNITY_EDITOR
        public bool IsHiddesInScene() => m_List.IsHiddenInScene(m_Index);
#endif
    }
}
