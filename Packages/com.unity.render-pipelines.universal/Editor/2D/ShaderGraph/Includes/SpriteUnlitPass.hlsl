#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

half4 _RendererColor;

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);

    SetUpSpriteInstanceProperties();
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
    output = BuildVaryings(input);
    output.color *= _RendererColor * unity_SpriteColor; // vertex color has to applied here
#if defined(DEBUG_DISPLAY)
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif
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

    if (color.a == 0.0)
        discard;

#if ALPHA_CLIP_THRESHOLD
    clip(color.a - surfaceDescription.AlphaClipThreshold);
#endif

#if defined(DEBUG_DISPLAY)
    SurfaceData2D surfaceData;
    InitializeSurfaceData(color.rgb, color.a, surfaceData);
    surfaceData.normalWS = unpacked.normalWS;
#if (SHADERPASS == SHADERPASS_SPRITELIT)
    surfaceData.normalTS = surfaceDescription.NormalTS;
#endif 
    InputData2D inputData;
    InitializeInputData(unpacked.positionWS.xy, half2(unpacked.texCoord0.xy), inputData);
    half4 debugColor = 0;

    SETUP_DEBUG_DATA_2D(inputData, unpacked.positionWS, unpacked.positionCS);

    if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
    {
        return debugColor;
    }
#endif

    // Disable vertex color multiplication. Users can get the color from VertexColor node
#if !defined(HAVE_VFX_MODIFICATION) && !defined(_DISABLE_COLOR_TINT)
    color *= unpacked.color;
#endif

    return color;
}
