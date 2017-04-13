struct VaryingsToPS
{
    VaryingsMeshToPS vmesh;
#ifdef VARYINGS_NEED_PASS
    VaryingsPassToPS vpass;
#endif
};

struct PackedVaryingsToPS
{
    PackedVaryingsMeshToPS vmesh;
#ifdef VARYINGS_NEED_PASS
    PackedVaryingsPassToPS vpass;
#endif
};

PackedVaryingsToPS PackVaryingsToPS(VaryingsToPS input)
{
    PackedVaryingsToPS output;
    output.vmesh = PackVaryingsMeshToPS(input.vmesh);
#ifdef VARYINGS_NEED_PASS
    output.vpass = PackVaryingsPassToPS(input.vpass);
#endif

    return output;
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
VaryingsMeshType VertMesh(AttributesMesh input)
{
    VaryingsMeshType output;

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    float3 normalWS = float3(0.0, 0.0, 0.0);
    float4 tangentWS = float4(0.0, 0.0, 0.0, 0.0);
    float4 vertexColor = float4(0.0, 0.0, 0.0, 0.0);

#if (defined(VARYINGS_NEED_TANGENT_TO_WORLD) || defined(TESSELLATION_ON)) && (SHADERPASS != SHADERPASS_VELOCITY)
    normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif

#if defined(VARYINGS_NEED_TANGENT_TO_WORLD) || defined(VARYINGS_DS_NEED_TANGENT)
    tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

#if defined(VARYINGS_NEED_COLOR) || defined(VARYINGS_DS_NEED_COLOR)
    vertexColor = input.color;
#endif

#if _VERTEX_WIND
    float3 rootWP = mul(GetObjectToWorldMatrix(), float4(0, 0, 0, 1)).xyz;
    ApplyWind(positionWS, normalWS, rootWP, _Stiffness, _Drag, _ShiverDrag, _ShiverDirectionality, _InitialBend, vertexColor.a, _Time);
#endif

#ifdef TESSELLATION_ON
    output.positionWS = positionWS;
    #ifdef _TESSELLATION_OBJECT_SCALE
    // Extract scaling from world transform
    float4x4 worldTransform = GetObjectToWorldMatrix();
    output.objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    output.objectScale.y = length(float3(worldTransform._m10, worldTransform._m11, worldTransform._m12));
    output.objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));
    #endif
    // TODO: deal with camera center rendering and instancing (This is the reason why we always perform tow steps transform to clip space + instancing matrix)
    // TODO: TEMP: Velocity has a flow as it doens't have normal. This need to be fix. In the mean time, generate fix normal so compiler doesn't complain - When fix, think to also enable ATTRIBUTES_NEED_NORMAL in LitVelocityPass.hlsl
    #if SHADERPASS == SHADERPASS_VELOCITY
    output.normalWS = float3(0.0, 0.0, 1.0);
    #else
    output.normalWS = normalWS;
    #endif
    #if defined(VARYINGS_NEED_TANGENT_TO_WORLD) || defined(VARYINGS_DS_NEED_TANGENT)
    output.tangentWS = tangentWS;
    #endif
#else

    // TODO deal with camera center rendering and instancing (This is the reason why we always perform tow steps transform to clip space + instancing matrix)
    #ifdef VARYINGS_NEED_POSITION_WS
    output.positionWS = positionWS;
    #endif
    output.positionCS = TransformWorldToHClip(positionWS);
    #ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.normalWS = normalWS;
    output.tangentWS = tangentWS;
    #endif
#endif

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
    output.color = vertexColor;
#endif

    return output;
}

#ifdef TESSELLATION_ON

VaryingsMeshToPS VertMeshTesselation(VaryingsMeshToDS input)
{
    VaryingsMeshToPS output;

    output.positionCS = TransformWorldToHClip(input.positionWS);

#ifdef VARYINGS_NEED_POSITION_WS
    output.positionWS = input.positionWS;
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

    return output;
}

#endif // TESSELLATION_ON
