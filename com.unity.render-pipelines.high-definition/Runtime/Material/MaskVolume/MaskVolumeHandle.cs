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

        public int GetAtlasID() => m_List.GetAtlasID(m_Index);
        
        public int DataSHL0Length => m_List.GetDataSHL0Length(m_Index);
        public void SetDataSHL0(ComputeBuffer buffer) => m_List.SetDataSHL0(m_Index, buffer);
    }
}
