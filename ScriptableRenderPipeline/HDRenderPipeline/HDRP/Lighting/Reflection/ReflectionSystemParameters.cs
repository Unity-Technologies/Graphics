namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public struct ReflectionSystemParameters
    {
        public static ReflectionSystemParameters Default = new ReflectionSystemParameters
        {
            maxPlanarReflectionProbes = 512,
            planarReflectionProbeSize = 128
        };

        public int maxPlanarReflectionProbes;
        public int planarReflectionProbeSize;
    }
}
