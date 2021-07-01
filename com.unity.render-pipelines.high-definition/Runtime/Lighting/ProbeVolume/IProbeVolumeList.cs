namespace UnityEngine.Rendering.HighDefinition
{
    interface IProbeVolumeList
    {
        void ReleaseRemovedVolumesFromAtlas();
        int GetVolumeCount();
        
        bool IsAssetCompatible(int i);
        bool IsDataAssigned(int i);
        bool IsDataUpdated(int i);

        Vector3 GetPosition(int i);
        Quaternion GetRotation(int i);

        ref ProbeVolumeArtistParameters GetParameters(int i);

        int GetAtlasID(int i);
        int GetBakeID(int i);
        ProbeVolume.ProbeVolumeAtlasKey ComputeProbeVolumeAtlasKey(int i);
        ProbeVolume.ProbeVolumeAtlasKey GetProbeVolumeAtlasKeyPrevious(int i);
        void SetProbeVolumeAtlasKeyPrevious(int i, ProbeVolume.ProbeVolumeAtlasKey key);

        int GetDataSHL01Length(int i);
        int GetDataSHL2Length(int i);
        int GetDataOctahedralDepthLength(int i);

        void SetDataSHL01(int i, ComputeBuffer buffer);
        void SetDataSHL2(int i, ComputeBuffer buffer);
        void SetDataValidity(int i, ComputeBuffer buffer);
        void SetDataOctahedralDepth(int i, ComputeBuffer buffer);
        
#if UNITY_EDITOR
        public bool IsHiddenInScene(int i);
#endif
    }
}
