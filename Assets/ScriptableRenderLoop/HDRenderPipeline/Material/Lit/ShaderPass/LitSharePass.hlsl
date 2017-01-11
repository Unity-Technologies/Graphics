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

#ifdef TESSELATION_ON
// Copy paste of above struct with POSITION rename to INTERNALTESSPOS (internal of unity shader compiler)
struct AttributesTesselation
{
    float3 positionOS   : INTERNALTESSPOS;
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
};

AttributesTesselation AttributesToAttributesTesselation(Attributes input)
{
    AttributesTesselation output;
    output.positionOS = input.positionOS;
    output.normalOS = input.normalOS;
    output.uv0 = input.uv0;
    output.uv1 = input.uv1;
#if WANT_UV2
    output.uv2 = input.uv2;
#endif
#if WANT_UV3
    output.uv3 = input.uv3;
#endif
    output.tangentOS = input.tangentOS;
    output.color = input.color;

    return output;
}

Attributes AttributesTesselationToAttributes(AttributesTesselation input)
{
    Attributes output;
    output.positionOS = input.positionOS;
    output.normalOS = input.normalOS;
    output.uv0 = input.uv0;
    output.uv1 = input.uv1;
#if WANT_UV2
    output.uv2 = input.uv2;
#endif
#if WANT_UV3
    output.uv3 = input.uv3;
#endif
    output.tangentOS = input.tangentOS;
    output.color = input.color;

    return output;
}

AttributesTesselation InterpolateWithBaryCoords(AttributesTesselation input0, AttributesTesselation input1, AttributesTesselation input2, float3 baryCoords)
{
    AttributesTesselation ouput;

    TESSELATION_INTERPOLATE_BARY(positionOS, baryCoords);
    TESSELATION_INTERPOLATE_BARY(normalOS, baryCoords);
    TESSELATION_INTERPOLATE_BARY(uv0, baryCoords);
    TESSELATION_INTERPOLATE_BARY(uv1, baryCoords);
#if WANT_UV2
    TESSELATION_INTERPOLATE_BARY(uv2, baryCoords);
#endif
#if WANT_UV3
    TESSELATION_INTERPOLATE_BARY(uv3, baryCoords);
#endif
    TESSELATION_INTERPOLATE_BARY(tangentOS, baryCoords);
    TESSELATION_INTERPOLATE_BARY(color, baryCoords);

    return ouput;
}
#endif // TESSELATION_ON


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
PackedVaryings Vert(Attributes input)
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
