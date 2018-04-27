namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public struct ReflectionSystemParameters
    {
        public static ReflectionSystemParameters Default = new ReflectionSystemParameters
        {
            maxPlanarReflectionProbePerRender = 512,
            maxActivePlanarReflectionProbe = 512,
            planarReflectionProbeSize = 128
        };

        public int maxPlanarReflectionProbePerRender;
        public int maxActivePlanarReflectionProbe;
        public int planarReflectionProbeSize;
    }
}
