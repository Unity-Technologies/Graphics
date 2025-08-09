#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

half4 White;

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    input.positionOS = input.positionOS;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#ifdef UNIVERSAL_USELEGACYSPRITEBLOCKS
    half4 color = surfaceDescription.SpriteColor;
#else
    half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);
#endif

#if ALPHA_CLIP_THRESHOLD
    clip(color.a - surfaceDescription.AlphaClipThreshold);
#endif

    // Disable vertex color multiplication. Users can get the color from VertexColor node
#if !defined(HAVE_VFX_MODIFICATION) && !defined(_DISABLE_COLOR_TINT)
    color *= unpacked.color;
#endif

    SurfaceData2D surfaceData;
    InitializeSurfaceData(color.rgb, color.a, surfaceDescription.SpriteMask, surfaceData);
    InputData2D inputData;
    InitializeInputData(unpacked.texCoord0.xy, half2(unpacked.screenPosition.xy / unpacked.screenPosition.w), inputData);
    SETUP_DEBUG_DATA_2D(inputData, unpacked.positionWS, unpacked.positionCS);

#if defined(SHADERGRAPH_PREVIEW_MAIN) || defined(SHADERGRAPH_PREVIEW)
    return CombinedShapeLightShared(surfaceData, inputData);
#else
    return White * CombinedShapeLightShared(surfaceData, inputData);
#endif
}
