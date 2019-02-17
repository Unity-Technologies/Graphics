
namespace UnityEngine.Experimental.VoxelizedShadowMaps
{
    public enum VoxelResolution
    {
        // for debugging
        _64 = 64,
        _128 = 64 << 1,
        _256 = 64 << 2,
        _512 = 64 << 3,

        // actually exposed in editor
        _1024 = 1024,
        _2048 = _1024 << 1,
        _4096 = _1024 << 2,
        _8192 = _1024 << 3,
        _16384 = _1024 << 4,
        _32768 = _1024 << 5,
        _65536 = _1024 << 6,
        _131072 = _1024 << 7,
        _262144 = _1024 << 8,
    }

    public enum ShadowsBlendMode
    {
        OnlyVxShadowMaps,
        BlendDynamicShadows,
    }

    public abstract class VxShadowMap : MonoBehaviour
    {
        public static readonly VoxelResolution subtreeResolution = VoxelResolution._4096;
        public static readonly int subtreeResolutionInt = (int)subtreeResolution;
    }
}
