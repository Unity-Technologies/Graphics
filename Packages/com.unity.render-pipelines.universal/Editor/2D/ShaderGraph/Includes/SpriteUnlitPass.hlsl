#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

half4 _RendererColor;

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);

#ifdef UNITY_INSTANCING_ENABLED
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteFlip);
#endif

    output = BuildVaryings(input);
    output.color *= _RendererColor;

#ifdef UNITY_INSTANCING_ENABLED
    output.color *= unity_SpriteColor;
#endif

    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

#ifdef UNIVERSAL_USELEGACYSPRITEBLOCKS
    half4 color = surfaceDescription.SpriteColor;
#else
    half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);
#endif

    if (color.a == 0.0)
        discard;

    #if defined(DEBUG_DISPLAY)
    SurfaceData2D surfaceData;
    InitializeSurfaceData(color.rgb, color.a, surfaceData);
    InputData2D inputData;
    InitializeInputData(unpacked.positionWS.xy, half2(unpacked.texCoord0.xy), inputData);
    half4 debugColor = 0;

    SETUP_DEBUG_DATA_2D(inputData, unpacked.positionWS);

    if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
    {
        return debugColor;
    }
    #endif

    color *= unpacked.color;
    return color;
}
