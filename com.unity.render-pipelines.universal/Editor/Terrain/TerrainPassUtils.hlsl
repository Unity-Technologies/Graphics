
#ifdef UNIVERSAL_TERRAIN_SPLAT01
#define GetSplat0UV(x) x.uvSplat01.xy
#define GetSplat1UV(x) x.uvSplat01.zw
#else
#define GetSplat0UV(x) 0.0
#define GetSplat1UV(x) 0.0
#endif

#ifdef UNIVERSAL_TERRAIN_SPLAT23
#define GetSplat2UV(x) x.uvSplat23.xy
#define GetSplat3UV(x) x.uvSplat23.zw
#else
#define GetSplat2UV(x) 0.0
#define GetSplat3UV(x) 0.0
#endif

void SplatmapFinalColor(inout half4 color, half fogCoord)
{
    color.rgb *= color.a;

#ifndef TERRAIN_GBUFFER // Technically we don't need fogCoord, but it is still passed from the vertex shader.
    #ifdef TERRAIN_SPLAT_ADDPASS
        color.rgb = MixFogColor(color.rgb, half3(0,0,0), fogCoord);
    #else
        color.rgb = MixFog(color.rgb, fogCoord);
    #endif
#endif
}
