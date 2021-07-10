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
    //16 bit data
    uint3 m_data;

    //flat data
    float3 m_first;
    float3 m_second;
};

void PackColor2(bool enable16bitPacking, in float3 first, in float3 second, inout PackedLightingColor2 dst)
{
    if (enable16bitPacking)
    {
        dst.m_data = uint3(
            f32tof16(first.x) | (f32tof16(second.x) << 16),
            f32tof16(first.y) | (f32tof16(second.y) << 16),
            f32tof16(first.z) | (f32tof16(second.z) << 16)
        );
    }
    else
    {
        dst.m_first = first;
        dst.m_second = second;
    }
}

float3 UnpackFirstColor(int enable16bitPacking, in PackedLightingColor2 src)
{
    if (enable16bitPacking)
        return float3(f16tof32(src.m_data.x), f16tof32(src.m_data.y), f16tof32(src.m_data.z));
    else
        return src.m_first;
}

float3 UnpackSecondColor(int enable16bitPacking, in PackedLightingColor2 src)
{
    if (enable16bitPacking)
        return float3(f16tof32(src.m_data.x >> 16), f16tof32(src.m_data.y >> 16), f16tof32(src.m_data.z >> 16));
    else
        return src.m_second;
}

struct PackedDirectLighting
{
    PackedLightingColor2 m_data;
};

void PackDirect(int enable16bitPacking, float3 diffuse, float3 specular, inout PackedDirectLighting dst)
{
    PackColor2(enable16bitPacking, diffuse, specular, dst.m_data);
}

float3 UnpackDiffuse(int enable16bitPacking, in PackedDirectLighting direct)
{
    return UnpackFirstColor(enable16bitPacking, direct.m_data);
}

float3 UnpackSpecular(int enable16bitPacking, in PackedDirectLighting direct)
{
    return UnpackSecondColor(enable16bitPacking, direct.m_data);
}

struct PackedIndirectLighting
{
    PackedLightingColor2 m_data;
};

void PackIndirect(int enable16bitPacking, float3 specularReflected, float3 specularTransmitted, inout PackedIndirectLighting dst)
{
    PackColor2(enable16bitPacking, specularReflected, specularTransmitted, dst.m_data);
}

float3 UnpackSpecularReflected(int enable16bitPacking, in PackedIndirectLighting indirect)
{
    return UnpackFirstColor(enable16bitPacking, indirect.m_data);
}

float3 UnpackSpecularTransmitted(int enable16bitPacking, in PackedIndirectLighting indirect)
{
    return UnpackSecondColor(enable16bitPacking, indirect.m_data);
}

struct PackedAggregateLighting
{
    int m_enable16bitPacking;
    float2 m_multipliers;
    PackedDirectLighting direct;
    PackedIndirectLighting indirect;
};

void InitPackedAggregateLighting(inout PackedAggregateLighting self, float exposureMultiplier, int enable16bitPacking, float exposureMultiplierInv)
{
    ZERO_INITIALIZE(PackedAggregateLighting, self);
#if AGGREGATE_LIGHTING_ENABLE_16BIT_PACKING_SOFTWARE
    self.m_enable16bitPacking = enable16bitPacking;
#else
    self.m_enable16bitPacking = 0;
#endif

    if (self.m_enable16bitPacking)
        self.m_multipliers = float2(exposureMultiplier, exposureMultiplierInv);
    else
        self.m_multipliers = float2(1.0, 1.0);
}

void PackedAccumulatePackedDirect(in PackedDirectLighting src, inout PackedAggregateLighting dst)
{
    int enabled = dst.m_enable16bitPacking;
    PackDirect(
        enabled,
        UnpackDiffuse(enabled, dst.direct) + UnpackDiffuse(enabled, src),
        UnpackSpecular(enabled, dst.direct) + UnpackSpecular(enabled, src),
        dst.direct);
}

void PackedAccumulateDirect(in DirectLighting src, inout PackedAggregateLighting dst)
{
    int enabled = dst.m_enable16bitPacking;
    PackDirect(
        enabled, 
        UnpackDiffuse(enabled, dst.direct) + src.diffuse * dst.m_multipliers.x,
        UnpackSpecular(enabled, dst.direct) + src.specular * dst.m_multipliers.x,
        dst.direct);
}

void PackedAccumulatePackedIndirect(in PackedIndirectLighting src, inout PackedAggregateLighting dst)
{
    int enabled = dst.m_enable16bitPacking;
    PackIndirect(
        enabled,
        UnpackSpecularReflected(enabled, dst.indirect) + UnpackSpecularReflected(enabled, src),
        UnpackSpecularTransmitted(enabled, dst.indirect) + UnpackSpecularTransmitted(enabled, src),
        dst.indirect);
}

void PackedAccumulateIndirect(in IndirectLighting src, inout PackedAggregateLighting dst)
{
    int enabled = dst.m_enable16bitPacking;
    PackIndirect(
        enabled,
        UnpackSpecularReflected(enabled, dst.indirect) + src.specularReflected * dst.m_multipliers.x,
        UnpackSpecularTransmitted(enabled, dst.indirect) + src.specularTransmitted * dst.m_multipliers.x,
        dst.indirect);
}

void UnpackAggregateLighting(in PackedAggregateLighting src, out AggregateLighting output)
{
    int enabled = src.m_enable16bitPacking;
    output.direct.diffuse = UnpackDiffuse(enabled, src.direct) * src.m_multipliers.y;
    output.direct.specular = UnpackSpecular(enabled, src.direct) * src.m_multipliers.y;
    output.indirect.specularReflected = UnpackSpecularReflected(enabled, src.indirect) * src.m_multipliers.y;
    output.indirect.specularTransmitted = UnpackSpecularTransmitted(enabled, src.indirect) * src.m_multipliers.y;
}


#endif
