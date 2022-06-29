
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

#if USE_SHAPE_LIGHT_TYPE_0
SHAPE_LIGHT(0)
#endif

#if USE_SHAPE_LIGHT_TYPE_1
SHAPE_LIGHT(1)
#endif

#if USE_SHAPE_LIGHT_TYPE_2
SHAPE_LIGHT(2)
#endif

#if USE_SHAPE_LIGHT_TYPE_3
SHAPE_LIGHT(3)
#endif

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

half4 _RendererColor;

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    output.color *= _RendererColor;
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

#ifndef HAVE_VFX_MODIFICATION
    color *= unpacked.color;
#endif

    SurfaceData2D surfaceData;
    InitializeSurfaceData(color.rgb, color.a, surfaceDescription.SpriteMask, surfaceData);
    InputData2D inputData;
    InitializeInputData(unpacked.texCoord0.xy, half2(unpacked.screenPosition.xy / unpacked.screenPosition.w), inputData);
    SETUP_DEBUG_DATA_2D(inputData, unpacked.positionWS);

    return CombinedShapeLightShared(surfaceData, inputData);
}
