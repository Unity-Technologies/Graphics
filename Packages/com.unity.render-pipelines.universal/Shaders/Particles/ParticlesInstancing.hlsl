#ifndef UNIVERSAL_PARTICLESINSTANCING_INCLUDED
#define UNIVERSAL_PARTICLESINSTANCING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParticlesInstancing.hlsl"

#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)

// Kept for backward compatibility with custom shaders that call it directly.
void ParticleInstancingMatrices(out float4x4 objectToWorld, out float4x4 worldToObject)
{
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];

    // transform matrix
    objectToWorld._11_21_31_41 = float4(data.transform._11_21_31, 0.0f);
    objectToWorld._12_22_32_42 = float4(data.transform._12_22_32, 0.0f);
    objectToWorld._13_23_33_43 = float4(data.transform._13_23_33, 0.0f);
    objectToWorld._14_24_34_44 = float4(data.transform._14_24_34, 1.0f);

    // inverse transform matrix
    float3x3 worldToObject3x3;
    worldToObject3x3[0] = objectToWorld[1].yzx * objectToWorld[2].zxy - objectToWorld[1].zxy * objectToWorld[2].yzx;
    worldToObject3x3[1] = objectToWorld[0].zxy * objectToWorld[2].yzx - objectToWorld[0].yzx * objectToWorld[2].zxy;
    worldToObject3x3[2] = objectToWorld[0].yzx * objectToWorld[1].zxy - objectToWorld[0].zxy * objectToWorld[1].yzx;

    float det = dot(objectToWorld[0].xyz, worldToObject3x3[0]);

    worldToObject3x3 = transpose(worldToObject3x3);

    worldToObject3x3 *= rcp(det);

    float3 worldToObjectPosition = mul(worldToObject3x3, -objectToWorld._14_24_34);

    worldToObject._11_21_31_41 = float4(worldToObject3x3._11_21_31, 0.0f);
    worldToObject._12_22_32_42 = float4(worldToObject3x3._12_22_32, 0.0f);
    worldToObject._13_23_33_43 = float4(worldToObject3x3._13_23_33, 0.0f);
    worldToObject._14_24_34_44 = float4(worldToObjectPosition, 1.0f);
}

// Builds VertexPositionInputs from the particle instance transform directly,
// bypassing GetVertexPositionInputs() which reads UNITY_MATRIX_M.
VertexPositionInputs ParticleInstancingGetVertexPositionInputs(float3x4 transform, float3 positionOS)
{
    VertexPositionInputs vertexInput;
    vertexInput.positionWS = ParticleInstancingTransformPosition(transform, positionOS);
    vertexInput.positionVS = TransformWorldToView(vertexInput.positionWS);
    vertexInput.positionCS = TransformWorldToHClip(vertexInput.positionWS);
    float4 ndc = vertexInput.positionCS * 0.5f;
    vertexInput.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    vertexInput.positionNDC.zw = vertexInput.positionCS.zw;
    return vertexInput;
}

// Builds VertexNormalInputs from the particle instance transform directly,
// bypassing GetVertexNormalInputs() which reads UNITY_MATRIX_I_M.
// Tangents transform with the forward 3x3 (covariant, like directions).
// Normals require the inverse-transpose: cofactor(m) equals det(m)*m^{-T};
// after SafeNormalize this gives the correct world normal under non-uniform scale.
// Matches TransformParticleMesh in the ShaderGraph package (Particles.hlsl).
VertexNormalInputs ParticleInstancingGetVertexNormalInputs(float3x4 transform, float3 normalOS, float4 tangentOS)
{
    ParticleInstancingTransformNormalTangent(transform, normalOS, tangentOS.xyz);

    VertexNormalInputs tbn;
    tbn.normalWS  = SafeNormalize(normalOS);
    tbn.tangentWS = real3(SafeNormalize(tangentOS.xyz));

    real sign = real(tangentOS.w) * GetOddNegativeScale();
    tbn.bitangentWS = real3(cross(tbn.normalWS, float3(tbn.tangentWS))) * sign;
    return tbn;
}

#endif

VertexPositionInputs GetParticleVertexPositionInputs(float3 positionOS)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    return ParticleInstancingGetVertexPositionInputs(unity_ParticleInstanceData[unity_InstanceID].transform, positionOS);
#else
    return GetVertexPositionInputs(positionOS);
#endif
}

VertexNormalInputs GetParticleVertexNormalInputs(float3 normalOS, float4 tangentOS)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    return ParticleInstancingGetVertexNormalInputs(unity_ParticleInstanceData[unity_InstanceID].transform, normalOS, tangentOS);
#else
    return GetVertexNormalInputs(normalOS, tangentOS);
#endif
}

VertexNormalInputs GetParticleVertexNormalInputs(float3 normalOS)
{
    return GetParticleVertexNormalInputs(normalOS, float4(0, 0, 0, 1));
}

#endif // UNIVERSAL_PARTICLESINSTANCING_INCLUDED
