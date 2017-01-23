struct AttributesMesh
{
    float3 positionOS   : POSITION;
#ifdef ATTRIBUTES_NEED_NORMAL	
    float3 normalOS     : NORMAL;
#endif
#ifdef ATTRIBUTES_NEED_TANGENT
    float4 tangentOS    : TANGENT; // Store sign in w
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD0	
    float2 uv0          : TEXCOORD0;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD1
    float2 uv1		    : TEXCOORD1;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD2
    float2 uv2		    : TEXCOORD2;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD3
    float2 uv3		    : TEXCOORD3;
#endif
#ifdef ATTRIBUTES_NEED_COLOR
    float4 color        : COLOR;
#endif

    // UNITY_INSTANCE_ID
};

struct VaryingsMeshToPS
{
    float4 positionCS;
#ifdef VARYINGS_NEED_POSITION_WS
    float3 positionWS;
#endif
#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    float3 tangentToWorld[3];
#endif
#ifdef VARYINGS_NEED_TEXCOORD0
    float2 texCoord0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    float2 texCoord1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    float2 texCoord2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    float2 texCoord3;
#endif
#ifdef VARYINGS_NEED_COLOR   
    float4 color;
#endif
};

struct PackedVaryingsMeshToPS
{
    float4 positionCS : SV_Position;

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    #ifdef VARYINGS_NEED_POSITION_WS
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
    #ifdef VARYINGS_NEED_POSITION_WS
    float3 interpolators0 : TEXCOORD0;
    #endif
#endif

    // Allocate only necessary space if shader compiler in the future are able to automatically pack
#ifdef VARYINGS_NEED_TEXCOORD1
    float4 interpolators4 : TEXCOORD4;
#elif defined(VARYINGS_NEED_TEXCOORD0)
    float2 interpolators4 : TEXCOORD4;
#endif

#ifdef VARYINGS_NEED_TEXCOORD3
    float4 interpolators5 : TEXCOORD5;
#elif defined(VARYINGS_NEED_TEXCOORD2)
    float2 interpolators5 : TEXCOORD5;
#endif

#ifdef VARYINGS_NEED_COLOR
    float4 interpolators6 : TEXCOORD6;
#endif

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMATIC;
#endif
};

// Functions to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryingsMeshToPS PackVaryingsMeshToPS(VaryingsMeshToPS input)
{
    PackedVaryingsMeshToPS output;

    output.positionCS = input.positionCS;

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.interpolators1.xyz = input.tangentToWorld[0];
    output.interpolators2.xyz = input.tangentToWorld[1];
    output.interpolators3.xyz = input.tangentToWorld[2];
    #ifdef VARYINGS_NEED_POSITION_WS
    output.interpolators1.w = input.positionWS.x;
    output.interpolators2.w = input.positionWS.y;
    output.interpolators3.w = input.positionWS.z;
    #endif
#else
    #ifdef VARYINGS_NEED_POSITION_WS
    output.interpolators0 = input.positionWS;
    #endif
#endif

#ifdef VARYINGS_NEED_TEXCOORD0 
    output.interpolators4.xy = input.texCoord0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.interpolators4.zw = input.texCoord1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.interpolators5.xy = input.texCoord2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.interpolators5.zw = input.texCoord3;
#endif

#ifdef VARYINGS_NEED_COLOR
    output.interpolators6 = input.color;
#endif

    return output;
}

FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
{
    FragInputs output = InitializeFragInputs();

    output.unPositionSS = input.positionCS; // input.positionCS is SV_Position

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.tangentToWorld[0] = input.interpolators1.xyz;
    output.tangentToWorld[1] = input.interpolators2.xyz;
    output.tangentToWorld[2] = input.interpolators3.xyz;
    #ifdef VARYINGS_NEED_POSITION_WS
    output.positionWS.xyz = float3(input.interpolators1.w, input.interpolators2.w, input.interpolators3.w);
    #endif
#else
    #ifdef VARYINGS_NEED_POSITION_WS
    output.positionWS.xyz = input.interpolators0.xyz;
    #endif
#endif

#ifdef VARYINGS_NEED_TEXCOORD0 
    output.texCoord0 = input.interpolators4.xy;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.texCoord1 = input.interpolators4.zw;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.texCoord2 = input.interpolators5.xy;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.texCoord3 = input.interpolators5.zw;
#endif
#ifdef VARYINGS_NEED_COLOR
    output.color = input.interpolators6;
#endif

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
#endif

    return output;
}

#ifdef TESSELLATION_ON

// Varying DS - use for domain shader
// We can deduce these defines from the other defines
// We need to pass to DS any varying required by pixel shader
// If we have required an attributes that is not present in varyings it mean we will be for DS
#if defined(VARYINGS_NEED_TANGENT_TO_WORLD) || defined(ATTRIBUTES_NEED_TANGENT)
#define VARYINGS_DS_NEED_TANGENT
#endif
#if defined(VARYINGS_NEED_TEXCOORD0) || defined(ATTRIBUTES_NEED_TEXCOORD0)
#define VARYINGS_DS_NEED_TEXCOORD0
#endif
#if defined(VARYINGS_NEED_TEXCOORD1) || defined(ATTRIBUTES_NEED_TEXCOORD1)
#define VARYINGS_DS_NEED_TEXCOORD1
#endif
#if defined(VARYINGS_NEED_TEXCOORD2) || defined(ATTRIBUTES_NEED_TEXCOORD2)
#define VARYINGS_DS_NEED_TEXCOORD2
#endif
#if defined(VARYINGS_NEED_TEXCOORD3) || defined(ATTRIBUTES_NEED_TEXCOORD3)
#define VARYINGS_DS_NEED_TEXCOORD3
#endif
#if defined(VARYINGS_NEED_COLOR) || defined(ATTRIBUTES_NEED_COLOR)
#define VARYINGS_DS_NEED_COLOR
#endif

// Varying for domain shader
// Position and normal are always present (for tessellation) and in world space
struct VaryingsMeshToDS
{
    float3 positionWS;
    float3 normalWS;
#ifdef VARYINGS_DS_NEED_TANGENT
    float4 tangentWS;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0 
    float2 texCoord0;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    float2 texCoord1;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    float2 texCoord2;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    float2 texCoord3;
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    float4 color;
#endif
#ifdef _TESSELLATION_OBJECT_SCALE
    float3 objectScale;
#endif
};

struct PackedVaryingsMeshToDS
{
    float3 interpolators0 : INTERNALTESSPOS; // positionWS
    float3 interpolators1 : NORMAL; // NormalWS

#ifdef VARYINGS_DS_NEED_TANGENT
    float4 interpolators2 : TANGENT;
#endif

    // Allocate only necessary space if shader compiler in the future are able to automatically pack
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    float4 interpolators3 : TEXCOORD0;
#elif defined(VARYINGS_DS_NEED_TEXCOORD0)
    float2 interpolators3 : TEXCOORD0;
#endif

#ifdef VARYINGS_DS_NEED_TEXCOORD3
    float4 interpolators4 : TEXCOORD1;
#elif defined(VARYINGS_DS_NEED_TEXCOORD2)
    float2 interpolators4 : TEXCOORD1;
#endif

#ifdef VARYINGS_DS_NEED_COLOR
    float4 interpolators5 : TEXCOORD2;
#endif

#ifdef _TESSELLATION_OBJECT_SCALE
    float3 interpolators6 : TEXCOORD3;
#endif
};

// Functions to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryingsMeshToDS PackVaryingsMeshToDS(VaryingsMeshToDS input)
{
    PackedVaryingsMeshToDS output;

    output.interpolators0 = input.positionWS;
    output.interpolators1 = input.normalWS;
#ifdef VARYINGS_DS_NEED_TANGENT
    output.interpolators2 = input.tangentWS;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0 
    output.interpolators3.xy = input.texCoord0;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    output.interpolators3.zw = input.texCoord1;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    output.interpolators4.xy = input.texCoord2;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    output.interpolators4.zw = input.texCoord3;
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    output.interpolators5 = input.color;
#endif
#ifdef _TESSELLATION_OBJECT_SCALE
    output.interpolators6 = input.objectScale;
#endif

    return output;
}

VaryingsMeshToDS UnpackVaryingsMeshToDS(PackedVaryingsMeshToDS input)
{
    VaryingsMeshToDS output;

    output.positionWS = input.interpolators0;
    output.normalWS = input.interpolators1;
#ifdef VARYINGS_DS_NEED_TANGENT
    output.tangentWS = input.interpolators2;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0 
    output.texCoord0 = input.interpolators3.xy;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    output.texCoord1 = input.interpolators3.zw;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    output.texCoord2 = input.interpolators4.xy;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    output.texCoord3 = input.interpolators4.zw;
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    output.color = input.interpolators5;
#endif
#ifdef _TESSELLATION_OBJECT_SCALE
    output.objectScale = input.interpolators6;
#endif
    return output;
}

VaryingsMeshToDS InterpolateWithBaryCoordsMeshToDS(VaryingsMeshToDS input0, VaryingsMeshToDS input1, VaryingsMeshToDS input2, float3 baryCoords)
{
    VaryingsMeshToDS ouput;

    TESSELLATION_INTERPOLATE_BARY(positionWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(normalWS, baryCoords);
#ifdef VARYINGS_DS_NEED_TANGENT
    // This will interpolate the sign but should be ok in practice as we may expect a triangle to have same sign (? TO CHECK)
    TESSELLATION_INTERPOLATE_BARY(tangentWS, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0 
    TESSELLATION_INTERPOLATE_BARY(texCoord0, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    TESSELLATION_INTERPOLATE_BARY(texCoord1, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2 
    TESSELLATION_INTERPOLATE_BARY(texCoord2, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3 
    TESSELLATION_INTERPOLATE_BARY(texCoord3, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_COLOR 
    TESSELLATION_INTERPOLATE_BARY(color, baryCoords);
#endif

#ifdef _TESSELLATION_OBJECT_SCALE
    // objectScale doesn't change for the whole object.
    ouput.objectScale = input0.objectScale;
#endif

    return ouput;
}

#endif // TESSELLATION_ON
