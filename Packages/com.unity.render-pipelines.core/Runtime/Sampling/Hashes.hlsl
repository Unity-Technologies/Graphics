#ifndef _SAMPLING_HASHES_HLSL_
#define _SAMPLING_HASHES_HLSL_

// Low bias hash from https://github.com/skeeto/hash-prospector
uint LowBiasHash32(uint x, uint seed = 0)
{
    x += seed;
    x ^= x >> 16;
    x *= 0x21f0aaad;
    x ^= x >> 15;
    x *= 0xd35a2d97;
    x ^= x >> 15;
    return x;
}

// Murmur Hash from https://github.com/aappleby/smhasher/wiki/MurmurHash3
uint MurmurAdd(uint hash, uint item)
{
	item *= 0xcc9e2d51;
	item = (item << 15) | (item >> 17);
	item *= 0x1b873593;

	hash ^= item;
	hash = (hash << 13) | (hash >> 19);
	hash = hash * 5 + 0xe6546b64;
	return hash;
}

uint MurmurFinalize(uint hash)
{
	hash ^= hash >> 16;
	hash *= 0x85ebca6b;
	hash ^= hash >> 13;
	hash *= 0xc2b2ae35;
	hash ^= hash >> 16;
	return hash;
}

uint MurmurHash(uint x, uint seed = 0)
{
    uint h = seed;
    h = MurmurAdd(h, x);
    return MurmurFinalize(h);
}

uint MurmurHash(uint2 x, uint seed = 0)
{
    uint h = seed;
    h = MurmurAdd(h, x.x);
    h = MurmurAdd(h, x.y);
    return MurmurFinalize(h);
}

uint MurmurHash(uint3 x, uint seed = 0)
{
    uint h = seed;
    h = MurmurAdd(h, x.x);
    h = MurmurAdd(h, x.y);
    h = MurmurAdd(h, x.z);
    return MurmurFinalize(h);
}

uint MurmurHash(uint4 x, uint seed = 0)
{
    uint h = seed;
    h = MurmurAdd(h, x.x);
    h = MurmurAdd(h, x.y);
    h = MurmurAdd(h, x.z);
    h = MurmurAdd(h, x.w);
    return MurmurFinalize(h);
}

uint XorShift32(uint rngState)
{
    rngState ^= rngState << 13;
    rngState ^= rngState >> 17;
    rngState ^= rngState << 5;
    return rngState;
}

// From PCG: A Family of Simple Fast Space-Efficient Statistically Good Algorithms for Random Number Generation.
// and "Hash Functions for GPU Rendering" paper
uint Pcg(uint v)
{
	uint state = v * 747796405u + 2891336453u;
	uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
	return (word >> 22u) ^ word;
}

uint2 Pcg2d(uint2 v)
{
    v = v * 1664525u + 1013904223u;

    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;

    v = v ^ (v >> 16u);

    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;

    v = v ^ (v >> 16u);

    return v;
}

uint3 Pcg3d(uint3 v)
{
    v = v * 1664525u + 1013904223u;

    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;

    v ^= v >> 16u;

    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;

    return v;
}

uint4 Pcg4d(uint4 v)
{
    v = v * 1664525u + 1013904223u;

    v.x += v.y * v.w;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v.w += v.y * v.z;

    v = v ^ (v >> 16u);

    v.x += v.y * v.w;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v.w += v.y * v.z;

    return v;
}

uint PixelHash(uint2 pixelCoord, uint seed = 0)
{
    return LowBiasHash32((pixelCoord.x & 0xFFFF) | (pixelCoord.y << 16), seed);
}

#endif // _SAMPLING_HASHES_HLSL_
