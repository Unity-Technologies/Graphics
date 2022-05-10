using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

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

        VolumeGlobalUniqueID GetAtlasID(int i);
        MaskVolume.MaskVolumeAtlasKey ComputeMaskVolumeAtlasKey(int i);
        MaskVolume.MaskVolumeAtlasKey GetMaskVolumeAtlasKeyPrevious(int i);
        void SetMaskVolumeAtlasKeyPrevious(int i, MaskVolume.MaskVolumeAtlasKey key);

        int GetDataSHL0Length(int i);
        void SetDataSHL0(CommandBuffer cmd, int i, ComputeBuffer buffer);

#if UNITY_EDITOR
        bool IsHiddenInScene(int i);
#endif
    }
}
