using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

namespace UnityEngine.Rendering.HighDefinition
{
    interface IProbeVolumeList
    {
        void ReleaseRemovedVolumesFromAtlas();
        int GetVolumeCount();

        bool IsAssetCompatible(int i);
        bool IsDataAssigned(int i);
        bool IsDataUpdated(int i);
        void SetDataUpdated(int i, bool value);

        Vector3 GetPosition(int i);
        Quaternion GetRotation(int i);

        ref ProbeVolumeArtistParameters GetParameters(int i);

        VolumeGlobalUniqueID GetAtlasID(int i);
        ProbeVolume.ProbeVolumeAtlasKey ComputeProbeVolumeAtlasKey(int i);
        ProbeVolume.ProbeVolumeAtlasKey GetProbeVolumeAtlasKeyPrevious(int i);
        void SetProbeVolumeAtlasKeyPrevious(int i, ProbeVolume.ProbeVolumeAtlasKey key);

        int GetDataSHL01Length(int i);
        int GetDataSHL2Length(int i);
        int GetDataValidityLength(int i);
        int GetDataOctahedralDepthLength(int i);

        void SetDataOctahedralDepth(int i, ComputeBuffer buffer);

        public void EnsureVolumeBuffers(int i);
        public void SetVolumeBuffers(int i);
        public ProbeVolumeBuffers GetVolumeBuffers(int i);

        // Dynamic GI
        int GetProbeVolumeEngineDataIndex(int i);
        OrientedBBox GetProbeVolumeEngineDataBoundingBox(int i);
        ProbeVolumeEngineData GetProbeVolumeEngineData(int i);
        void ClearProbeVolumeEngineData(int i);
        void SetProbeVolumeEngineData(int i, int dataIndex, in OrientedBBox box, in ProbeVolumeEngineData data);
        OrientedBBox ConstructOBBEngineData(int i, Vector3 camOffset);
        ref ProbePropagationBuffers GetPropagationBuffers(int i);
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
