#ifndef UNIVERSAL_PARTICLESINSTANCING_INCLUDED
#define UNIVERSAL_PARTICLESINSTANCING_INCLUDED

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

void ParticleInstancingMatrices(out float4x4 objectToWorld, out float4x4 worldToObject)
{
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];

    // transform matrix
    objectToWorld._11_21_31_41 = float4(data.transform._11_21_31, 0.0f);
    objectToWorld._12_22_32_42 = float4(data.transform._12_22_32, 0.0f);
    objectToWorld._13_23_33_43 = float4(data.transform._13_23_33, 0.0f);
    objectToWorld._14_24_34_44 = float4(data.transform._14_24_34, 1.0f);

    // inverse transform matrix (TODO: replace with a library implementation if/when available)
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

void ParticleInstancingSetup()
{
    ParticleInstancingMatrices(unity_ObjectToWorld, unity_WorldToObject);
}

#else

void ParticleInstancingSetup() {}

#endif

#endif // UNIVERSAL_PARTICLESINSTANCING_INCLUDED
