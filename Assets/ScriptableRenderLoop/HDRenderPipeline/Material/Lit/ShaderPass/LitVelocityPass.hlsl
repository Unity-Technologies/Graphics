#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// TODO: For now disable per pixel and per vertex displacement mapping with motion vector
// as vertex normal is not available + force UV0 for alpha test (not compatible with layered system...)
#define VARYING_WANT_POSITION_WS
#define VARYING_WANT_PASS_SPECIFIC

#if defined(_ALPHATEST_ON)
#define ATTRIBUTES_WANT_UV0
    #ifdef LAYERED_LIT_SHADER
    #define ATTRIBUTES_WANT_UV1
        #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
        #define ATTRIBUTES_WANT_UV2
        #endif
        #if defined(_REQUIRE_UV3)
        #define ATTRIBUTES_WANT_UV3
        #endif
    #endif
#endif

#if defined(_ALPHATEST_ON)
#define VARYING_WANT_TEXCOORD0
    #ifdef LAYERED_LIT_SHADER
    #define VARYING_WANT_TEXCOORD1
        #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
        #define VARYING_WANT_TEXCOORD2
        #endif
        #if defined(_REQUIRE_UV3)
        #define VARYING_WANT_TEXCOORD3
        #endif
    #endif
#endif

// Available semantic start from TEXCOORD4
struct AttributesPass
{
    float3 previousPositionOS : NORMAL; // Contain previous transform position (in case of skinning for example)
};

struct VaryingsPass
{
    // Note: Z component is not use
    float4 transferPositionCS;
    float4 transferPreviousPositionCS;
};

// Available interpolator start from TEXCOORD8
struct PackedVaryingsPass
{
    // Note: Z component is not use
    float3 interpolators0 : TEXCOORD8
    float3 interpolators1 : TEXCOORD9;
};

PackedVaryingsPass PackVaryingsPass(VaryingsPass input)
{
    PackedVaryingsPass output;
    output.interpolators0 = float3(input.transferPositionCS.xyw);
    output.interpolators1 = float3(input.transferPreviousPositionCS.xyw);
}

VaryingsPass UnpackVaryingsPass(PackedVaryingsPass input)
{
    PackedVaryingsPass output;
    output.interpolators0 = float3(input.transferPositionCS.xyw);
    output.interpolators1 = float3(input.transferPreviousPositionCS.xyw);
}


FragInputs UnpackVaryings(PackedVaryings input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    output.unPositionSS = input.positionCS; // input.positionCS is SV_Position
    output.positionWS = input.interpolators[0].xyz;
    output.positionCS = float4(input.interpolators[1].xy, 0.0, input.interpolators[1].z);
    output.previousPositionCS = float4(input.interpolators[1].xy, 0.0, input.interpolators[2].z);

#if NEED_TANGENT_TO_WORLD
    output.texCoord0.xy = float2(input.interpolators[0].w, input.interpolators[1].w);
    output.tangentToWorld[0] = input.interpolators[3].xyz;
    output.tangentToWorld[1] = input.interpolators[4].xyz;
    output.tangentToWorld[2] = float3(input.interpolators[2].w, input.interpolators[3].w, input.interpolators[4].w);
#elif NEED_TEXCOORD0
    output.texCoord0.xy = float2(input.interpolators[0].w, input.interpolators[1].w);
#endif

    return output;
}

PackedVaryings Vert(Attributes input)
{
    Varyings output;

    output.positionWS = TransformObjectToWorld(input.positionOS);
    // TODO deal with camera center rendering and instancing (This is the reason why we always perform tow steps transform to clip space + instancing matrix)
    output.positionCS = TransformWorldToHClip(output.positionWS);

    // TODO: Clean this code, put in function ?
    output.transferPositionCS = mul(_NonJitteredVP, mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)));
    output.transferPreviousPositionCS = mul(_PreviousVP, mul(_PreviousM, _HasLastPositionData ? float4(input.previousPositionOS, 1.0) : float4(input.positionOS, 1.0)));

#if NEED_TEXCOORD0
    output.texCoord0 = input.uv0;
#endif

#if NEED_TANGENT_TO_WORLD
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    output.tangentToWorld[0] = tangentToWorld[0];
    output.tangentToWorld[1] = tangentToWorld[1];
    output.tangentToWorld[2] = tangentToWorld[2];
#endif

    return PackVaryings(output);
}
