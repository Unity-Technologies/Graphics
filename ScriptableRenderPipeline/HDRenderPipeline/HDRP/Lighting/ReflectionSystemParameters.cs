namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public struct ReflectionSystemParameters
    {
        public static ReflectionSystemParameters Default = new ReflectionSystemParameters
        {
            maxPlanarReflectionProbes = 512
        };

        public int maxPlanarReflectionProbes;
    }
}
