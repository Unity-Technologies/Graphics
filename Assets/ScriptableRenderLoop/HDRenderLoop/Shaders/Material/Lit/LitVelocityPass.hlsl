#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#define NEED_TEXCOORD0 defined(_ALPHATEST_ON)
#define NEED_TANGENT_TO_WORLD NEED_TEXCOORD0 && (defined(_HEIGHTMAP) && !defined(_HEIGHTMAP_AS_DISPLACEMENT))

struct Attributes
{
    float3 positionOS : POSITION;
    float3 previousPositionOS : NORMAL; // Contain previous transform position (in case of skinning for example)
#if NEED_TEXCOORD0
    float2 uv0 : TEXCOORD0;
#endif
#if NEED_TANGENT_TO_WORLD
    float4 tangentOS : TANGENT;
#endif
};

struct Varyings
{
    float4 positionCS;
    // Note: Z component is not use
    float4 transferPositionCS;
    float4 transferPreviousPositionCS;
#if NEED_TEXCOORD0
    float2 texCoord0;
#endif
#if NEED_TANGENT_TO_WORLD
    float3 positionWS;
    float3 tangentToWorld[3];
#endif
};

struct PackedVaryings
{
    float4 positionCS : SV_Position;
#if NEED_TANGENT_TO_WORLD
    float4 interpolators[5] : TEXCOORD0;
#elif NEED_TEXCOORD0
    float4 interpolators[2] : TEXCOORD0;
#else
    float4 interpolators[2] : TEXCOORD0;
#endif
};

// Function to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryings PackVaryings(Varyings input)
{
    PackedVaryings output;
    output.positionCS = input.positionCS;
    output.interpolators[0].xyz = float4(input.transferPositionCS.xyw, 0.0);
    output.interpolators[1].xyz = float4(input.transferPreviousPositionCS.xyw, 0.0);

#if NEED_TANGENT_TO_WORLD
    output.interpolators[0].w = input.texCoord0.x;
    output.interpolators[1].w = input.texCoord0.y;

    output.interpolators[2].xyz = input.tangentToWorld[0];
    output.interpolators[3].xyz = input.tangentToWorld[1];
    output.interpolators[4].xyz = input.tangentToWorld[2];

    output.interpolators[2].w = input.positionWS.x;
    output.interpolators[3].w = input.positionWS.y;
    output.interpolators[4].w = input.positionWS.z;
#elif NEED_TEXCOORD0
    output.interpolators[0].w = input.texCoord0.x;
    output.interpolators[1].w = input.texCoord0.y;
#endif

    return output;
}

FragInput UnpackVaryings(PackedVaryings input)
{
    FragInput output;
    ZERO_INITIALIZE(FragInput, output);

    output.unPositionSS = input.positionCS;  // as input we have the vpos
    output.positionCS = float4(input.interpolators[0].xy, 0.0, input.interpolators[0].z);
    output.previousPositionCS = float4(input.interpolators[1].xy, 0.0, input.interpolators[1].z);

#if NEED_TANGENT_TO_WORLD
    output.texCoord0.xy = float2(input.interpolators[0].w, input.interpolators[1].w);
    output.positionWS.xyz = float2(input.interpolators[2].w, input.interpolators[3].w, input.interpolators[4].w);
    output.tangentToWorld[0] = input.interpolators[2].xyz;
    output.tangentToWorld[1] = input.interpolators[3].xyz;
    output.tangentToWorld[2] = input.interpolators[4].xyz;
#elif NEED_TEXCOORD0
    output.texCoord0.xy = float2(input.interpolators[0].w, input.interpolators[1].w);
#endif

    return output;
}

PackedVaryings Vert(Attributes input)
{
    Varyings output;

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

    // TODO: Clean this code, put in function ?
    output.transferPositionCS = mul(_NonJitteredVP, mul(unity_ObjectToWorld, input.positionOS));
    output.transferPreviousPositionCS = mul(_PreviousVP, mul(_PreviousM, _HasLastPositionData ? float4(input.previousPositionOS, 1.0) : input.positionOS));

#if NEED_TEXCOORD0
    output.texCoord0 = input.uv0;
#endif

#if NEED_TANGENT_TO_WORLD
    output.positionWS = positionWS;

    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    output.tangentToWorld[0] = tangentToWorld[0];
    output.tangentToWorld[1] = tangentToWorld[1];
    output.tangentToWorld[2] = tangentToWorld[2];
#endif

    return PackVaryings(output);
}
