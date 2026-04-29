namespace UnityEngine.PathTracing.Core
{
    // Must be kept in-sync with the equivalent type in Unity Core assembly.
    internal enum LightSamplingMode
    {
        RIS = 0,
        Uniform = 1,
        RoundRobin = 2,
    };

    // Must be kept in-sync with the equivalent type in Unity Core assembly.
    internal enum EmissiveSamplingMode
    {
        LightSampling = 0,
        BRDFSampling = 1,
        MIS = 2,
    };
}
