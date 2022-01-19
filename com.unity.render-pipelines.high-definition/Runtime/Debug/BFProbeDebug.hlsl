#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

TEXTURECUBE_ARRAY(_InputCubemap);
SAMPLER(sampler_InputCubemap);

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 normalOS : TEXCOORD0;
    float3 cameraToSurfaceVecOS : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings Vert(Attributes IN)
{
    UNITY_SETUP_INSTANCE_ID(IN);

    float3 positionRWS = mul(UNITY_MATRIX_M, IN.positionOS).xyz;
    float3 cameraToSurfaceVecWS = positionRWS - GetCurrentViewPosition();
    float3 cameraToSurfaceVecOS = TransformWorldToObjectDir(cameraToSurfaceVecWS, false);

    Varyings OUT;
    OUT.positionCS = mul(UNITY_MATRIX_VP, float4(positionRWS, 1.f));
    OUT.normalOS = IN.normalOS;
    OUT.cameraToSurfaceVecOS = cameraToSurfaceVecOS;
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    return OUT;
}

void FragDepthOnly(Varyings IN)
{
}

float4 Frag(Varyings IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

#if UNITY_ANY_INSTANCING_ENABLED
    uint sliceIndex = unity_InstanceID;
#else
    uint sliceIndex = 0;
#endif

    float3 reflectedVecOS = reflect(IN.cameraToSurfaceVecOS, normalize(IN.normalOS));
    float4 col = SAMPLE_TEXTURECUBE_ARRAY(_InputCubemap, sampler_InputCubemap, reflectedVecOS, sliceIndex);

    return float4(col.xyz, 1.0f);
}
