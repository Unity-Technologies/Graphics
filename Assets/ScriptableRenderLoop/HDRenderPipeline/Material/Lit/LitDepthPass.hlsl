#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// Check if Alpha test is enabled. If it is, check if parallax is enabled on this material
#define NEED_TEXCOORD0 defined(_ALPHATEST_ON)
#define NEED_TANGENT_TO_WORLD NEED_TEXCOORD0 && (defined(_HEIGHTMAP) && !defined(_HEIGHTMAP_AS_DISPLACEMENT))

struct Attributes
{
    float3 positionOS : POSITION;
#if NEED_TEXCOORD0
    float2 uv0 : TEXCOORD0;
#endif
#if NEED_TANGENT_TO_WORLD
    float3 normalOS  : NORMAL;
    float4 tangentOS : TANGENT;
#endif
};

struct Varyings
{
    float4 positionCS;
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
    float4 interpolators[4] : TEXCOORD0;
#elif NEED_TEXCOORD0
    float4 interpolators[1] : TEXCOORD0;
#endif
};

// Function to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryings PackVaryings(Varyings input)
{
    PackedVaryings output;
    output.positionCS = input.positionCS;
#if NEED_TANGENT_TO_WORLD
    output.interpolators[0].xyz = input.positionWS.xyz;
    output.interpolators[1].xyz = input.tangentToWorld[0];
    output.interpolators[2].xyz = input.tangentToWorld[1];
    output.interpolators[3].xyz = input.tangentToWorld[2];

    output.interpolators[0].w = input.texCoord0.x;
    output.interpolators[1].w = input.texCoord0.y;
#elif NEED_TEXCOORD0
    output.interpolators[0] = float4(input.texCoord0, 0.0, 0.0);
#endif

    return output;
}

FragInputs UnpackVaryings(PackedVaryings input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    output.unPositionSS = input.positionCS; // input.positionCS is SV_Position

#if NEED_TANGENT_TO_WORLD
    output.positionWS.xyz = input.interpolators[0].xyz;
    output.tangentToWorld[0] = input.interpolators[1].xyz;
    output.tangentToWorld[1] = input.interpolators[2].xyz;
    output.tangentToWorld[2] = input.interpolators[3].xyz;

    output.texCoord0.xy = float2(input.interpolators[0].w, input.interpolators[1].w);
#elif NEED_TEXCOORD0
    output.texCoord0.xy = input.interpolators[0].xy;
#endif

    return output;
}

PackedVaryings Vert(Attributes input)
{
    Varyings output;

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

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
