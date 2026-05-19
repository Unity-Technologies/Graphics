
//VFX_DECLARE_BINDING(ParticleData, ParticleDataBinding);

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
        VFXProcessBlocks(particleAttributes);

        ParticleDataBinding.particleAttributeBuffer.StoreData(particleAttributes, particleIndex);

        if (particleAttributes.alive)
        {
        }
        else
        {
            ParticleDataBinding.DeleteParticle(particleIndex);
        }
    }
}
