struct VaryingsToPS
{
    VaryingsMeshToPS vmesh;
#ifdef VARYINGS_NEED_PASS
    VaryingsPassToPS vpass;
#endif
};

struct PackedVaryingsToPS
{
#ifdef VARYINGS_NEED_PASS
    PackedVaryingsPassToPS vpass;
#endif

    PackedVaryingsMeshToPS vmesh;

    // SGVs must be packed after all non-SGVs have been packed.
    // If there are several SGVs, they are packed in the order of HLSL declaration.

    UNITY_VERTEX_OUTPUT_STEREO

#if defined(PLATFORM_SUPPORTS_PRIMITIVE_ID_IN_PIXEL_SHADER) && SHADER_STAGE_FRAGMENT
#if (defined(VARYINGS_NEED_PRIMITIVEID) || (SHADERPASS == SHADERPASS_FULL_SCREEN_DEBUG))
    uint primitiveID : SV_PrimitiveID;
#endif
#endif

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryingsToPS PackVaryingsToPS(VaryingsToPS input)
{
    PackedVaryingsToPS output;
    output.vmesh = PackVaryingsMeshToPS(input.vmesh);
#ifdef VARYINGS_NEED_PASS
    output.vpass = PackVaryingsPassToPS(input.vpass);
#endif

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    return output;
}

FragInputs UnpackVaryingsToFragInputs(PackedVaryingsToPS packedInput)
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

#if defined(PLATFORM_SUPPORTS_PRIMITIVE_ID_IN_PIXEL_SHADER) && SHADER_STAGE_FRAGMENT
#if (defined(VARYINGS_NEED_PRIMITIVEID) || (SHADERPASS == SHADERPASS_FULL_SCREEN_DEBUG))
    input.primitiveID = packedInput.primitiveID;
#endif
#endif

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    input.isFrontFace = IS_FRONT_VFACE(packedInput.cullFace, true, false);
#endif

    return input;
}

#ifdef TESSELLATION_ON


struct VaryingsToDS
{
    VaryingsMeshToDS vmesh;
#ifdef VARYINGS_NEED_PASS
    VaryingsPassToDS vpass;
#endif
};

struct PackedVaryingsToDS
{
    PackedVaryingsMeshToDS vmesh;
#ifdef VARYINGS_NEED_PASS
    PackedVaryingsPassToDS vpass;
#endif
};

PackedVaryingsToDS PackVaryingsToDS(VaryingsToDS input)
{
    PackedVaryingsToDS output;
    output.vmesh = PackVaryingsMeshToDS(input.vmesh);
#ifdef VARYINGS_NEED_PASS
    output.vpass = PackVaryingsPassToDS(input.vpass);
#endif

    return output;
}

VaryingsToDS UnpackVaryingsToDS(PackedVaryingsToDS input)
{
    VaryingsToDS output;
    output.vmesh = UnpackVaryingsMeshToDS(input.vmesh);
#ifdef VARYINGS_NEED_PASS
    output.vpass = UnpackVaryingsPassToDS(input.vpass);
#endif

    return output;
}

VaryingsToDS InterpolateWithBaryCoordsToDS(VaryingsToDS input0, VaryingsToDS input1, VaryingsToDS input2, float3 baryCoords)
{
    VaryingsToDS output;

    output.vmesh = InterpolateWithBaryCoordsMeshToDS(input0.vmesh, input1.vmesh, input2.vmesh, baryCoords);
#ifdef VARYINGS_NEED_PASS
    output.vpass = InterpolateWithBaryCoordsPassToDS(input0.vpass, input1.vpass, input2.vpass, baryCoords);
#endif

    return output;
}

#endif // TESSELLATION_ON

#ifdef TESSELLATION_ON
#define VaryingsType VaryingsToDS
#define VaryingsMeshType VaryingsMeshToDS
#define PackedVaryingsType PackedVaryingsToDS
#define PackVaryingsType PackVaryingsToDS
#else
#define VaryingsType VaryingsToPS
#define VaryingsMeshType VaryingsMeshToPS
#define PackedVaryingsType PackedVaryingsToPS
#define PackVaryingsType PackVaryingsToPS
#endif

// TODO: Here we will also have all the vertex deformation (GPU skinning, vertex animation, morph target...) or we will need to generate a compute shaders instead (better! but require work to deal with unpacking like fp16)
VaryingsMeshType VertMesh(AttributesMesh input, float3 worldSpaceOffset
#ifdef HAVE_VFX_MODIFICATION
    , out AttributesElement element
#endif
)
{
    VaryingsMeshType output;
#if defined(USE_CUSTOMINTERP_SUBSTRUCT) || defined(HAVE_VFX_MODIFICATION)
    ZERO_INITIALIZE(VaryingsMeshType, output); // Only required with custom interpolator to quiet the shader compiler about not fully initialized struct
#endif

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

#ifdef HAVE_VFX_MODIFICATION
    ZERO_INITIALIZE(AttributesElement, element);

    if(!GetMeshAndElementIndex(input, element))
        return output; // Culled index.

    if(!GetInterpolatorAndElementData(output, element))
        return output; // Dead particle.

    SetupVFXMatrices(element, output);
#endif

#ifdef HAVE_MESH_MODIFICATION
    input = ApplyMeshModification(input, _TimeParameters.xyz
    #ifdef USE_CUSTOMINTERP_SUBSTRUCT
        // If custom interpolators are in use, we need to write them to the shader graph generated VaryingsMesh
        , output
    #endif
    #ifdef HAVE_VFX_MODIFICATION
        , element
    #endif
    );
#endif

    // This return the camera relative position (if enable)
    float3 positionRWS = TransformObjectToWorld(input.positionOS) + worldSpaceOffset;
#ifdef ATTRIBUTES_NEED_NORMAL
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#else
    float3 normalWS = float3(0.0, 0.0, 0.0); // We need this case to be able to compile ApplyVertexModification that doesn't use normal.
#endif

#ifdef ATTRIBUTES_NEED_TANGENT
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

    // Do vertex modification in camera relative space (if enable)
#if defined(HAVE_VERTEX_MODIFICATION)
    ApplyVertexModification(input, normalWS, positionRWS, _TimeParameters.xyz);
#endif

#ifdef TESSELLATION_ON
    output.positionRWS = positionRWS;
#ifdef VARYINGS_NEED_POSITIONPREDISPLACEMENT_WS
    output.positionPredisplacementRWS = positionRWS;
#endif
    // For tessellation we evaluate the tessellation factor from vertex shader then interpolate it in Hull Shader
    // Note: For unknow reason evaluating the tessellationFactor directly in Hull shader cause internal compiler issue for both Metal and Vulkan (Unity issue) when use with shadergraph
    // so we prefer this version to be compatible with all platforms, have same code for non shader graph and shader graph version and also it should be faster.
    output.tessellationFactor = GetTessellationFactor(input);
    output.normalWS = normalWS;
#if defined(VARYINGS_NEED_TANGENT_TO_WORLD) || defined(VARYINGS_DS_NEED_TANGENT)
    output.tangentWS = tangentWS;
#endif
#else // TESSELLATION_ON
#ifdef VARYINGS_NEED_POSITION_WS
    output.positionRWS = positionRWS;
#endif
#ifdef VARYINGS_NEED_POSITIONPREDISPLACEMENT_WS
    output.positionPredisplacementRWS = positionRWS;
#endif

    output.positionCS = TransformWorldToHClip(positionRWS);
#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.normalWS = normalWS;
    output.tangentWS = tangentWS;
#endif
#if !defined(SHADER_API_METAL) && defined(SHADERPASS) && (SHADERPASS == SHADERPASS_FULL_SCREEN_DEBUG)
    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VERTEX_DENSITY)
        IncrementVertexDensityCounter(output.positionCS);
#endif
#endif // TESSELLATION_ON

#if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
    output.texCoord0 = input.uv0;
#endif
#if defined(VARYINGS_NEED_TEXCOORD1) || defined(VARYINGS_DS_NEED_TEXCOORD1)
    output.texCoord1 = input.uv1;
#endif
#if defined(VARYINGS_NEED_TEXCOORD2) || defined(VARYINGS_DS_NEED_TEXCOORD2)
    output.texCoord2 = input.uv2;
#endif
#if defined(VARYINGS_NEED_TEXCOORD3) || defined(VARYINGS_DS_NEED_TEXCOORD3)
    output.texCoord3 = input.uv3;
#endif
#if defined(VARYINGS_NEED_COLOR) || defined(VARYINGS_DS_NEED_COLOR)
    output.color = input.color;
#endif

    return output;
}

VaryingsMeshType VertMesh(AttributesMesh input)
{
#ifdef HAVE_VFX_MODIFICATION
    AttributesElement element;
    return VertMesh(input, 0.0f, element);
#else
    return VertMesh(input, 0.0f);
#endif
}

#ifdef HAVE_VFX_MODIFICATION
VaryingsMeshType VertMesh(AttributesMesh input, out AttributesElement element)
{
    return VertMesh(input, 0.0f, element);
}
#endif

#ifdef TESSELLATION_ON

VaryingsMeshToPS VertMeshTesselation(VaryingsMeshToDS input)
{
    VaryingsMeshToPS output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = TransformWorldToHClip(input.positionRWS);

#if !defined(SHADER_API_METAL) && defined(SHADERPASS) && (SHADERPASS == SHADERPASS_FULL_SCREEN_DEBUG)
    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VERTEX_DENSITY)
        IncrementVertexDensityCounter(output.positionCS);
#endif

#ifdef VARYINGS_NEED_POSITION_WS
    output.positionRWS = input.positionRWS;
#endif
#ifdef VARYINGS_NEED_POSITIONPREDISPLACEMENT_WS
    output.positionPredisplacementRWS = input.positionPredisplacementRWS;
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.normalWS = input.normalWS;
    output.tangentWS = input.tangentWS;
#endif

#ifdef VARYINGS_NEED_TEXCOORD0
    output.texCoord0 = input.texCoord0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.texCoord1 = input.texCoord1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.texCoord2 = input.texCoord2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.texCoord3 = input.texCoord3;
#endif
#ifdef VARYINGS_NEED_COLOR
    output.color = input.color;
#endif

    // Call is last to deal with 'not completly initialize warning'. We don't want to ZeroInitialize the output struct to be able to detect issue.
#ifdef USE_CUSTOMINTERP_SUBSTRUCT
    // If custom interpolators are in use, we need to write them to the shader graph generated VaryingsMesh
    VertMeshTesselationCustomInterpolation(input, output);
#endif

    return output;
}

#endif // TESSELLATION_ON
