namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCacheGlobalProbeConstants
    {
        public float _EnvironmentIntensityMultiplier;
        public uint _GlobalProbePad0;
        public uint _GlobalProbePad1;
        public uint _GlobalProbePad2;
    }
}
