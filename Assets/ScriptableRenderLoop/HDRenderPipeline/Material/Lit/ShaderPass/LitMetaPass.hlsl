#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif
CBUFFER_START(UnityMetaPass)
// x = use uv1 as raster position
// y = use uv2 as raster position
bool4 unity_MetaVertexControl;

// x = return albedo
// y = return normal
bool4 unity_MetaFragmentControl;

CBUFFER_END

// This was not in constant buffer in original unity, so keep outiside. But should be in as ShaderRenderPass frequency
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
};

struct Varyings
{
    float4 positionCS;
    float2 texCoord0;
    float2 texCoord1;
};

struct PackedVaryings
{
    float4 positionCS : SV_Position;
    float4 interpolators[1] : TEXCOORD0;
};

// Function to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryings PackVaryings(Varyings input)
{
    PackedVaryings output;
    output.positionCS = input.positionCS;
    output.interpolators[0].xy = input.texCoord0;
    output.interpolators[0].zw = input.texCoord1;

    return output;
}

FragInputs UnpackVaryings(PackedVaryings input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    output.unPositionSS = input.positionCS;  // input.positionCS is SV_Position
    output.texCoord0 = input.interpolators[0].xy;
    output.texCoord1 = input.interpolators[0].zw;

    return output;
}

PackedVaryings Vert(Attributes input)
{
    Varyings output;

    // Output UV coordinate in vertex shader
    if (unity_MetaVertexControl.x)
    {
        input.positionOS.xy = input.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        //v.positionOS.z = vertex.z > 0 ? 1.0e-4f : 0.0f;
    }
    if (unity_MetaVertexControl.y)
    {
        input.positionOS.xy = input.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        //v.positionOS.z = vertex.z > 0 ? 1.0e-4f : 0.0f;
    }

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.texCoord0 = input.uv0;
    output.texCoord1 = input.uv1;

    return PackVaryings(output);
}
