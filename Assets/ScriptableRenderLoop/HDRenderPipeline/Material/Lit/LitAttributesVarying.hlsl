struct Attributes
{
    float3 positionOS   : POSITION;
#ifdef ATTRIBUTES_WANT_NORMAL	
    float3 normalOS     : NORMAL;
#endif
#ifdef ATTRIBUTES_WANT_TANGENT
    float4 tangentOS    : TANGENT; // Store sign in w
#endif
#ifdef ATTRIBUTES_WANT_UV0	
    float2 uv0          : TEXCOORD0;
#endif
#ifdef ATTRIBUTES_WANT_UV1
    float2 uv1		    : TEXCOORD1;
#endif
#ifdef ATTRIBUTES_WANT_UV2
    float2 uv2		    : TEXCOORD2;
#endif
#ifdef ATTRIBUTES_WANT_UV3
    float2 uv3		    : TEXCOORD3;
#endif
#ifdef ATTRIBUTES_WANT_COLOR
    float4 color        : COLOR;
#endif
    // UNITY_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS;
#ifdef VARYING_WANT_POSITION_WS
    float3 positionWS;
#endif
#ifdef VARYING_WANT_TANGENT_TO_WORLD
    float3 tangentToWorld[3];
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
    float2 texCoord3;
#endif
#ifdef VARYING_WANT_COLOR   
    float4 color;
#endif
};

struct PackedVaryings
{
    float4 positionCS : SV_Position;

#ifdef VARYING_WANT_TANGENT_TO_WORLD
    #ifdef VARYING_WANT_POSITION_WS
    // if present, pack positionWS
    float4 interpolators1 : TEXCOORD1;
    float4 interpolators2 : TEXCOORD2;
    float4 interpolators3 : TEXCOORD3;
    #else
    float3 interpolators1 : TEXCOORD1;
    float3 interpolators2 : TEXCOORD2;
    float3 interpolators3 : TEXCOORD3;
    #endif
#else
    #ifdef VARYING_WANT_POSITION_WS
    float3 interpolators0 : TEXCOORD0;
    #endif
#endif

    // Allocate only necessary space if shader compiler in the future are able to automatically pack
#ifdef VARYING_WANT_TEXCOORD1
    float4 interpolators4 : TEXCOORD3;
#elif defined(VARYING_WANT_TEXCOORD0)
    float2 interpolators4 : TEXCOORD3;
#endif

#ifdef VARYING_WANT_TEXCOORD3
    float4 interpolators5 : TEXCOORD4;
#elif defined(VARYING_WANT_TEXCOORD2)
    float2 interpolators5 : TEXCOORD4;
#endif

#ifdef VARYING_WANT_COLOR
    float4 interpolators6 : TEXCOORD6;
#endif

#if defined(VARYING_WANT_CULLFACE) && SHADER_STAGE_FRAGMENT
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMATIC;
#endif
};

// Functions to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryings PackVaryings(Varyings input)
{
    PackedVaryings output;

    output.positionCS = input.positionCS;

#ifdef VARYING_WANT_TANGENT_TO_WORLD
    output.interpolators1.xyz = input.tangentToWorld[0];
    output.interpolators2.xyz = input.tangentToWorld[1];
    output.interpolators3.xyz = input.tangentToWorld[2];
    #ifdef VARYING_WANT_POSITION_WS
    output.interpolators1.w = input.positionWS.x;
    output.interpolators2.w = input.positionWS.y;
    output.interpolators3.w = input.positionWS.z;
    #else
    output.interpolators1.w = 0.0;
    output.interpolators2.w = 0.0;
    output.interpolators3.w = 0.0;
    #endif
#else
    #ifdef VARYING_WANT_POSITION_WS
    output.interpolators0.xyz = input.positionWS;
    output.interpolators0.w = 0.0;
    #endif
#endif

#ifdef VARYING_WANT_TEXCOORD0 
    output.interpolators4.xy = input.texCoord0;
#endif
#ifdef VARYING_WANT_TEXCOORD1
    output.interpolators4.zw = input.texCoord1;
#endif
#ifdef VARYING_WANT_TEXCOORD2
    output.interpolators5.xy = input.texCoord2;
#endif
#ifdef VARYING_WANT_TEXCOORD3
    output.interpolators5.zw = input.texCoord3;
#endif

#ifdef VARYING_WANT_COLOR
    output.interpolators6 = input.color;
#endif

    return output;
}

FragInputs UnpackVaryings(PackedVaryings input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    output.unPositionSS = input.positionCS; // input.positionCS is SV_Position

#ifdef VARYING_WANT_TANGENT_TO_WORLD
    output.tangentToWorld[0] = input.interpolators1.xyz;
    output.tangentToWorld[1] = input.interpolators2.xyz;
    output.tangentToWorld[2] = input.interpolators3.xyz;
    #ifdef WANT_PER_PIXEL_WORLDPOS
    output.positionWS.xyz = float3(input.interpolators1.w, input.interpolators2.w, input.interpolators3.w);
    #endif
#else
    #ifdef VARYING_WANT_POSITION_WS
    output.positionWS.xyz = input.interpolators0.xyz;
    #endif
#endif

#ifdef VARYING_WANT_TEXCOORD0 
    output.texCoord0 = input.interpolators4.xy;
#endif
#ifdef VARYING_WANT_TEXCOORD1
    output.texCoord1 = input.interpolators4.zw;
#endif
#ifdef VARYING_WANT_TEXCOORD2
    output.texCoord2 = input.interpolators5.xy;
#endif
#ifdef VARYING_WANT_TEXCOORD3
    output.texCoord3 = input.interpolators5.zw;
#endif
#ifdef VARYING_WANT_COLOR
    output.color = input.interpolators6;
#endif

#if defined(VARYING_WANT_CULLFACE) && SHADER_STAGE_FRAGMENT
    output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
#endif

    return output;
}

#ifdef TESSELLATION_ON

// Varying DS - use for domain shader
// We can deduce these define from the other define
// We need to pass to DS any varying required by pixel shader
// If we have required an attribute it mean we will use it at least for DS
#ifdef VARYING_WANT_TANGENT_TO_WORLD
#define VARYING_DS_WANT_NORMAL
#define VARYING_DS_WANT_TANGENT
#endif
#if defined(VARYING_WANT_TEXCOORD0) || defined(ATTRIBUTES_WANT_UV0)
#define VARYING_DS_WANT_TEXCOORD0
#endif
#if defined(VARYING_WANT_TEXCOORD1) || defined(ATTRIBUTES_WANT_UV1)
#define VARYING_DS_WANT_TEXCOORD1
#endif
#if defined(VARYING_WANT_TEXCOORD2) || defined(ATTRIBUTES_WANT_UV2)
#define VARYING_DS_WANT_TEXCOORD2
#endif
#if defined(VARYING_WANT_TEXCOORD3) || defined(ATTRIBUTES_WANT_UV3)
#define VARYING_DS_WANT_TEXCOORD3
#endif
#if defined(VARYING_WANT_COLOR) || defined(ATTRIBUTES_WANT_COLOR)
float4 VARYING_DS_WANT_COLOR;
#endif

#endif // TESSELLATION_ON

// Varying for domain shader
// Position and normal are always present (for tessellation) and in world space
struct VaryingsDS
{
    float3 positionWS;
#ifdef VARYING_DS_WANT_NORMAL
    float3 normalWS;
#endif
#ifdef VARYING_DS_WANT_TANGENT
    float4 tangentWS;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD0 
    float2 texCoord0;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD1
    float2 texCoord1;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD2
    float2 texCoord2;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD3
    float2 texCoord3;
#endif
#ifdef VARYING_DS_WANT_COLOR
    float4 color;
#endif
};

struct PackedVaryingsDS
{
    float3 interpolators0 : INTERNALTESSPOS; // positionWS

#ifdef VARYING_DS_WANT_NORMAL
    float3 interpolators1 : NORMAL;
#endif
#ifdef VARYING_DS_WANT_TANGENT
    float4 interpolators2 : TANGENT;
#endif

    // Allocate only necessary space if shader compiler in the future are able to automatically pack
#ifdef VARYING_DS_WANT_TEXCOORD1
    float4 interpolators3 : TEXCOORD0;
#elif defined(VARYING_DS_WANT_TEXCOORD0)
    float2 interpolators3 : TEXCOORD0;
#endif

#ifdef VARYING_DS_WANT_TEXCOORD3
    float4 interpolators4 : TEXCOORD1;
#elif defined(VARYING_DS_WANT_TEXCOORD2)
    float2 interpolators4 : TEXCOORD1;
#endif

#ifdef VARYING_DS_WANT_COLOR
    float4 interpolators5 : TEXCOORD2;
#endif
};

// Functions to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryingsDS PackVaryingsDS(VaryingsDS input)
{
    PackedVaryingsDS output;

    output.interpolators0 = input.positionWS;
#ifdef VARYING_DS_WANT_NORMAL
    output.interpolators1 = input.normalWS;
#endif
#ifdef VARYING_DS_WANT_TANGENT
    output.interpolators2 = input.tangentWS;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD0 
    output.interpolators3.xy = input.texCoord0;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD1
    output.interpolators3.zw = input.texCoord1;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD2
    output.interpolators4.xy = input.texCoord2;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD3
    output.interpolators4.zw = input.texCoord3;
#endif
#ifdef VARYING_DS_WANT_COLOR
    output.interpolators5 = input.color;
#endif

    return output;
}

VaryingsDS UnpackVaryingsDS(PackedVaryingsDS input)
{
    VaryingsDS output;

    output.positionWS = input.interpolators0;
#ifdef VARYING_DS_WANT_NORMAL
    output.normalWS = input.interpolators1;
#endif
#ifdef VARYING_DS_WANT_TANGENT
    output.tangentWS = input.interpolators2;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD0 
    output.texCoord0 = input.interpolators3.xy;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD1
    output.texCoord1 = input.interpolators3.zw;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD2
    output.texCoord2 = input.interpolators4.xy;
#endif
#ifdef VARYING_DS_WANT_TEXCOORD3
    output.texCoord3 = input.interpolators4.zw;
#endif
#ifdef VARYING_DS_WANT_COLOR
    output.color = input.interpolators5;
#endif

    return output;
}

VaryingsDS InterpolateWithBaryCoords(VaryingsDS input0, VaryingsDS input1, VaryingsDS input2, float3 baryCoords)
{
    VaryingsDS ouput;

    TESSELLATION_INTERPOLATE_BARY(positionWS, baryCoords);
#ifdef VARYING_DS_WANT_NORMAL
    TESSELLATION_INTERPOLATE_BARY(normalWS, baryCoords);
#endif
#ifdef VARYING_DS_WANT_TANGENT
    TESSELLATION_INTERPOLATE_BARY(tangentWS, baryCoords);
#endif
#ifdef VARYING_DS_WANT_TEXCOORD0 
    TESSELLATION_INTERPOLATE_BARY(texCoord0, baryCoords);
#endif
#ifdef VARYING_DS_WANT_TEXCOORD1
    TESSELLATION_INTERPOLATE_BARY(texCoord1, baryCoords);
#endif
#ifdef VARYING_DS_WANT_TEXCOORD2 
    TESSELLATION_INTERPOLATE_BARY(texCoord2, baryCoords);
#endif
#ifdef VARYING_DS_WANT_TEXCOORD3 
    TESSELLATION_INTERPOLATE_BARY(texCoord3, baryCoords);
#endif
#ifdef VARYING_DS_WANT_COLOR 
    TESSELLATION_INTERPOLATE_BARY(color, baryCoords);
#endif

    return ouput;
}

#endif // TESSELLATION_ON

#ifdef TESSELLATION_ON
#define VaryingsType VaryingsDS
#define PackedVaryingsType PackedVaryingsDS
#define PackVaryingsType PackVaryingsDS
#else
#define VaryingsType Varyings
#define PackedVaryingsType PackedVaryings
#define PackVaryingsType PackVaryings
#endif
