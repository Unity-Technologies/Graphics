using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

namespace UnityEngine.Rendering.HighDefinition
{
    struct MaskVolumeHandle
    {
        IMaskVolumeList m_List;
        int m_Index;
        
        public MaskVolumeHandle(IMaskVolumeList list, int index)
        {
            m_List = list;
            m_Index = index;
        }
        
        public bool IsDataAssigned() => m_List.IsDataAssigned(m_Index);
        public bool IsDataUpdated() => m_List.IsDataUpdated(m_Index);
        public Vector3Int GetResolution() => m_List.GetResolution(m_Index);
        
        public Vector3 position => m_List.GetPosition(m_Index);
        public Quaternion rotation => m_List.GetRotation(m_Index);

        public ref MaskVolumeArtistParameters parameters => ref m_List.GetParameters(m_Index);

        public VolumeGlobalUniqueID GetAtlasID() => m_List.GetAtlasID(m_Index);
        public MaskVolume.MaskVolumeAtlasKey ComputeMaskVolumeAtlasKey() => m_List.ComputeMaskVolumeAtlasKey(m_Index);
        public MaskVolume.MaskVolumeAtlasKey GetMaskVolumeAtlasKeyPrevious() => m_List.GetMaskVolumeAtlasKeyPrevious(m_Index);
        public void SetMaskVolumeAtlasKeyPrevious(MaskVolume.MaskVolumeAtlasKey key) => m_List.SetMaskVolumeAtlasKeyPrevious(m_Index, key);

        public int DataSHL0Length => m_List.GetDataSHL0Length(m_Index);
        public void SetDataSHL0(CommandBuffer cmd, ComputeBuffer buffer) => m_List.SetDataSHL0(cmd, m_Index, buffer);

#if UNITY_EDITOR
        public bool IsHiddenInScene() => m_List.IsHiddenInScene(m_Index);
#endif
    }
}
