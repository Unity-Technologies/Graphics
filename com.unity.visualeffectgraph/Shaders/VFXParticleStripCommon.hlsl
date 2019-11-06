#define STRIP_FIRST_INDEX 0
#define STRIP_NEXT_INDEX 1
#define STRIP_MIN_ALIVE 2
#define STRIP_MAX_ALIVE 3
#define STRIP_DATA_X(buffer,data,stripIndex) buffer[(stripIndex << 2) + (data)]
#define STRIP_DATA(data,stripIndex) STRIP_DATA_X(stripDataBuffer,data,stripIndex)

struct StripData
{
	uint stripIndex;
	uint particleCountInStrip;
	uint firstIndex;
	uint nextIndex;
};

#if HAS_STRIPS
const StripData GetStripDataFromStripIndex(uint stripIndex, uint particleCountInStrip)
{
	StripData stripData = (StripData)0;
	stripData.stripIndex = stripIndex;
    stripData.particleCountInStrip = particleCountInStrip;
	stripData.firstIndex = STRIP_DATA(STRIP_FIRST_INDEX,stripData.stripIndex);
	stripData.nextIndex = STRIP_DATA(STRIP_NEXT_INDEX,stripData.stripIndex);
	return stripData;
}

const StripData GetStripDataFromParticleIndex(uint particleIndex, uint particleCountInStrip)
{
	uint stripIndex = particleIndex / particleCountInStrip;
	return GetStripDataFromStripIndex(stripIndex, particleCountInStrip);
}

uint GetParticleIndex(uint relativeIndex, const StripData data)
{
	return data.stripIndex * data.particleCountInStrip + (relativeIndex + data.firstIndex) % data.particleCountInStrip;
}

uint GetRelativeIndex(uint particleIndex, const StripData data)
{
	return (data.particleCountInStrip + particleIndex - data.firstIndex) % data.particleCountInStrip;	
}

#if HAS_ATTRIBUTES
void InitStripAttributes(uint particleIndex, inout Attributes attributes, const StripData data)
{
#if VFX_USE_STRIPINDEX_CURRENT
    attributes.stripIndex = data.stripIndex;
#endif
#if VFX_USE_PARTICLEINDEXINSTRIP_CURRENT
    attributes.particleIndexInStrip = GetRelativeIndex(particleIndex, data);
#endif
}
#endif
#endif
