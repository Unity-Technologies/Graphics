namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct SurfaceCacheGIScreenResolveDenoisingConstants
    {
        [HLSLArray(2, typeof(Matrix4x4))]
        public fixed float _ClipToWorldTransforms[2 * 16];

        public Vector4 _CameraWorldPos; // xyz: position, w: unused.

        public int _PassIndex;
        public uint _ScreenResolveDenoisingPad0;
        public uint _ScreenResolveDenoisingPad1;
        public uint _ScreenResolveDenoisingPad2;
    }
}
