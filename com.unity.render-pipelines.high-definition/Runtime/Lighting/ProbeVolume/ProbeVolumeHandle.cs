using System;
using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

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
        public bool IsDataUpdated() => m_List.IsDataUpdated(m_Index);
        public void SetDataUpdated(bool value) => m_List.SetDataUpdated(m_Index, value);

        public Vector3 position => m_List.GetPosition(m_Index);
        public Quaternion rotation => m_List.GetRotation(m_Index);

        public ref ProbeVolumeArtistParameters parameters => ref m_List.GetParameters(m_Index);

        public VolumeGlobalUniqueID GetAtlasID() => m_List.GetAtlasID(m_Index);
        public ProbeVolume.ProbeVolumeAtlasKey ComputeProbeVolumeAtlasKey() => m_List.ComputeProbeVolumeAtlasKey(m_Index);
        public ProbeVolume.ProbeVolumeAtlasKey GetProbeVolumeAtlasKeyPrevious() => m_List.GetProbeVolumeAtlasKeyPrevious(m_Index);
        public void SetProbeVolumeAtlasKeyPrevious(ProbeVolume.ProbeVolumeAtlasKey key) => m_List.SetProbeVolumeAtlasKeyPrevious(m_Index, key);

        public int DataSHL01Length => m_List.GetDataSHL01Length(m_Index);
        public int DataSHL2Length => m_List.GetDataSHL2Length(m_Index);
        public int DataValidityLength => m_List.GetDataValidityLength(m_Index);
        public int DataOctahedralDepthLength  => m_List.GetDataOctahedralDepthLength(m_Index);
        public void SetDataOctahedralDepth(ComputeBuffer buffer) => m_List.SetDataOctahedralDepth(m_Index, buffer);
        public void EnsureVolumeBuffers() => m_List.EnsureVolumeBuffers(m_Index);
        public void SetVolumeBuffers() => m_List.SetVolumeBuffers(m_Index);
        public ProbeVolumeBuffers GetVolumeBuffers() => m_List.GetVolumeBuffers(m_Index);

        // Dynamic GI
        public int GetProbeVolumeEngineDataIndex() => m_List.GetProbeVolumeEngineDataIndex(m_Index);
        public OrientedBBox GetProbeVolumeEngineDataBoundingBox() => m_List.GetProbeVolumeEngineDataBoundingBox(m_Index);
        public ProbeVolumeEngineData GetProbeVolumeEngineData() => m_List.GetProbeVolumeEngineData(m_Index);
        public void ClearProbeVolumeEngineData() => m_List.ClearProbeVolumeEngineData(m_Index);
        public void SetProbeVolumeEngineData(int dataIndex, in OrientedBBox box, in ProbeVolumeEngineData data) => m_List.SetProbeVolumeEngineData(m_Index, dataIndex, in box, in data);
        public OrientedBBox ConstructOBBEngineData(Vector3 camOffset) => m_List.ConstructOBBEngineData(m_Index, camOffset);
        public ref ProbePropagationBuffers propagationBuffers => ref m_List.GetPropagationBuffers(m_Index);
        public bool HasNeighbors() => m_List.HasNeighbors(m_Index);

        public int HitNeighborAxisLength => m_List.GetHitNeighborAxisLength(m_Index);
        public int NeighborAxisLength => m_List.GetNeighborAxisLength(m_Index);
        public void SetHitNeighborAxis(ComputeBuffer buffer) => m_List.SetHitNeighborAxis(m_Index, buffer);
        public void SetNeighborAxis(ComputeBuffer buffer) => m_List.SetNeighborAxis(m_Index, buffer);

        public void SetLastSimulatedFrame(int simulationFrameTick) => m_List.SetLastSimulatedFrame(m_Index, simulationFrameTick);
        public int GetLastSimulatedFrame() => m_List.GetLastSimulatedFrame(m_Index);

        public bool AbleToSimulateDynamicGI()
        {
            return parameters.supportDynamicGI
                   && IsDataAssigned()
                   && HasNeighbors()
                   && GetProbeVolumeEngineDataIndex() >= 0;
        }

#if UNITY_EDITOR
        public bool IsHiddenInScene() => m_List.IsHiddenInScene(m_Index);
#endif
    }
}
