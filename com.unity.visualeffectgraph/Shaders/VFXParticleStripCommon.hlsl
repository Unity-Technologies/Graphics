#define STRIP_FIRST_INDEX 0
#define STRIP_NEXT_INDEX 1
#define STRIP_PREV_NEXT_INDEX 2
#define STRIP_MIN_ALIVE 3
#define STRIP_MAX_ALIVE 4
#define STRIP_DATA_X(buffer,data,stripIndex) buffer[(stripIndex * 5) + (data)]
#define STRIP_DATA(data,stripIndex) STRIP_DATA_X(stripDataBuffer,data,stripIndex)

struct StripData
{
    uint stripIndex;
    uint capacity;
    uint firstIndex;
    uint nextIndex;
    uint prevNextIndex;
};

#if HAS_STRIPS
const StripData GetStripDataFromStripIndex(uint stripIndex, uint instanceIndex)
{
    StripData stripData = (StripData)0;
    stripData.stripIndex = stripIndex;
    stripData.capacity = PARTICLE_PER_STRIP_COUNT;

    uint bufferIndex = (instanceIndex * STRIP_COUNT) + stripIndex;
    stripData.firstIndex = STRIP_DATA(STRIP_FIRST_INDEX, bufferIndex);
    stripData.nextIndex = STRIP_DATA(STRIP_NEXT_INDEX, bufferIndex);
    stripData.prevNextIndex = STRIP_DATA(STRIP_PREV_NEXT_INDEX, bufferIndex);
    return stripData;
}

const StripData GetStripDataFromParticleIndex(uint particleIndex, uint instanceIndex)
{
    uint stripIndex = particleIndex / PARTICLE_PER_STRIP_COUNT;
    return GetStripDataFromStripIndex(stripIndex, instanceIndex);
}

uint GetParticleIndex(uint relativeIndex, const StripData data)
{
    return data.stripIndex * data.capacity + (relativeIndex + data.firstIndex) % data.capacity;
}

uint GetRelativeIndex(uint particleIndex, const StripData data)
{
    return (data.capacity + particleIndex - data.firstIndex) % data.capacity;
}

#if HAS_VFX_ATTRIBUTES
void InitStripAttributes(uint particleIndex, inout VFXAttributes attributes, const StripData data)
{
#if VFX_USE_STRIPINDEX_CURRENT
    attributes.stripIndex = data.stripIndex;
#endif
#if VFX_USE_PARTICLEINDEXINSTRIP_CURRENT
    attributes.particleIndexInStrip = GetRelativeIndex(particleIndex, data);
#endif
#if VFX_USE_PARTICLECOUNTINSTRIP_CURRENT
    attributes.particleCountInStrip = data.nextIndex;
#endif
}

void InitStripAttributesWithSpawn(uint spawnCount, uint particleIndex, inout VFXAttributes attributes, const StripData data)
{
    InitStripAttributes(particleIndex, attributes, data);
#if VFX_USE_PARTICLECOUNTINSTRIP_CURRENT
    attributes.particleCountInStrip = data.prevNextIndex + spawnCount; // Override particle count in init as nextIndex is not constant accross threads
#endif
#if VFX_USE_SPAWNINDEXINSTRIP_CURRENT
    attributes.spawnIndexInStrip = GetRelativeIndex(particleIndex, data) - data.prevNextIndex;
#endif
}
#endif
#endif
