
//VFX_DECLARE_BINDING(ParticleData, ParticleDataBinding);
//VFX_DECLARE_BINDING(EventListData, EventListDataBinding);

//VFX_MAP_ATTRIBUTES(Attributes, ParticleDataBinding.particleAttributeBuffer);
//VFX_MAP_ATTRIBUTES(SourceAttributes, EventListDataBinding.attributes);

void main(ThreadData threadData)
{
	uint spawnIndex = threadData.index;
    uint eventCount = EventListDataBinding.eventListData.eventCount;
    uint spawnCount = EventListDataBinding.eventListData.spawnCount;

    if(spawnIndex >= spawnCount)
    {
        return;
    }

    uint systemSeed = ContextData.systemSeed;
    uint baseSpawnIndex = ContextData.initSpawnIndex;

    Attributes particleAttributes;
    particleAttributes.Init();
    particleAttributes.particleId = EventListDataBinding.baseEventCount + spawnIndex;
    particleAttributes.seed = WangHash(particleAttributes.particleId ^ systemSeed);
    particleAttributes.spawnIndex = spawnIndex;

    SourceAttributes sourceAttributes;
	sourceAttributes.Init();

	uint sourceIndex = EventListDataBinding.eventListData.GetAttributesIndex(spawnIndex);
	SourceAttributeDataBinding.LoadData(sourceAttributes, sourceIndex);

    VFXProcessSubtasks(particleAttributes, sourceAttributes);

    if (particleAttributes.alive)
    {
        uint particleIndex;
        if (ParticleDataBinding.NewParticle(spawnIndex, particleIndex))
        {
            ParticleDataBinding.particleAttributeBuffer.StoreData(particleAttributes, particleIndex);
        }
    }
}
