namespace UnityEngine.Rendering
{
    [GenerateHLSL]
    internal enum LensFlareOcclusionPermutation
    {
        Depth = (1 << 0),
        FogOpacity = (1 << 2),
        Water = (1 << 3)
    }
}
