
#ifdef UNITY_INSTANCING_ENABLED

    UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
        UNITY_DEFINE_INSTANCED_PROP(float4, unity_SpritePropsArray)
        UNITY_DEFINE_INSTANCED_PROP(float4, unity_SpriteColorArray)
    UNITY_INSTANCING_BUFFER_END(PerDrawSprite)
    #define unity_SpriteProps UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpritePropsArray)
    #define unity_SpriteColor UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteColorArray)

#else

    float4 unity_SpriteColor;
    float4 unity_SpriteProps;

#endif

float3 UnityFlipSprite(float3 pos, float2 flip)
{
    uint flipBits = asuint(flip.x);
    float fx = (flipBits & 1u) ? -1.0f : 1.0f;
    float fy = (flipBits & 2u) ? -1.0f : 1.0f;
    return float3(pos.xy * float2(fx, fy), pos.z);
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
    output = BuildVaryings(input);
    output.color *= unity_SpriteColor;
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

#ifdef BUILTIN_USELEGACYSPRITEBLOCKS
    half4 color = surfaceDescription.SpriteColor;
#else
    half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);
#endif

#if ALPHA_CLIP_THRESHOLD
    clip(color.a - surfaceDescription.AlphaClipThreshold);
#endif

#if !defined(HAVE_VFX_MODIFICATION) && !defined(_DISABLE_COLOR_TINT)
    color *= unpacked.color;
#endif

    return color;
}
