namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCachePunctualLightSamplingConstants
    {
        public float _MaterialAtlasTexelSize;
        public float _AlbedoBoost;
        public uint _FrameIdx;
        public uint _PunctualLightCount;

        public uint _SamplesPerPunctualLight;
        public uint _PunctualLightSamplingPad0;
        public uint _PunctualLightSamplingPad1;
        public uint _PunctualLightSamplingPad2;
    }
}
