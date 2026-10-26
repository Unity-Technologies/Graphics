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

    Varyings varyings = (Varyings)0;

    // Run the graph vertex stage and write its (possibly modified) element-local Position into the
    // vertex; uie_std_vert then applies translation -> bone -> group -> clip. A pass-through graph
    // returns input.positionOS, so the output matches the built-in path. uie_build_vertex also
    // forwards the custom interpolators into the Varyings.
#if defined(FEATURES_GRAPH_VERTEX)
    uieInput.vertex.xyz = uie_build_vertex(input, varyings);
#endif

    v2f uieOutput = uie_std_vert(uieInput);

    varyings.positionCS = uieOutput.pos;

    // Load the element record once and reuse it for opacity and the world position below; uie_std_vert
    // loads its own copy internally, but sharing here avoids the extra reads on the custom path.
    uint transformId, opacityId;
    float2 translation;
    uie_load_element_info(uieInput, transformId, opacityId, translation);

    // Custom shaders carry the straight tint (vertexColor * dynamicColor, no opacity, no premultiply)
    // and the per-element opacity separately; opacity is applied at the end of the fragment. This
    // ignores uie_std_vert's baked color, which is for the built-in path only.
    varyings.color = uie_element_tint(uieInput.color, uieInput.packedIds);
    varyings.opacity = ReadShaderInfo(uie_id_to_texel_opacity(opacityId)).a;

    // The element world position is group * bone * (local + translation). uie_local_to_world stops at
    // group space, so apply the group->world transform on top.
#ifdef VARYINGS_NEED_POSITION_WS
    varyings.positionWS = TransformObjectToWorld(uie_local_to_world(uieInput.vertex, transformId, translation));
#endif

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

// The picking/selection ids must round-trip exactly; UIE_FRAG_T can be fixed4/half4, which may not
// preserve them (e.g. _ObjectId above 2048 loses integer precision in half).
#if defined(SCENEPICKINGPASS) || defined(SCENESELECTIONPASS)
#define UIE_CUSTOM_FRAG_T float4
#else
#define UIE_CUSTOM_FRAG_T UIE_FRAG_T
#endif

UIE_CUSTOM_FRAG_T uie_custom_frag(PackedVaryings packedInput) : SV_Target
{
    Varyings varyings = UnpackVaryings(packedInput);
    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(varyings);

    // BuildSurfaceDescriptionInputs only populates opacity when a node requires it; force it from the
    // carried varying so the Element Color node's Opacity output is correct even on its own.
    surfaceDescriptionInputs.opacity = varyings.opacity;

#if !defined(SCENESELECTIONPASS) && !defined(SCENEPICKINGPASS)
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
#endif

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

    // Selection/picking only need the covered footprint (rect/arc coverage and clipping above);
    // the graph's color output is replaced by the id, so SurfaceDescriptionFunction is skipped.
#if defined(SCENESELECTIONPASS)
    return float4(_ObjectId, _PassValue, 1, 1);
#elif defined(SCENEPICKINGPASS)
    return _SelectionID;
#else
    surfaceDescription.Alpha *= coverage;

    // Apply the per-element opacity here, at the very end, instead of baking it into the tint in the
    // vertex. Authors can disable this (Graph Settings) and apply opacity themselves via the Element
    // Opacity node's output. Read the raw varying rather than surfaceDescriptionInputs.opacity:
    // the latter is only populated when a node requires it, but this apply always needs it.
#if defined(UITK_AUTOMATIC_OPACITY)
    surfaceDescription.Alpha *= varyings.opacity;
#endif

    return float4(surfaceDescription.BaseColor, surfaceDescription.Alpha);
#endif // SCENESELECTIONPASS / SCENEPICKINGPASS
}
