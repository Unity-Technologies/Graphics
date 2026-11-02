#ifndef CUSTOM_PARTICLESINSTANCING_INCLUDED
#define CUSTOM_PARTICLESINSTANCING_INCLUDED

#ifndef UNITY_PARTICLE_INSTANCE_DATA
#define UNITY_PARTICLE_INSTANCE_DATA CustomParticleInstanceData
#define UNITY_PARTICLE_INSTANCE_DATA_NO_ANIM_FRAME
#endif

struct CustomParticleInstanceData
{
    float3x4 transform;
    uint color;
    float agePercent;
};

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParticlesInstancing.hlsl"

///<funchints>
///     <sg:ProviderKey>Custom.Particles.AgePercent</sg:ProviderKey>
///     <sg:DisplayName>Particle Age</sg:DisplayName>
///     <sg:SearchCategory>VFX/Particles</sg:SearchCategory>
///     <sg:SearchName>Age</sg:SearchName>
///     <sg:SearchTerms>Particle, Age</sg:SearchTerms>
///</funchints>
UNITY_EXPORT_REFLECTION
void GetParticleAge(inout float agePercent)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];
    agePercent = data.agePercent;
#elif defined (SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)
    agePercent = 0.5;
#endif
}

#endif // CUSTOM_PARTICLESINSTANCING_INCLUDED
