#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/BFProbe.cs.hlsl"

TEXTURE2D(_BFProbeStorage);

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
    uint probeIndex = unity_InstanceID;
#else
    uint probeIndex = 0;
#endif

    float3 reflectedVecOS = reflect(IN.cameraToSurfaceVecOS, normalize(IN.normalOS));

    float2 uvInProbe = saturate(0.5f*PackNormalOctQuadEncode(reflectedVecOS) + 0.5f);

    float2 probeXY = uint2(probeIndex % BFPROBECONFIG_STORAGE_WIDTH_IN_PROBES, probeIndex / BFPROBECONFIG_STORAGE_WIDTH_IN_PROBES);
    float2 uvInStorage = (probeXY*(float)BFPROBECONFIG_STORAGE_OCT_SIZE + 0.5f + uvInProbe*(float)(BFPROBECONFIG_STORAGE_OCT_SIZE - 1))
           /(float2(BFPROBECONFIG_STORAGE_WIDTH_IN_PROBES, BFPROBECONFIG_STORAGE_HEIGHT_IN_PROBES)*BFPROBECONFIG_STORAGE_OCT_SIZE);

    float4 col = SAMPLE_TEXTURE2D(_BFProbeStorage, s_linear_clamp_sampler, uvInStorage);

    return float4(col.xyz, 1.0f);
}
