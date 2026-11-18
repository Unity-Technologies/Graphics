#if SHADERPASS != SHADERPASS_CUSTOM_UI
#error SHADERPASS_CUSTOM_UI_is_not_correctly_defined
#endif

// Fullscreen-quad pass for a ShaderGraph-authored filter; matches the built-in filter shaders' contract.

Varyings BuildVaryings(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    UIEFilterAppData filterInput = (UIEFilterAppData)0;
    UNITY_TRANSFER_INSTANCE_ID(input, filterInput);
    #if defined(UNITY_INSTANCING_ENABLED)
    filterInput.vertexID = input.vertexID;
    #else
    filterInput.vertex = float4(input.positionOS, 1);
    filterInput.uv = input.uv0.xy;
    #endif
    UIEFilterVertex filterVertex = UIEGetFilterVertex(filterInput);

    output.positionCS = filterVertex.position;
    #if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
    output.texCoord0 = float4(filterVertex.uv, 0, 0);
    #endif

    return output;
}

PackedVaryings vert(Attributes input)
{
    Varyings output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_Target
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);

    // SRGBToLinear is SRP Core's equivalent of the legacy GammaToLinearSpace.
    if (UIEFilterOutputsLinear())
        color.rgb = SRGBToLinear(color.rgb);

    return color;
}
