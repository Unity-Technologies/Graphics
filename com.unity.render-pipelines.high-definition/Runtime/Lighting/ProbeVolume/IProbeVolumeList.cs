namespace UnityEngine.Rendering.HighDefinition
{
    interface IProbeVolumeList
    {
        void ReleaseRemovedVolumesFromAtlas();
        int GetVolumeCount();

        bool IsAssetCompatible(int i);
        bool IsDataAssigned(int i);
        int GetDataVersion(int i);
        void IncrementDataVersion(int i);

        Vector3 GetPosition(int i);
        Quaternion GetRotation(int i);

        ProbeVolumeArtistParameters GetParameters(int i);

        ProbeVolume.ProbeVolumeAtlasKey ComputeProbeVolumeAtlasKey(int i);

        int GetDataSHL01Length(int i);
        int GetDataSHL2Length(int i);
        int GetDataOctahedralDepthLength(int i);

        void SetDataOctahedralDepth(int i, ComputeBuffer buffer);

        public void EnsureVolumeBuffers(int i);
        public ref ProbeVolumePipelineData GetPipelineData(int i);

        // Dynamic GI
        ref ProbeVolumePropagationPipelineData GetPropagationPipelineData(int i);
        bool HasNeighbors(int i);
        int GetHitNeighborAxisLength(int i);
        int GetNeighborAxisLength(int i);
        void SetHitNeighborAxis(int i, ComputeBuffer buffer);
        void SetNeighborAxis(int i, ComputeBuffer buffer);

#if UNITY_EDITOR
        public bool IsHiddenInScene(int i);
#endif
    }
}
