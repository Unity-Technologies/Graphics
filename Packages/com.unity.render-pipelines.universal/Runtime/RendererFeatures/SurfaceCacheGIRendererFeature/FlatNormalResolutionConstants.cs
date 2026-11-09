namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct SurfaceCacheGIFlatNormalResolutionConstants
    {
        [HLSLArray(2, typeof(Matrix4x4))]
        public fixed float _ClipToWorldTransforms[2 * 16];
    }
}
