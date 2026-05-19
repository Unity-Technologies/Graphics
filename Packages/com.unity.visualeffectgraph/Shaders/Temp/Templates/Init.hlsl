
//VFX_DECLARE_BINDING(ParticleData, ParticleDataBinding);
//VFX_DECLARE_BINDING(EventListData, EventListDataBinding);

//VFX_MAP_ATTRIBUTES(Attributes, ParticleDataBinding.particleAttributeBuffer);
//VFX_MAP_ATTRIBUTES(SourceAttributes, EventListDataBinding.attributes);

void main(ThreadData threadData)
{
	uint eventIndex = threadData.index;
    uint eventCount = EventListDataBinding.eventListData.eventCount;

    if(eventIndex >= eventCount)
    {
        return;
    }

    uint systemSeed = ContextData.systemSeed;
    uint baseSpawnIndex = ContextData.initSpawnIndex;

    Attributes particleAttributes;
    particleAttributes.Init();
    particleAttributes.particleId = EventListDataBinding.eventListData.baseEventCount + eventIndex;
    particleAttributes.seed = WangHash(particleAttributes.particleId ^ systemSeed);
    particleAttributes.spawnIndex = eventIndex;

    SourceAttributes sourceAttributes;
	sourceAttributes.Init();
    sourceAttributes.spawnCount = eventCount;
	uint sourceIndex = EventListDataBinding.eventListData.GetAttributesIndex(eventIndex);
	SourceAttributeDataBinding.LoadData(sourceAttributes, sourceIndex);

    VFXProcessBlocks(particleAttributes, sourceAttributes);

    if (particleAttributes.alive)
    {
        uint particleIndex;
        if (ParticleDataBinding.NewParticle(eventIndex, particleIndex))
        {
            ParticleDataBinding.particleAttributeBuffer.StoreData(particleAttributes, particleIndex);
        }
    }
}
