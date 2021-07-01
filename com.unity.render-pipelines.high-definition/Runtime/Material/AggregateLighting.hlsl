// This file provides utilities and functions for aggregate lighting operations.

#ifndef AGGREGATE_LIGHTING_H
#define AGGREGATE_LIGHTING_H

#ifndef AGGREGATE_LIGHTING_ENABLE_16BIT_PACKING_SOFTWARE
#define AGGREGATE_LIGHTING_ENABLE_16BIT_PACKING_SOFTWARE 0
#endif

// These structure allow to accumulate lighting accross the Lit material
// AggregateLighting is init to zero and transfer to EvaluateBSDF, but the LightLoop can't access its content.
struct DirectLighting
{
    real3 diffuse;
    real3 specular;
};

struct IndirectLighting
{
    real3 specularReflected;
    real3 specularTransmitted;
};

struct AggregateLighting
{
    DirectLighting   direct;
    IndirectLighting indirect;
};

void AccumulateDirectLighting(DirectLighting src, inout AggregateLighting dst)
{
    dst.direct.diffuse += src.diffuse;
    dst.direct.specular += src.specular;
}

void AccumulateIndirectLighting(IndirectLighting src, inout AggregateLighting dst)
{
    dst.indirect.specularReflected += src.specularReflected;
    dst.indirect.specularTransmitted += src.specularTransmitted;
}

//Structure representing 2 packed lighting colors
struct PackedLightingColor2
{
#if AGGREGATE_LIGHTING_ENABLE_16BIT_PACKING_SOFTWARE
    uint3 m_data;
#else
    float3 m_first;
    float3 m_second;
#endif
};

void PackColor2(in float3 first, in float3 second, inout PackedLightingColor2 dst)
{
#if AGGREGATE_LIGHTING_ENABLE_16BIT_PACKING_SOFTWARE
        dst.m_data = uint3(
            f32tof16(first.x) | (f32tof16(second.x) << 16),
            f32tof16(first.y) | (f32tof16(second.y) << 16),
            f32tof16(first.z) | (f32tof16(second.z) << 16)
        );
#else
        dst.m_first = first;
        dst.m_second = second;
#endif
}

float3 UnpackFirstColor(in PackedLightingColor2 src)
{
#if AGGREGATE_LIGHTING_ENABLE_16BIT_PACKING_SOFTWARE
    return float3(f16tof32(src.m_data.x), f16tof32(src.m_data.y), f16tof32(src.m_data.z));
#else
    return src.m_first;
#endif
}

float3 UnpackSecondColor(in PackedLightingColor2 src)
{
#if AGGREGATE_LIGHTING_ENABLE_16BIT_PACKING_SOFTWARE
    return float3(f16tof32(src.m_data.x >> 16), f16tof32(src.m_data.y >> 16), f16tof32(src.m_data.z >> 16));
#else
    return src.m_second;
#endif
}

struct PackedDirectLighting
{
    PackedLightingColor2 m_data;
};

void PackDirect(float3 diffuse, float3 specular, inout PackedDirectLighting dst)
{
    PackColor2(diffuse, specular, dst.m_data);
}

float3 UnpackDiffuse(in PackedDirectLighting direct)
{
    return UnpackFirstColor(direct.m_data);
}

float3 UnpackSpecular(in PackedDirectLighting direct)
{
    return UnpackSecondColor(direct.m_data);
}

struct PackedIndirectLighting
{
    PackedLightingColor2 m_data;
};

void PackIndirect(float3 specularReflected, float3 specularTransmitted, inout PackedIndirectLighting dst)
{
    PackColor2(specularReflected, specularTransmitted, dst.m_data);
}

float3 UnpackSpecularReflected(in PackedIndirectLighting indirect)
{
    return UnpackFirstColor(indirect.m_data);
}

float3 UnpackSpecularTransmitted(in PackedIndirectLighting indirect)
{
    return UnpackSecondColor(indirect.m_data);
}

struct PackedAggregateLighting
{
    float2 m_multipliers;
    PackedDirectLighting direct;
    PackedIndirectLighting indirect;
};

void InitPackedAggregateLighting(inout PackedAggregateLighting self, float exposureMultiplier, float exposureMultiplierInv)
{
    ZERO_INITIALIZE(PackedAggregateLighting, self);
#if AGGREGATE_LIGHTING_ENABLE_16BIT_PACKING_SOFTWARE
    self.m_multipliers = float2(exposureMultiplier, exposureMultiplierInv);
#else
    self.m_multipliers = float2(1.0, 1.0);
#endif
}

void PackedAccumulatePackedDirect(in PackedDirectLighting src, inout PackedAggregateLighting dst)
{
    PackDirect(
        UnpackDiffuse(dst.direct) + UnpackDiffuse(src),
        UnpackSpecular(dst.direct) + UnpackSpecular(src),
        dst.direct);
}

void PackedAccumulateDirect(in DirectLighting src, inout PackedAggregateLighting dst)
{
    PackDirect(
        UnpackDiffuse(dst.direct) + src.diffuse * dst.m_multipliers.x,
        UnpackSpecular(dst.direct) + src.specular * dst.m_multipliers.x,
        dst.direct);
}

void PackedAccumulatePackedIndirect(in PackedIndirectLighting src, inout PackedAggregateLighting dst)
{
    PackIndirect(
        UnpackSpecularReflected(dst.indirect) + UnpackSpecularReflected(src),
        UnpackSpecularTransmitted(dst.indirect) + UnpackSpecularTransmitted(src),
        dst.indirect);
}

void PackedAccumulateIndirect(in IndirectLighting src, inout PackedAggregateLighting dst)
{
    PackIndirect(
        UnpackSpecularReflected(dst.indirect) + src.specularReflected * dst.m_multipliers.x,
        UnpackSpecularTransmitted(dst.indirect) + src.specularTransmitted * dst.m_multipliers.x,
        dst.indirect);
}

void UnpackAggregateLighting(in PackedAggregateLighting src, out AggregateLighting output)
{
    output.direct.diffuse = UnpackDiffuse(src.direct) * src.m_multipliers.y;
    output.direct.specular = UnpackSpecular(src.direct) * src.m_multipliers.y;
    output.indirect.specularReflected = UnpackSpecularReflected(src.indirect) * src.m_multipliers.y;
    output.indirect.specularTransmitted = UnpackSpecularTransmitted(src.indirect) * src.m_multipliers.y;
}


#endif
