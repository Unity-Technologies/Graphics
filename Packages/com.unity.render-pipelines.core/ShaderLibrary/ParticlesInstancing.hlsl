#ifndef CORE_PARTICLESINSTANCING_INCLUDED
#define CORE_PARTICLESINSTANCING_INCLUDED

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && !defined(SHADER_TARGET_SURFACE_ANALYSIS)
#define UNITY_PARTICLE_INSTANCING_ENABLED
#endif

#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)

#ifndef UNITY_PARTICLE_INSTANCE_DATA
#define UNITY_PARTICLE_INSTANCE_DATA DefaultParticleInstanceData
#endif

struct DefaultParticleInstanceData
{
    float3x4 transform;
    uint color;
    float animFrame;
};

StructuredBuffer<UNITY_PARTICLE_INSTANCE_DATA> unity_ParticleInstanceData;
float4 unity_ParticleUVShiftData;
half unity_ParticleUseMeshColors;

// Transforms object-space position to world-space using the 3x4 particle transform.
float3 ParticleInstancingTransformPosition(float3x4 transform, float3 positionOS)
{
    return mul(transform, float4(positionOS, 1.0));
}

// Transforms object-space position to world-space using the 3x4 particle transform.
void ParticleInstancingTransformNormalTangent(float3x4 transform, inout float3 normal, inout float3 tangent)
{
    float3x3 m = (float3x3)transform;

    float3x3 cofactors;
    cofactors[0] = m[1].yzx * m[2].zxy - m[1].zxy * m[2].yzx;
    cofactors[1] = m[0].zxy * m[2].yzx - m[0].yzx * m[2].zxy;
    cofactors[2] = m[0].yzx * m[1].zxy - m[0].zxy * m[1].yzx;

    normal = mul(cofactors, normal);
    tangent = mul(m, tangent);
}

#endif

// Particle transforms are now applied directly in vertex shaders via the helpers above.
void ParticleInstancingSetup() {}

#include "ShaderApiReflectionSupport.hlsl"

///<funchints>
///     <sg:ProviderKey>Core.Particles.Transforms</sg:ProviderKey>
///     <sg:DisplayName>Particle Transforms</sg:DisplayName>
///     <sg:SearchCategory>VFX/Particles</sg:SearchCategory>
///     <sg:SearchName>Transforms</sg:SearchName>
///     <sg:SearchTerms>Particle, Transforms</sg:SearchTerms>
///</funchints>
///<paramhints name = "position">
///     <sg:DisplayName>Position</sg:DisplayName>
///     <Position />
///     <Default>ObjectSpace</Default>
///</paramhints>
///<paramhints name = "normal">
///     <sg:DisplayName>Normal</sg:DisplayName>
///     <Normal />
///     <Default>ObjectSpace</Default>
///</paramhints>
///<paramhints name = "tangent">
///     <sg:DisplayName>Tangent</sg:DisplayName>
///     <Tangent />
///     <Default>ObjectSpace</Default>
///</paramhints>
UNITY_EXPORT_REFLECTION
void TransformParticleMesh(inout float3 position, inout float3 normal, inout float3 tangent)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];
    position = ParticleInstancingTransformPosition(data.transform, position);
    ParticleInstancingTransformNormalTangent(data.transform, normal, tangent);
#endif
}

#endif // CORE_PARTICLESINSTANCING_INCLUDED
