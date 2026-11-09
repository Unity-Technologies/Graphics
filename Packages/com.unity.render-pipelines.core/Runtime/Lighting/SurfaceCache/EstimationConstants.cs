namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCacheEstimationConstants
    {
        // Vectors are declared as Vector4 because some platforms (e.g. Metal) don't pack a scalar into the last component of a float3.
        public Vector4 _VolumeTargetPos; // xyz: position, w: unused.
        public Vector4 _DirectionalLightDirection; // xyz: direction, w: unused.
        public Vector4 _DirectionalLightIntensity; // xyz: intensity, w: unused.

        public uint _PunctualLightCount;
        public uint _FrameIdx;
        public uint _VolumeSpatialResolution;
        public uint _VolumeCascadeCount;

        public uint _MultiBounce;
        public uint _SampleCount;
        public uint _WarmUpSampleMultiplier;
        public uint _RingConfigOffset;

        public int _PatchWarping;
        public float _AlbedoBoost;
        public float _EnvironmentIntensityMultiplier;
        public float _EmissiveTriangleIntensityMultiplier;

        public uint _PunctualLightSampleCount;
        public uint _BouncePatchAllocation;
        public float _VolumeVoxelMinSize;
        public float _ShortHysteresis;

        public float _MaterialAtlasTexelSize; // The size of 1 texel in the material atlases.
        public uint _PunctualLightCandidateCount;
        public float _RayTracingNormalBias;
        public uint _EstimationPad0;
    }
}
