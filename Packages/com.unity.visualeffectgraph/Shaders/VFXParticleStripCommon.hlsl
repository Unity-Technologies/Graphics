
#define STRIP_FIRST_INDEX 0
#define STRIP_NEXT_INDEX 1
#define STRIP_PREV_NEXT_INDEX 2
#define STRIP_MIN_ALIVE 3
#define STRIP_MAX_ALIVE 4

#if VFX_USE_INSTANCING
#define STRIP_DATA_OFFSET instancingBatchSize
#else
#define STRIP_DATA_OFFSET 1
#endif
#define STRIP_DATA_INDEX(instanceIndex, stripIndex) ((instanceIndex * STRIP_COUNT) + stripIndex)
#define STRIP_DATA_X(buffer,data,bufferIndex) buffer[STRIP_DATA_OFFSET + (bufferIndex * 5) + (data)]
#define STRIP_DATA(data,bufferIndex) STRIP_DATA_X(stripDataBuffer,data,bufferIndex)

#define STRIP_PARTICLE_COUNTER(instanceIndex) stripDataBuffer[instanceIndex]

#define STRIP_PARTICLE_IN_EDGE (id & 1)

struct StripData
{
    uint stripIndex;
    uint capacity;
    uint firstIndex;
    uint nextIndex;
    uint prevNextIndex;
};

#if HAS_STRIPS_DATA
const StripData GetStripDataFromStripIndex(uint stripIndex, uint instanceIndex)
{
    StripData stripData = (StripData)0;
    stripData.stripIndex = stripIndex;
    stripData.capacity = PARTICLE_PER_STRIP_COUNT;

    uint bufferIndex = STRIP_DATA_INDEX(instanceIndex, stripIndex);
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

bool FindIndexInStrip(inout uint index, uint id, uint instanceIndex, out uint relativeIndexInStrip, out StripData stripData)
{
    uint particlePerStripCount = PARTICLE_PER_STRIP_COUNT;

#if HAS_STRIPS && !VFX_HAS_INDIRECT_DRAW
    // skipping last particle per strip as an optimization
    particlePerStripCount--;
#endif

    uint stripIndex = index / particlePerStripCount;
    stripData = GetStripDataFromStripIndex(stripIndex, instanceIndex);

#if VFX_HAS_INDIRECT_DRAW
    // In indirect mode, index is global particle index
    uint relativeParticleIndex = GetRelativeIndex(index, stripData);
#else
    // By default, index is the relative particle index
    uint relativeParticleIndex = index - stripIndex * particlePerStripCount;
#endif

    uint maxEdgeIndex = relativeIndexInStrip = relativeParticleIndex; // vertex index in the strip
#if HAS_STRIPS
    // For strip outputs, particle index belongs to one segment or another depending of the edge
    relativeIndexInStrip += STRIP_PARTICLE_IN_EDGE;

    // For strip outputs, we render one particle less
    maxEdgeIndex += 1;
#endif

    index = GetParticleIndex(relativeIndexInStrip, stripData);

    return maxEdgeIndex < stripData.nextIndex;
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
