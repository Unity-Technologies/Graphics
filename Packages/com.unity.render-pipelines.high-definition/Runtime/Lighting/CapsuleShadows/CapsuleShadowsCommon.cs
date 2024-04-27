using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum CapsuleShadowResolution
    {
        Half,
        Quarter,
    }

    [GenerateHLSL]
    [Flags]
    public enum CapsuleShadowFlags
    {
        DirectEnabled = (1 << 0),
        IndirectEnabled = (1 << 1),
        QuarterResolution = (1 << 2),
        FadeSelfShadow = (1 << 3),
        FullCapsuleOcclusion = (1 << 4),
        FullCapsuleAmbientOcclusion = (1 << 5),
        LayerMaskEnabled = (1 << 6),

        ShowRayTracedReference = (1 << 8),  // debug settings, default false
        UseCheckerboardDepths = (1 << 9),   // debug settings, default true
        UseCoarseCulling = (1 << 10),       // debug settings, default true
        UseSplitDepthRange = (1 << 11),     // debug settings, default true
        UseSparseTiles = (1 << 12),         // debug settings, default true
    }

    [GenerateHLSL]
    internal enum CapsuleShadowConstants
    {
        MaxShadowCasterCount = 8,
        MaxCoarseTileCountPerView = 64,
        MaxViewCount = 2,
        MaxCoarseTileCount = MaxCoarseTileCountPerView * MaxViewCount,
    }

    [GenerateHLSL]
    internal enum CapsuleShadowCounterSlot
    {
        CoarseTileDepthRangeBase,
        CoarseTileShadowCountBase = CoarseTileDepthRangeBase + 2*CapsuleShadowConstants.MaxCoarseTileCount,
        TileListDispatchArg = CoarseTileShadowCountBase + CapsuleShadowConstants.MaxCoarseTileCount,
        Count = TileListDispatchArg + 3, // keep last
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleShadowFilterTile
    {
        public uint coord;  // [31:30]=viewIndex, [29:15]=tileY, [14:0]=tileX
        public uint bits;   // [31:12]=unused, [11:8]=cornerBorders, [7:0]=casterValid
    }
}
