namespace UnityEngine.Rendering.HighDefinition
{
    interface IMaskVolumeList
    {
        void ReleaseRemovedVolumesFromAtlas();
        int GetVolumeCount();
        
        bool IsDataAssigned(int i);
        bool IsDataUpdated(int i);
        Vector3Int GetResolution(int i);

        Vector3 GetPosition(int i);
        Quaternion GetRotation(int i);

        ref MaskVolumeArtistParameters GetParameters(int i);

        int GetAtlasID(int i);

        int GetDataSHL0Length(int i);
        void SetDataSHL0(int i, ComputeBuffer buffer);
    }
}
