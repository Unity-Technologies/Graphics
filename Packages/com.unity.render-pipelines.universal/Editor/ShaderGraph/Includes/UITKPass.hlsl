#if SHADERPASS != SHADERPASS_CUSTOM_UI
#error SHADERPASS_CUSTOM_UI_is_not_correctly_defined
#endif


#define UIE_NOINTERPOLATION nointerpolation

PackedVaryings uie_custom_vert(Attributes input)
{
    appdata_t uieInput = (appdata_t)0;
    uieInput.vertex = float4(input.positionOS, 1.0f);
    uieInput.color = input.color;
    uieInput.uv = input.uv0;
    uieInput.xformClipPages = input.uv1;
    uieInput.ids = input.uv2;
    uieInput.flags = input.uv3;
    uieInput.opacityColorPages = input.uv4;
    uieInput.settingIndex = input.uv5;
    uieInput.circle = input.uv6;
    uieInput.textureId = input.uv7.x;

    v2f uieOutput = uie_std_vert(uieInput);

    Varyings varyings = (Varyings)0;
    varyings.positionCS = uieOutput.pos;

#ifdef VARYINGS_NEED_POSITION_WS
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    varyings.positionWS = positionWS;
#endif
    
    varyings.color = uieOutput.color;
    varyings.texCoord0 = uieOutput.uvClip;
    varyings.texCoord1 = uieOutput.typeTexSettings;
    varyings.texCoord3 = float4(uieOutput.textCoreLoc.x, uieOutput.textCoreLoc.y, input.uv0.z, input.uv0.w); // Layout uv in z, w
    varyings.texCoord4 = uieOutput.circle;

    PackedVaryings packedOutput = PackVaryings(varyings);
    return packedOutput;
}

UIE_FRAG_T uie_custom_frag(PackedVaryings packedInput) : SV_Target
{
    Varyings varyings = UnpackVaryings(packedInput);
    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(varyings);

    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    // TODO: In the future, we should try to use surfaceDescription.coverage instead of computing coverage outside
    // of the branches like we do here.
    half renderType = round(surfaceDescriptionInputs.typeTexSettings.x);
    half isArc = surfaceDescriptionInputs.typeTexSettings.w;
    float2 outer = surfaceDescriptionInputs.circle.xy;
    float2 inner = surfaceDescriptionInputs.circle.zw;
    float coverage = uie_sg_compute_aa_coverage(renderType, isArc, outer, inner);

    coverage *= uie_fragment_clip(surfaceDescriptionInputs.uvClip.zw);

    // Clip fragments when coverage is close to 0 (< 1/256 here).
    // This will write proper masks values in the stencil buffer.
    clip(coverage - 0.003f);

    surfaceDescription.Alpha *= coverage;

    return float4(surfaceDescription.BaseColor, surfaceDescription.Alpha);
}
