
//VFX_DECLARE_BINDING(ParticleData, ParticleDataBinding);
//VFX_DECLARE_BINDING(RenderingData, RenderingDataBinding);

//VFX_MAP_ATTRIBUTES(Attributes, ParticleDataBinding.particleAttributeBuffer);

void main(ThreadData threadData)
{
    uint particleIndex = threadData.index;
    uint maxParticleCount = ContextData.maxParticleCount;

    if(particleIndex >= maxParticleCount)
    {
        return;
    }

    Attributes particleAttributes;
    ParticleDataBinding.particleAttributeBuffer.LoadData(particleAttributes, particleIndex);

    if (particleAttributes.alive)
    {
        VFXProcessSubtasks(particleAttributes);

        if (particleAttributes.alive)
        {
            // The sorting key is one of the task's own parameters - whatever the output's criterion, sign
            // included - so registering it is all that is left to do here.
            SortingKVP kvp;
            kvp.key = contextParameters.sortKey;
            kvp.index = particleIndex;
            RenderingDataBinding.RegisterSortingKVP(kvp);
        }
    }
}
