// Template for applying shared noise parameters to each noise type
#define NOISE_TEMPLATE(NAME, TYPE, FUNC) \
float Generate##NAME(TYPE coordinate, float amplitude, float frequency, int octaveCount, float persistence) \
{ \
    float total = 0.0f; \
\
    for (int octaveIndex = 0; octaveIndex < octaveCount; octaveIndex++) \
    { \
        total += FUNC(coordinate * frequency) * amplitude; \
        amplitude *= persistence; \
        frequency *= 2.0f; \
    } \
 \
    return total; \
}

// Value Noise
float ValueNoiseHash(float n)
{
    return frac(sin(n) * 1e4f);
}

float ValueNoiseHash(float2 p)
{
    float1 a = 1e4f * sin(17.0f * p.x + p.y * 0.1f);
    float1 b = (0.1f + abs(sin(p.y * 13.0f + p.x)));
    return frac(a * b);
}

float GenerateValueNoise1D(float coordinate)
{
    float i = floor(coordinate);
    float f = frac(coordinate);
    float u = f * f * (3.0f - 2.0f * f);
    return lerp(ValueNoiseHash(i), ValueNoiseHash(i + 1.0f), u);
}

float GenerateValueNoise2D(float2 coordinate)
{
    float2 i = floor(coordinate);
    float2 f = frac(coordinate);

    // Four corners in 2D of a tile
    float a = ValueNoiseHash(i);
    float b = ValueNoiseHash(i + float2(1.0f, 0.0f));
    float c = ValueNoiseHash(i + float2(0.0f, 1.0f));
    float d = ValueNoiseHash(i + float2(1.0f, 1.0f));

    // Simple 2D lerp using smoothstep envelope between the values.
    // return float3(lerp(lerp(a, b, smoothstep(0.0, 1.0, f.x)),
    //          lerp(c, d, smoothstep(0.0, 1.0, f.x)),
    //          smoothstep(0.0, 1.0, f.y)));

    // Same code, with the clamps in smoothstep and common subexpressions
    // optimized away.
    float2 u = f * f * (3.0f - 2.0f * f);
    return lerp(a, b, u.x) + (c - a) * u.y * (1.0f - u.x) + (d - b) * u.x * u.y;
}

float GenerateValueNoise3D(float3 coordinate)
{
    float3 step = float3(110, 241, 171);

    float3 i = floor(coordinate);
    float3 f = frac(coordinate);

    // For performance, compute the base input to a 1D ValueNoiseHash from the integer part of the argument and the
    // incremental change to the 1D based on the 3D -> 1D wrapping
    float n = dot(i, step);

    float3 u = f * f * (3.0f - 2.0f * f);
    return lerp(lerp(lerp(ValueNoiseHash(n + dot(step, float3(0, 0, 0))), ValueNoiseHash(n + dot(step, float3(1, 0, 0))), u.x),
        lerp(ValueNoiseHash(n + dot(step, float3(0, 1, 0))), ValueNoiseHash(n + dot(step, float3(1, 1, 0))), u.x), u.y),
        lerp(lerp(ValueNoiseHash(n + dot(step, float3(0, 0, 1))), ValueNoiseHash(n + dot(step, float3(1, 0, 1))), u.x),
            lerp(ValueNoiseHash(n + dot(step, float3(0, 1, 1))), ValueNoiseHash(n + dot(step, float3(1, 1, 1))), u.x), u.y), u.z);
}

NOISE_TEMPLATE(ValueNoise, float, GenerateValueNoise1D);
NOISE_TEMPLATE(ValueNoise, float2, GenerateValueNoise2D);
NOISE_TEMPLATE(ValueNoise, float3, GenerateValueNoise3D);

// Perlin Noise
float PerlinNoiseFade(float t) { return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f); }
float2 PerlinNoiseFade(float2 t) { return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f); }
float3 PerlinNoiseFade(float3 t) { return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f); }

float2 NoisePermute(float2 x) { return fmod(((x * 34.0f) + 1.0f) * x, 289.0f); }
float3 NoisePermute(float3 x) { return fmod(((x * 34.0f) + 1.0f) * x, 289.0f); }
float4 NoisePermute(float4 x) { return fmod(((x * 34.0f) + 1.0f) * x, 289.0f); }

float2 NoiseTaylorInvSqrt(float2 r) { return 1.79284291400159f - 0.85373472095314f * r; }
float3 NoiseTaylorInvSqrt(float3 r) { return 1.79284291400159f - 0.85373472095314f * r; }
float4 NoiseTaylorInvSqrt(float4 r) { return 1.79284291400159f - 0.85373472095314f * r; }

float GeneratePerlinNoise1D(const float coordinate)
{
    float2 Pi = floor(coordinate) + float2(0.0f, 1.0f);
    float2 Pf = frac(coordinate) - float2(0.0f, 1.0f);
    Pi = fmod(Pi, 289.0f);     // To avoid truncation effects in permutation
    float2 ix = Pi.xy;
    float2 fx = Pf.xy;
    float2 i = NoisePermute(ix);
    float2 gx = 2.0f * frac(i * 0.0243902439f) - 1.0f;     // 1/41 = 0.024...
    float2 gy = abs(gx) - 0.5f;
    float2 tx = floor(gx + 0.5f);
    gx = gx - tx;
    float2 g00 = float2(gx.x, gy.x);
    float2 g10 = float2(gx.y, gy.y);
    float2 norm = NoiseTaylorInvSqrt(float2(dot(g00, g00), dot(g10, g10)));
    g00 *= norm.x;
    g10 *= norm.y;
    float1 n00 = dot(g00, float2(fx.x, 0.0f));
    float1 n10 = dot(g10, float2(fx.y, 1.0f));
    float1 fade_x = PerlinNoiseFade(Pf.x);
    float1 n_x = lerp(n00, n10, fade_x);
    return (2.3f * n_x) * 0.5f + 0.5f;
}

float GeneratePerlinNoise2D(const float2 coordinate)
{
    float4 Pi = floor(coordinate.xyxy) + float4(0.0f, 0.0f, 1.0f, 1.0f);
    float4 Pf = frac(coordinate.xyxy) - float4(0.0f, 0.0f, 1.0f, 1.0f);
    Pi = fmod(Pi, 289.0f);     // To avoid truncation effects in permutation
    float4 ix = float4(Pi.xz, Pi.xz);
    float4 iy = Pi.yyww;
    float4 fx = float4(Pf.xz, Pf.xz);
    float4 fy = Pf.yyww;
    float4 i = NoisePermute(NoisePermute(ix) + iy);
    float4 gx = 2.0f * frac(i * 0.0243902439f) - 1.0f;     // 1/41 = 0.024...
    float4 gy = abs(gx) - 0.5f;
    float4 tx = floor(gx + 0.5f);
    gx = gx - tx;
    float2 g00 = float2(gx.x, gy.x);
    float2 g10 = float2(gx.y, gy.y);
    float2 g01 = float2(gx.z, gy.z);
    float2 g11 = float2(gx.w, gy.w);
    float4 norm = NoiseTaylorInvSqrt(float4(dot(g00, g00), dot(g01, g01), dot(g10, g10), dot(g11, g11)));
    g00 *= norm.x;
    g01 *= norm.y;
    g10 *= norm.z;
    g11 *= norm.w;
    float n00 = dot(g00, float2(fx.x, fy.x));
    float n10 = dot(g10, float2(fx.y, fy.y));
    float n01 = dot(g01, float2(fx.z, fy.z));
    float n11 = dot(g11, float2(fx.w, fy.w));
    float2 fade_xy = PerlinNoiseFade(Pf.xy);
    float2 n_x = lerp(float2(n00, n01), float2(n10, n11), fade_xy.x);
    float n_xy = lerp(n_x.x, n_x.y, fade_xy.y);
    return (2.3f * n_xy) * 0.5f + 0.5f;
}

float GeneratePerlinNoise3D(const float3 coordinate)
{
    float3 Pi0 = floor(coordinate);     // Integer part for indexing
    float3 Pi1 = Pi0 + 1.0f;     // Integer part + 1
    Pi0 = fmod(Pi0, 289.0f);
    Pi1 = fmod(Pi1, 289.0f);
    float3 Pf0 = frac(coordinate);     // Fractional part for interpolation
    float3 Pf1 = Pf0 - 1.0f;     // Fractional part - 1.0
    float4 ix = float4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
    float4 iy = float4(Pi0.y, Pi0.y, Pi1.y, Pi1.y);
    float4 iz0 = Pi0.z;
    float4 iz1 = Pi1.z;

    float4 ixy = NoisePermute(NoisePermute(ix) + iy);
    float4 ixy0 = NoisePermute(ixy + iz0);
    float4 ixy1 = NoisePermute(ixy + iz1);

    float4 gx0 = ixy0 / 7.0f;
    float4 gy0 = frac(floor(gx0) / 7.0f) - 0.5f;
    gx0 = frac(gx0);
    float4 gz0 = 0.5f - abs(gx0) - abs(gy0);
    float4 sz0 = step(gz0, 0.0f);
    gx0 -= sz0 * (step(0.0f, gx0) - 0.5f);
    gy0 -= sz0 * (step(0.0f, gy0) - 0.5f);

    float4 gx1 = ixy1 / 7.0f;
    float4 gy1 = frac(floor(gx1) / 7.0f) - 0.5f;
    gx1 = frac(gx1);
    float4 gz1 = 0.5f - abs(gx1) - abs(gy1);
    float4 sz1 = step(gz1, 0.0f);
    gx1 -= sz1 * (step(0.0f, gx1) - 0.5f);
    gy1 -= sz1 * (step(0.0f, gy1) - 0.5f);

    float3 g000 = float3(gx0.x, gy0.x, gz0.x);
    float3 g100 = float3(gx0.y, gy0.y, gz0.y);
    float3 g010 = float3(gx0.z, gy0.z, gz0.z);
    float3 g110 = float3(gx0.w, gy0.w, gz0.w);
    float3 g001 = float3(gx1.x, gy1.x, gz1.x);
    float3 g101 = float3(gx1.y, gy1.y, gz1.y);
    float3 g011 = float3(gx1.z, gy1.z, gz1.z);
    float3 g111 = float3(gx1.w, gy1.w, gz1.w);

    float4 norm0 = NoiseTaylorInvSqrt(float4(dot(g000, g000), dot(g010, g010), dot(g100, g100), dot(g110, g110)));
    g000 *= norm0.x;
    g010 *= norm0.y;
    g100 *= norm0.z;
    g110 *= norm0.w;
    float4 norm1 = NoiseTaylorInvSqrt(float4(dot(g001, g001), dot(g011, g011), dot(g101, g101), dot(g111, g111)));
    g001 *= norm1.x;
    g011 *= norm1.y;
    g101 *= norm1.z;
    g111 *= norm1.w;

    float n000 = dot(g000, Pf0);
    float n100 = dot(g100, float3(Pf1.x, Pf0.y, Pf0.z));
    float n010 = dot(g010, float3(Pf0.x, Pf1.y, Pf0.z));
    float n110 = dot(g110, float3(Pf1.x, Pf1.y, Pf0.z));
    float n001 = dot(g001, float3(Pf0.x, Pf0.y, Pf1.z));
    float n101 = dot(g101, float3(Pf1.x, Pf0.y, Pf1.z));
    float n011 = dot(g011, float3(Pf0.x, Pf1.y, Pf1.z));
    float n111 = dot(g111, Pf1);

    float3 fade_xyz = PerlinNoiseFade(Pf0);
    float4 n_z = lerp(float4(n000, n100, n010, n110), float4(n001, n101, n011, n111), fade_xyz.z);
    float2 n_yz = lerp(n_z.xy, n_z.zw, fade_xyz.y);
    float n_xyz = lerp(n_yz.x, n_yz.y, fade_xyz.x);
    return (2.2f * n_xyz) * 0.5f + 0.5f;
}

NOISE_TEMPLATE(PerlinNoise, float, GeneratePerlinNoise1D);
NOISE_TEMPLATE(PerlinNoise, float2, GeneratePerlinNoise2D);
NOISE_TEMPLATE(PerlinNoise, float3, GeneratePerlinNoise3D);

// Simplex Noise
float GenerateSimplexNoise1D(float coordinate)
{
    const float4 C = float4(0.211324865405187f, 0.366025403784439f, -0.577350269189626f, 0.024390243902439f);
    float i = floor(coordinate + (coordinate * C.y));
    float x0 = coordinate - i + (i * C.x);
    float2 x12 = x0.x + C.xz;
    x12.x -= 1.0f;
    i = fmod(i, 289.0f);
    float3 p = NoisePermute(i + float3(0.0f, 1.0f, 1.0f));
    float3 m = max(0.5f - float3(x0 * x0, x12.x * x12.x, x12.y * x12.y), 0.0f);
    m = m * m;
    m = m * m;
    float3 x = 2.0f * frac(p * C.w) - 1.0f;
    float3 h = abs(x) - 0.5f;
    float3 ox = floor(x + 0.5f);
    float3 a0 = x - ox;
    m *= NoiseTaylorInvSqrt(a0 * a0 + h * h);
    float3 g;
    g.x = a0.x * x0.x;
    g.yz = a0.yz * x12.xy;
    return (130.0f * dot(m, g)) * 0.5f + 0.5f;
}

float GenerateSimplexNoise2D(float2 coordinate)
{
    const float4 C = float4(0.211324865405187f, 0.366025403784439f, -0.577350269189626f, 0.024390243902439f);
    float2 i = floor(coordinate + dot(coordinate, C.y));
    float2 x0 = coordinate - i + dot(i, C.x);
    float2 i1;
    i1 = (x0.x > x0.y) ? float2(1.0f, 0.0f) : float2(0.0f, 1.0f);
    float4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;
    i = fmod(i, 289.0f);
    float3 p = NoisePermute(NoisePermute(i.y + float3(0.0f, i1.y, 1.0f)) + i.x + float3(0.0f, i1.x, 1.0f));
    float3 m = max(0.5f - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0f);
    m = m * m;
    m = m * m;
    float3 x = 2.0f * frac(p * C.w) - 1.0f;
    float3 h = abs(x) - 0.5f;
    float3 ox = floor(x + 0.5f);
    float3 a0 = x - ox;
    m *= NoiseTaylorInvSqrt(a0 * a0 + h * h);
    float3 g;
    g.x = a0.x  * x0.x + h.x  * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return (130.0f * dot(m, g)) * 0.5f + 0.5f;
}

float GenerateSimplexNoise3D(float3 coordinate)
{
    const float2 C = float2(1.0f / 6.0f, 1.0f / 3.0f);
    const float4 D = float4(0.0f, 0.5f, 1.0f, 2.0f);

    // First corner
    float3 i = floor(coordinate + dot(coordinate, C.y));
    float3 x0 = coordinate - i + dot(i, C.x);

    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0f - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);

    //  x0 = x0 - 0. + 0.0 * C
    float3 x1 = x0 - i1 + 1.0f * C.x;
    float3 x2 = x0 - i2 + 2.0f * C.x;
    float3 x3 = x0 - 1. + 3.0f * C.x;

    // Permutations
    i = fmod(i, 289.0f);
    float4 p = NoisePermute(NoisePermute(NoisePermute(
        i.z + float4(0.0f, i1.z, i2.z, 1.0f))
        + i.y + float4(0.0f, i1.y, i2.y, 1.0f))
        + i.x + float4(0.0f, i1.x, i2.x, 1.0f));

    // Gradients
    // ( N*N points uniformly over a square, mapped onto an octahedron.)
    float n_ = 1.0f / 7.0f;     // N=7
    float3  ns = n_ * float3(D.w, D.y, D.z) - float3(D.x, D.z, D.x);

    float4 j = p - 49.0f * floor(p * ns.z * ns.z);     //  mod(p,N*N)

    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0f * x_);        // mod(j,N)

    float4 x = x_ * ns.x + ns.y;
    float4 y = y_ * ns.x + ns.y;
    float4 h = 1.0f - abs(x) - abs(y);

    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);

    float4 s0 = floor(b0) * 2.0f + 1.0f;
    float4 s1 = floor(b1) * 2.0f + 1.0f;
    float4 sh = -step(h, 0.0f);

    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    float3 p0 = float3(a0.xy, h.x);
    float3 p1 = float3(a0.zw, h.y);
    float3 p2 = float3(a1.xy, h.z);
    float3 p3 = float3(a1.zw, h.w);

    //Normalise gradients
    float4 norm = NoiseTaylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;

    // Mix final noise value
    float4 m = max(0.6f - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0f);
    m = m * m;
    return (42.0f * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)))) * 0.5f + 0.5f;
}

NOISE_TEMPLATE(SimplexNoise, float, GenerateSimplexNoise1D);
NOISE_TEMPLATE(SimplexNoise, float2, GenerateSimplexNoise2D);
NOISE_TEMPLATE(SimplexNoise, float3, GenerateSimplexNoise3D);

// VoroNoise
float3 VoroHash3(float2 p)
{
    float3 q = float3(dot(p, float2(127.1f, 311.7f)), dot(p, float2(269.5f, 183.3f)), dot(p, float2(419.2f, 371.9f)));
    return frac(sin(q) * 43758.5453f);
}

float GenerateVoroNoise(float2 coordinate, float amplitude, float frequency, float warp, float smoothness)
{
    coordinate *= frequency;

    float2 p = floor(coordinate);
    float2 f = frac(coordinate);

    float k = 1.0f + 63.0f * pow(1.0f - smoothness, 4.0f);

    float va = 0.0f;
    float wt = 0.0f;
    for (int j = -2; j <= 2; j++)
    {
        for (int i = -2; i <= 2; i++)
        {
            float2 g = float2(float(i), float(j));
            float3 o = VoroHash3(p + g) * float3(warp.xx, 1.0f);
            float2 r = g - f + o.xy;
            float d = dot(r, r);
            float ww = pow(1.0f - smoothstep(0.0f, 1.414f, sqrt(d)), k);
            va += o.z * ww;
            wt += ww;
        }
    }

    return ((va / wt) * 2 - 1) * amplitude;
}
