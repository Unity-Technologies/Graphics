namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct SurfaceCacheGIScreenResolveUpsamplingConstants
    {
        [HLSLArray(2, typeof(Matrix4x4))]
        public fixed float _ClipToWorldTransforms[2 * 16];

        public Vector4 _CameraWorldPos; // xyz: position, w: unused.

        public float _Intensity;
        public uint _ScreenResolveUpsamplingPad0;
        public uint _ScreenResolveUpsamplingPad1;
        public uint _ScreenResolveUpsamplingPad2;
    }
}
