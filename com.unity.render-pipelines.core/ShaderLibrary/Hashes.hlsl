void Hash_Tchou_2_1_uint(uint2 v, out uint o)
{
    // ~6 alu (2 mul)
    v.y ^= 1103515245U;
    v.x += v.y;
    v.x *= v.y;
    v.x ^= v.x >> 5u;
    v.x *= 0x27d4eb2du;
    o = v.x;
}

void Hash_Tchou_2_1_float(float2 i, out float o)
{
    uint r;
    uint2 v = (uint2) (int2) round(i);
    Hash_Tchou_2_1_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_2_1_half(half2 i, out half o)
{
    uint r;
    uint2 v = (uint2) (int2) round(i);
    Hash_Tchou_2_1_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_2_3_uint(uint2 q, out uint3 o)
{
    // ~10 alu (2 mul)
    uint3 v;
    v.xy = q;
    v.y ^= 1103515245U;
    v.x += v.y;
    v.x *= v.y;
    v.x ^= v.x >> 5u;
    v.x *= 0x27d4eb2du;
    v.y ^= (v.x << 3u);
    v.z = v.x ^ (v.y << 5u);
    o = v;
}

void Hash_Tchou_2_3_float(float2 i, out float3 o)
{
    uint3 r;
    uint2 v = (uint2) (int2) round(i);
    Hash_Tchou_2_3_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_2_3_half(half2 i, out half3 o)
{
    uint3 r;
    uint2 v = (uint2) (int2) round(i);
    Hash_Tchou_2_3_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_2_2_uint(uint2 v, out uint2 o)
{
    // ~8 alu (2 mul)
    v.y ^= 1103515245U;
    v.x += v.y;
    v.x *= v.y;
    v.x ^= v.x >> 5u;
    v.x *= 0x27d4eb2du;
    v.y ^= (v.x << 3u);
    o = v;
}

void Hash_Tchou_2_2_float(float2 i, out float2 o)
{
    uint2 r;
    uint2 v = (uint2) (int2) round(i);
    Hash_Tchou_2_2_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_2_2_half(half2 i, out half2 o)
{
    uint2 r;
    uint2 v = (uint2) (int2) round(i);
    Hash_Tchou_2_2_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_3_1_uint(uint3 v, out uint o)
{
    // ~15 alu (3 mul)
    v.x ^= 1103515245U;
    v.y ^= v.x + v.z;
    v.y = v.y * 134775813;
    v.z += v.x ^ v.y;
    v.y += v.x ^ v.z;
    v.x += v.y * v.z;
    v.x = v.x * 0x27d4eb2du;
    o = v.x;
}

void Hash_Tchou_3_1_float(float3 i, out float o)
{
    uint r;
    uint3 v = (uint3) (int3) round(i);
    Hash_Tchou_3_1_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_3_1_half(half3 i, out half o)
{
    uint r;
    uint3 v = (uint3) (int3) round(i);
    Hash_Tchou_3_1_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_3_3_uint(uint3 v, out uint3 o)
{
    // ~15 alu (3 mul)
    v.x ^= 1103515245U;
    v.y ^= v.x + v.z;
    v.y = v.y * 134775813;
    v.z += v.x ^ v.y;
    v.y += v.x ^ v.z;
    v.x += v.y * v.z;
    v.x = v.x * 0x27d4eb2du;
    v.z ^= v.x << 3;
    v.y += v.z << 3;
    o = v;
}

void Hash_Tchou_3_3_float(float3 i, out float3 o)
{
    uint3 r, v = (uint3) (int3) round(i);
    Hash_Tchou_3_3_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_3_3_half(half3 i, out half3 o)
{
    uint3 r, v = (uint3) (int3) round(i);
    Hash_Tchou_3_3_uint(v, r);
    o = r * (1.0 / float(0xffffffff));
}

void Hash_LegacySine_2_1_float(float2 i, out float o)
{
    float angle = dot(i, float2(12.9898, 78.233));
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angle)*43758.5453);
}

void Hash_LegacySine_2_1_half(half2 i, out half o)
{
    half angle = dot(i, half2(12.9898, 78.233));
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angle)*43758.5453);
}

void Hash_BetterSine_2_1_float(float2 i, out float o)
{
    float angle = dot(i, float2(12.9898, 78.233) / 1000.0f);
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angle)*43758.5453);
}

void Hash_BetterSine_2_1_half(half2 i, out half o)
{
    float angle = dot(i, half2(12.9898, 78.233) / 1000.0f);
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angle)*43758.5453);
}

void Hash_LegacySine_2_2_float(float2 i, out float2 o)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    float2 angles = mul(i, m);
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angles = fmod(angles, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angles));
}

void Hash_LegacySine_2_2_half(half2 i, out half2 o)
{
    half2x2 m = half2x2(15.27, 47.63, 99.41, 89.98);
    half2 angles = mul(i, m);
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angles = fmod(angles, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angles));
}

void Hash_LegacyMod_2_1_float(float2 i, out float o)
{
    // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
    i = i % 289;
    // need full precision, otherwise half overflows when p > 1
    float x = float(34 * i.x + 1) * i.x % 289 + i.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    o = x;
}

void Hash_LegacyMod_2_1_half(half2 i, out half o)
{
    // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
    i = i % 289;
    // need full precision, otherwise half overflows when p > 1
    float x = float(34 * i.x + 1) * i.x % 289 + i.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    o = x;
}
