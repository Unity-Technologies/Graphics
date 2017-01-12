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

#define ATTRIBUTES_WANT_NORMAL
#define ATTRIBUTES_WANT_UV0
#define ATTRIBUTES_WANT_UV1
#define ATTRIBUTES_WANT_UV2

#define VARYING_WANT_TEXCOORD0
#define VARYING_WANT_TEXCOORD1

// Include structure declaration and packing functions
#include "LitAttributesVarying.hlsl"

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
