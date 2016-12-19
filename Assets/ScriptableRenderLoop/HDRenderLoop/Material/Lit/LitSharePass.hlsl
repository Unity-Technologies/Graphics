#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

//-------------------------------------------------------------------------------------
// Attribute/Varying
//-------------------------------------------------------------------------------------

#define WANT_UV2 (DYNAMICLIGHTMAP_ON) || (SHADERPASS == SHADERPASS_DEBUG_VIEW_MATERIAL) || defined(_REQUIRE_UV2_OR_UV3)
#define WANT_UV3 (SHADERPASS == SHADERPASS_DEBUG_VIEW_MATERIAL) || defined(_REQUIRE_UV2_OR_UV3)

struct Attributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv0          : TEXCOORD0;
    float2 uv1		    : TEXCOORD1;
#if WANT_UV2
    float2 uv2		    : TEXCOORD2;
#endif
#if WANT_UV3
    float2 uv3		    : TEXCOORD3;
#endif
    float4 tangentOS    : TANGENT;  // Always present as we require it also in case of anisotropic lighting
    float4 color        : COLOR;

    // UNITY_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS;
    float3 positionWS;
    float2 texCoord0;
    float2 texCoord1;
#if WANT_UV2
    float2 texCoord2;
#endif
#if WANT_UV3
    float2 texCoord3;
#endif
    float3 tangentToWorld[3];
    float4 color;
};

struct PackedVaryings
{
    float4 positionCS : SV_Position;
#if (WANT_UV2) || (WANT_UV3)
    float4 interpolators[6] : TEXCOORD0;
#else
    float4 interpolators[5] : TEXCOORD0;
#endif

#if SHADER_STAGE_FRAGMENT
    #if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMATIC;
    #endif
#endif
};

// Function to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryings PackVaryings(Varyings input)
{
    PackedVaryings output;
    output.positionCS = input.positionCS;
    output.interpolators[0].xyz = input.positionWS.xyz;
    output.interpolators[1].xyz = input.tangentToWorld[0];
    output.interpolators[2].xyz = input.tangentToWorld[1];
    output.interpolators[3].xyz = input.tangentToWorld[2];

    output.interpolators[0].w = input.texCoord0.x;
    output.interpolators[1].w = input.texCoord0.y;
    output.interpolators[2].w = input.texCoord1.x;
    output.interpolators[3].w = input.texCoord1.y;

    output.interpolators[4] = input.color;

#if (WANT_UV2) || (WANT_UV3)
    output.interpolators[5] = float4(0.0, 0.0, 0.0, 0.0);

#if WANT_UV2
    output.interpolators[5].xy = input.texCoord2.xy;
#endif

#if WANT_UV3
    output.interpolators[5].zw = input.texCoord3.xy;
#endif

#endif

    return output;
}

FragInputs UnpackVaryings(PackedVaryings input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    output.unPositionSS = input.positionCS; // input.positionCS is SV_Position
    output.positionWS.xyz = input.interpolators[0].xyz;
    output.tangentToWorld[0] = input.interpolators[1].xyz;
    output.tangentToWorld[1] = input.interpolators[2].xyz;
    output.tangentToWorld[2] = input.interpolators[3].xyz;

    output.texCoord0.xy = float2(input.interpolators[0].w, input.interpolators[1].w);
    output.texCoord1.xy = float2(input.interpolators[2].w, input.interpolators[3].w);

    output.vertexColor = input.interpolators[4];

#if WANT_UV2
    output.texCoord2 = input.interpolators[5].xy;
#endif
#if WANT_UV3
    output.texCoord3 = input.interpolators[5].zw;
#endif

#if SHADER_STAGE_FRAGMENT
    #if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
    #endif
#endif

    return output;
}

//-------------------------------------------------------------------------------------
// Vertex shader
//-------------------------------------------------------------------------------------

// TODO: Here we will also have all the vertex deformation (GPU skinning, vertex animation, morph target...) or we will need to generate a compute shaders instead (better! but require work to deal with unpacking like fp16)
PackedVaryings VertDefault(Attributes input)
{
    Varyings output;

    output.positionWS = TransformObjectToWorld(input.positionOS);
    // TODO deal with camera center rendering and instancing (This is the reason why we always perform tow steps transform to clip space + instancing matrix)
    output.positionCS = TransformWorldToHClip(output.positionWS);

    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    output.texCoord0 = input.uv0;
    output.texCoord1 = input.uv1;
#if WANT_UV2
    output.texCoord2 = input.uv2;
#endif

#if WANT_UV3
	output.texCoord3 = input.uv3;
#endif

    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    output.tangentToWorld[0] = tangentToWorld[0];
    output.tangentToWorld[1] = tangentToWorld[1];
    output.tangentToWorld[2] = tangentToWorld[2];

    output.color = input.color;

    return PackVaryings(output);
}
