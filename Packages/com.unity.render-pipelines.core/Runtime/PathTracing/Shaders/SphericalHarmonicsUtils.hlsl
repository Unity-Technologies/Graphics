#define SH_COLOR_CHANNELS 3
#define SH_COEFFICIENTS_PER_CHANNEL 9
#define SH_TOTAL_COEFFICIENTS (SH_COLOR_CHANNELS * SH_COEFFICIENTS_PER_CHANNEL)

#define SH_CHANNEL_RED 0
#define SH_CHANNEL_GREEN 1
#define SH_CHANNEL_BLUE 2

// 1/2 * sqrt(1/π)
#define SH_L0_NORMALIZATION 0.2820947917738781434740397257803862929220253146644994284220428608f
// 1/2 * sqrt(3/π)
#define SH_L1_NORMALIZATION 0.4886025119029199215863846228383470045758856081942277021382431574f
// sqrt(15/π)/2
#define SH_L2_2_NORMALIZATION 1.0925484305920790705433857058026884026904329595042589753478516999f
// sqrt(15/π)/2
#define SH_L2_1_NORMALIZATION SH_L2_2_NORMALIZATION
// sqrt(5/π)/4
#define SH_L20_NORMALIZATION 0.3153915652525200060308936902957104933242475070484115878434078878f
// sqrt(15/π)/2
#define SH_L21_NORMALIZATION SH_L2_2_NORMALIZATION
// sqrt(15/π)/4
#define SH_L22_NORMALIZATION 0.5462742152960395352716928529013442013452164797521294876739258499f

// Index into a buffer of floats representing contiguous L2 Spherical Harmonics coefficients, with separate channel and level.
uint SHIndex(uint probeIdx, uint channel, uint level)
{
    return probeIdx * SH_TOTAL_COEFFICIENTS + channel * SH_COEFFICIENTS_PER_CHANNEL + level;
}

// Index into a buffer of floats representing contiguous L2 Spherical Harmonics coefficients, with the coefficient index.
uint SHIndex(uint probeIdx, uint coefficient)
{
    return probeIdx * SH_TOTAL_COEFFICIENTS + coefficient;
}

// Basis function Y_0.
float SHL0()
{
    return SH_L0_NORMALIZATION;
}

// Basis function Y_1,-1.
float SHL1_1(float3 direction)
{
    return SH_L1_NORMALIZATION * direction.x;
}

// Basis function Y_1,0.
float SHL10(float3 direction)
{
    return SH_L1_NORMALIZATION * direction.y;
}

// Basis function Y_1,1.
float SHL11(float3 direction)
{
    return SH_L1_NORMALIZATION * direction.z;
}

// Basis function Y_2,-2.
float SHL2_2(float3 direction)
{
    return SH_L2_2_NORMALIZATION * direction.x * direction.y;
}

// Basis function Y_2,-1.
float SHL2_1(float3 direction)
{
    return SH_L2_1_NORMALIZATION * direction.y * direction.z;
}

// Basis function Y_2,0.
float SHL20(float3 direction)
{
    return SH_L20_NORMALIZATION * (3.0f * direction.z * direction.z - 1.0f);
}

// Basis function Y_2,1.
float SHL21(float3 direction)
{
    return SH_L21_NORMALIZATION * direction.x * direction.z;
}

// Basis function Y_2,2.
float SHL22(float3 direction)
{
    return SH_L22_NORMALIZATION * (direction.x * direction.x - direction.y * direction.y);
}
