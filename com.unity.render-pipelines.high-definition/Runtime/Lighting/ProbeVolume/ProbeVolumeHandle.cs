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

        public Vector3 position => m_List.GetPosition(m_Index);
        public Quaternion rotation => m_List.GetRotation(m_Index);

        public ref ProbeVolumeArtistParameters parameters => ref m_List.GetParameters(m_Index);

        public int GetAtlasID() => m_List.GetAtlasID(m_Index);
        public int GetBakeID() => m_List.GetBakeID(m_Index);
        public ProbeVolume.ProbeVolumeAtlasKey ComputeProbeVolumeAtlasKey() => m_List.ComputeProbeVolumeAtlasKey(m_Index);
        public ProbeVolume.ProbeVolumeAtlasKey GetProbeVolumeAtlasKeyPrevious() => m_List.GetProbeVolumeAtlasKeyPrevious(m_Index);
        public void SetProbeVolumeAtlasKeyPrevious(ProbeVolume.ProbeVolumeAtlasKey key) => m_List.SetProbeVolumeAtlasKeyPrevious(m_Index, key);
        
        public int DataSHL01Length => m_List.GetDataSHL01Length(m_Index);
        public int DataSHL2Length => m_List.GetDataSHL2Length(m_Index);
        public int DataOctahedralDepthLength  => m_List.GetDataOctahedralDepthLength(m_Index);
        public void SetDataSHL01(ComputeBuffer buffer) => m_List.SetDataSHL01(m_Index, buffer);
        public void SetDataSHL2(ComputeBuffer buffer) => m_List.SetDataSHL2(m_Index, buffer);
        public void SetDataValidity(ComputeBuffer buffer) => m_List.SetDataValidity(m_Index, buffer);
        public void SetDataOctahedralDepth(ComputeBuffer buffer) => m_List.SetDataOctahedralDepth(m_Index, buffer);
        
#if UNITY_EDITOR
        public bool IsHiddesInScene() => m_List.IsHiddenInScene(m_Index);
#endif
    }
}
