namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCacheTemporalFilteringConstants
    {
        public uint _RingConfigOffset;
        public float _TemporalSmoothing;
        public uint _TemporalFilteringPad0;
        public uint _TemporalFilteringPad1;
    }
}
