#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#define ATTRIBUTES_TESSELATION_WANT_UV (defined(TESSELLATION_ON) && (defined(_TESSELATION_DISPLACEMENT) || defined(_TESSELATION_DISPLACEMENT_PHONG)))

#define ATTRIBUTES_WANT_NORMAL defined(TESSELLATION_ON) || (defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT))
#define ATTRIBUTES_WANT_TANGENT defined(TESSELLATION_ON) || (defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT))
#define ATTRIBUTES_WANT_UV0 ATTRIBUTES_TESSELATION_WANT_UV || defined(_ALPHATEST_ON))
#define ATTRIBUTES_WANT_UV1 (WANT_UV0) && !defined(LAYERED_LIT_SHADER) // Layered shader can use any UV
#define ATTRIBUTES_WANT_UV2 (WANT_UV0 && _REQUIRE_UV2_OR_UV3) && !defined(LAYERED_LIT_SHADER) // Layered shader can use any UV
#define ATTRIBUTES_WANT_UV3 ATTRIBUTES_WANT_UV2

struct Attributes
{
    float3 positionOS   : POSITION;
#if ATTRIBUTES_WANT_NORMAL	
    float3 normalOS     : NORMAL;
#endif
#if ATTRIBUTES_WANT_UV0	
    float2 uv0          : TEXCOORD0;
#endif
#if ATTRIBUTES_WANT_UV1
    float2 uv1		    : TEXCOORD1;
#endif
#if ATTRIBUTES_WANT_UV2
    float2 uv2		    : TEXCOORD2;
#endif
#if ATTRIBUTES_WANT_UV3
    float2 uv3		    : TEXCOORD3;
#endif
#if ATTRIBUTES_WANT_TANGENT
    float4 tangentOS    : TANGENT;  // Always present as we require it also in case of anisotropic lighting
#endif
#if ATTRIBUTES_WANT_COLOR
    float4 color        : COLOR;
#endif

    // UNITY_INSTANCE_ID
};

#define VARYINGDS_WANT_TEXCOORD0 ATTRIBUTES_WANT_UV0

struct VaryingsDS
{
    float3 positionWS;
#ifdef VARYINGDS_WANT_TEXCOORD0 
    float2 texCoord0;
#endif
#ifdef VARYINGDS_WANT_TEXCOORD1
    float2 texCoord1;
#endif
#ifdef VARYINGDS_WANT_TEXCOORD2
    float2 texCoord2;
#endif
#ifdef VARYINGDS_WANT_TEXCOORD3
    float2 texCoord2;
#endif
#ifdef VARYINGDS_WANT_TANGENT_TO_WORLD
    float3 tangentToWorld[3];
#endif
#ifdef VARYINGDS_WANT_COLOR
    float4 color;
#endif
};

struct PackedVaryingsDS
{
#if VARYING_DS_WANT_TEXCOORD0 || VARYING_DS_WANT_TEXCOORD1
    float4 interpolators1 : TEXCOORD1;
#endif
#if VARYING_DS_WANT_TEXCOORD2 || VARYING_DS_WANT_TEXCOORD3
    float4 interpolators2 : TEXCOORD2;
#endif
#if VARYING_DS_WANT_TANGENT_TO_WORLD
    // if present, pack positionWS
    float4 interpolators3 : TEXCOORD3;
    float4 interpolators4 : TEXCOORD4;
    float4 interpolators5 : TEXCOORD5;
#else
    float3 interpolators0 : TEXCOORD0; // positionWS
#endif
#if VARYING_DS_WANT_COLOR
    float4 interpolators6 : TEXCOORD6;
#endif
};

#define VARYING_WANT_WORLDPOS
#define VARYING_WANT_TEXCOORD0
#define VARYING_WANT_TEXCOORD1
#define VARYING_WANT_TEXCOORD2
#define VARYING_WANT_TEXCOORD3

struct Varyings
{
    float4 positionCS;
#ifdef VARYING_WANT_WORLDPOS
    float3 positionWS;
#endif
#ifdef VARYING_WANT_TEXCOORD0
    float2 texCoord0;
#endif
#ifdef VARYING_WANT_TEXCOORD1
    float2 texCoord1;
#endif
#ifdef VARYING_WANT_TEXCOORD2
    float2 texCoord2;
#endif
#ifdef VARYING_WANT_TEXCOORD3
    float2 texCoord2;
#endif
    float3 tangentToWorld[3];
    float4 color;
};

struct PackedVaryings
{
    float4 positionCS : SV_Position;
#if defined(VARYING_WANT_TEXCOORD0) || defined(VARYING_WANT_TEXCOORD1)
    float4 interpolators1 : TEXCOORD1;
#endif
#if defined(VARYING_WANT_TEXCOORD2) || defined(VARYING_WANT_TEXCOORD3)
    float4 interpolators2 : TEXCOORD2;
#endif
#if defined(VARYING_WANT_TANGENT_TO_WORLD)
    #ifdef VARYING_WANT_WORLDPOS
    // if present, pack positionWS
    float4 interpolators3 : TEXCOORD3;
    float4 interpolators4 : TEXCOORD4;
    float4 interpolators5 : TEXCOORD5;
    #else
    float3 interpolators3 : TEXCOORD3;
    float3 interpolators4 : TEXCOORD4;
    float3 interpolators5 : TEXCOORD5;
    #endif
#else
#ifdef VARYING_WANT_WORLDPOS
    float4 interpolators1 : TEXCOORD0;
#endif
#endif
#ifdef VARYING_WANT_COLOR
    float4 interpolators6 : TEXCOORD6;
#endif
};


#ifdef TESSELLATION_ON

AttributesTessellation InterpolateWithBaryCoords(AttributesTessellation input0, AttributesTessellation input1, AttributesTessellation input2, float3 baryCoords)
{
    AttributesTessellation ouput;

    TESSELLATION_INTERPOLATE_BARY(positionOS, baryCoords);
#if NEED_TEXCOORD0
    TESSELLATION_INTERPOLATE_BARY(uv0, baryCoords);
#endif
#if NEED_TANGENT_TO_WORLD
    TESSELLATION_INTERPOLATE_BARY(normalOS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(tangentOS, baryCoords);
#endif

    return ouput;
}
#endif // TESSELLATION_ON


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
