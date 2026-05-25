#if SHADERPASS != SHADERPASS_CUSTOM_UI
#error SHADERPASS_CUSTOM_UI_is_not_correctly_defined
#endif

PackedVaryings uie_custom_vert(Attributes input)
{
    appdata_t uieInput = (appdata_t)0;
    uieInput.vertex    = float4(input.positionOS, 1.0f);
    uieInput.color     = input.color;
    uieInput.uv        = input.uv0;
    uieInput.packedIds = input.uv4;
    uieInput.circle    = input.uv5;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, uieInput);

    v2f uieOutput = uie_std_vert(uieInput);

    Varyings varyings = (Varyings)0;
    varyings.positionCS = uieOutput.pos;

    // Run the shader graph vertex stage so custom interpolators (and any other
    // VertexDescription outputs) can be written into the Varyings. Position/Normal/
    // Tangent on VertexDescription are not used here because UITK's vertex transform
    // is owned by uie_std_vert above; only opt-in CIs are forwarded.
#if defined(FEATURES_GRAPH_VERTEX)
    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
    #if defined(CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC)
        CustomInterpolatorPassThroughFunc(varyings, vertexDescription);
    #endif
#endif

#ifdef VARYINGS_NEED_POSITION_WS
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    varyings.positionWS = positionWS;
#endif
    
    varyings.color = uieOutput.color;

    // User UV0-UV3 are forwarded to texCoord0-3 on demand (opt-in via PanelSettings.extraVertexChannels).
    // input.uv0 carries layoutUV in .zw — the surface-input builder strips it before exposing uv0 to graphs.
#ifdef VARYINGS_NEED_TEXCOORD0
    varyings.texCoord0 = input.uv0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    varyings.texCoord1 = input.uv1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    varyings.texCoord2 = input.uv2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    varyings.texCoord3 = input.uv3;
#endif

    // UITK internals relocated to texCoord4-7; texCoord0-3 stay reserved for the user.
    varyings.texCoord4 = uieOutput.uvClip;
    varyings.texCoord5 = uieOutput.typeTexSettings;
    varyings.texCoord6 = float4(uieOutput.textCoreLoc.x, uieOutput.textCoreLoc.y, input.uv0.z, input.uv0.w); // Layout uv in z, w
    varyings.texCoord7 = uieOutput.circle;

    UNITY_TRANSFER_INSTANCE_ID(input, varyings);
    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(uieOutput, varyings);

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
