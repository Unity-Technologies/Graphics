
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
