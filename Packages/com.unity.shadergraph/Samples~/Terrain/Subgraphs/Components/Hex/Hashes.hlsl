void Hash_Tchou_3_3_uint(uint3 v, out uint3 o)
{
	// 16 alu (3 mul)
	v.y ^= v.x + v.z;        // 2			issue:   v.x + v.z == v.y --- result is always zero in this plane
	v.y = v.y * 134775813;   // 1 (1 mul)
	v.z += v.x ^ v.y;        // 2
	v.y += v.x ^ v.z;        // 2
	v.x += v.y * v.z;        // 2 (1 mul)
	v.x = v.x * 0x27d4eb2du; // 1 (1 mul)
	v.x ^= v.x << 16;        // 2
	v.z ^= v.x << 3;         // 2
	v.y += v.z << 3;         // 2
	o = v;
}

void Hash_Tchou_3_3_float(float3 i, out float3 o)
{
	uint3 r, v = (uint3) (int3) round(i);
	Hash_Tchou_3_3_uint(v, r);
	o = r * (1.0 / float(0xffffffff));
}

void Hash_Tchou_2_1_uint(uint2 v, out uint o)
{
    v.y ^= 1103515245U;     // 1
    v.x *= v.y;             // 1 mul
    v.x ^= v.x >> 3u;       // 2
    v.x *= 0x27d4eb2du;     // 1 mul
    v.x ^= v.x >> 15u;      // 2
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
	// 13 ALU (3 mul)
	uint3 v = uint3(q, q.x ^ q.y);	// TODO can we do without this?

	v.x *= v.y * v.z;       // 2 (1 mul)
	v.x *= 0x27d4eb2du;     // 1 (1 mul)   
	v.x ^= v.x >> 4u;       // 2
	v.y += v.z ^ v.x;       // 2
	v.y ^= v.y >> 15u;      // 2
	v.y *= 0x27d4eb2du;     // 1 (1 mul)
	v.z += v.x ^ v.y;       // 2
	v.x += v.z;             // 1

	o = v;
}

void Hash_Tchou_2_3_float(float2 i, out float3 o)
{
	uint3 r;
	uint2 v = (uint2) (int2) round(i);
	Hash_Tchou_2_3_uint(v, r);
	o = r * (1.0 / float(0xffffffff));
}

void Hash_PCG_3_3_uint(uint3 v, out uint3 o)
{
	// 16 alu (7 mul)
	v = v * 1664525u + 1013904223u;		// LCG (numerical recipes)
	v.x += v.y*v.z;
	v.y += v.z*v.x;
	v.z += v.x*v.y;
	v ^= v >> 16u;
	v.x += v.y*v.z;
	v.y += v.z*v.x;
	v.z += v.x*v.y;
	o = v;
}

void Hash_PCG_2_3_uint(uint2 v, out uint3 o)
{
	Hash_PCG_3_3_uint(uint3(v, v.x ^ v.y), o);
}

void Hash_PCG_3_3_float(float3 i, out float3 o)
{
	uint3 r, v = (uint3) (int3) round(i);
	Hash_PCG_3_3_uint(v, r);
	o = r * (1.0 / float(0xffffffff));
}

void Hash_PCG_2_3_float(float2 i, out float3 o)
{
	uint2 v = (uint2) (int2) round(i);
	uint3 r;
	Hash_PCG_2_3_uint(v, r);
	o = r * (1.0 / float(0xffffffff));
}

void Hash_LegacySine_2_1_float(float2 i, out float o)
{
    float angle = dot(i, float2(12.9898, 78.233));
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angle)*43758.5453);
}

void Hash_LegacySine_2_1_half(half2 i, out half o)
{
    half angle = dot(i, half2(12.9898, 78.233));
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angle)*43758.5453);
}

void Hash_BetterSine_2_1_float(float2 i, out float o)
{
    float angle = dot(i, float2(12.9898, 78.233) / 1000.0f);
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angle)*43758.5453);
}

void Hash_BetterSine_2_1_half(half2 i, out half o)
{
    float angle = dot(i, half2(12.9898, 78.233) / 1000.0f);
#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
    // 'sin()' has bad precision on Mali GPUs for inputs > 10000
    angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
#endif
    o = frac(sin(angle)*43758.5453);
}

void Hash_LegacySine_2_2_float(float2 i, out float2 o)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    o = frac(sin(mul(i, m)));
}

void Hash_LegacySine_2_2_half(half2 i, out half2 o)
{
    half2x2 m = half2x2(15.27, 47.63, 99.41, 89.98);
    o = frac(sin(mul(i, m)));
}
