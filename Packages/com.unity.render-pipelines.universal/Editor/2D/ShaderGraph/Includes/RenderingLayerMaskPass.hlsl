#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

half4 _RendererColor;

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);

#if !_ENABLE_SORT_3D_AS_2D_COMPATIBLE
    SetUpSpriteInstanceProperties();
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
#endif
    output = BuildVaryings(input);
#if !_ENABLE_SORT_3D_AS_2D_COMPATIBLE
    output.color *= _RendererColor * unity_SpriteColor; // vertex color has to applied here
#endif
#if defined(DEBUG_DISPLAY)
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

void frag(PackedVaryings packedInput, out uint outRenderingLayers : SV_Target0)
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

    outRenderingLayers = GetMeshRenderingLayer();
}

