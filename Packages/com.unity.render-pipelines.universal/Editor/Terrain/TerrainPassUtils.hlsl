
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"

void SplatmapFinalColor(inout URP_LIGHT_ACCUM4 color, float4 positionCS)
{
    color.rgb *= color.a;

#ifndef TERRAIN_GBUFFER // Technically we don't need fogCoord, but it is still passed from the vertex shader.
    #ifdef TERRAIN_SPLAT_ADDPASS
        color.rgb = BlendDistanceFogColor(color.rgb, positionCS, half3(0,0,0));
    #else
        color.rgb = BlendDistanceFog(color.rgb, positionCS);
    #endif
#endif
}
