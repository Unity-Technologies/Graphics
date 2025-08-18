#if SHADERPASS != SHADERPASS_FOG_VOLUME_PREVIEW
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/HDRenderPipeline.VolumetricLighting.cs.hlsl"

struct VertexToFragment
{
    float4 positionCS : SV_POSITION;
    float3 viewDirectionWS : TEXCOORD0;
};

VertexToFragment Vert(uint instanceId : INSTANCEID_SEMANTIC, uint vertexId : VERTEXID_SEMANTIC)
{
    VertexToFragment output;
    ZERO_INITIALIZE(VertexToFragment, output);

    // Workaround because we can't customize the ShaderGraph preview
    // To ensure that we always draw a box volume with fog inside, we do a fullscreen quad in the ShaderGraph preview
    // This works because in most case the min vertex count of the models in SG preview is 4.
    if (vertexId < 3)
    {
        output.positionCS = GetFullScreenTriangleVertexPosition(vertexId);
        float3 positionWS = ComputeWorldSpacePosition(output.positionCS, UNITY_MATRIX_I_VP);
        output.viewDirectionWS = GetWorldSpaceViewDir(positionWS);
    }

    return output;
}

void Frag(VertexToFragment v2f, out float4 outColor : SV_Target0)
{
    // For now it's not possible to have a ShaderGraph preview of fog
    outColor = 0;
}
