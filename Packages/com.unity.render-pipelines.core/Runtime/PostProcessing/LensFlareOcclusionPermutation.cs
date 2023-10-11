namespace UnityEngine.Rendering
{
    [GenerateHLSL]
    internal enum LensFlareOcclusionPermutation
    {
        Depth = (1 << 0),
        CloudLayer = (1 << 1),
        VolumetricCloud = (1 << 2),
        Water = (1 << 3)
    }
}
