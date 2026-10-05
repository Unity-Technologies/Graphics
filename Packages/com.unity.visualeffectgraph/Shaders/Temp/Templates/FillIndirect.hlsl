
//VFX_DECLARE_BINDING(ParticleData, ParticleDataBinding);
//VFX_DECLARE_BINDING(RenderingData, RenderingDataBinding);

//VFX_MAP_ATTRIBUTES(Attributes, ParticleDataBinding.particleAttributeBuffer);

void main(ThreadData threadData)
{
    uint particleIndex = threadData.index;
    uint maxParticleCount = ContextData.maxParticleCount;

    if (particleIndex >= maxParticleCount)
    {
        return;
    }

    Attributes particleAttributes;
    ParticleDataBinding.particleAttributeBuffer.LoadData(particleAttributes, particleIndex);

    if (particleAttributes.alive)
    {
        RenderingDataBinding.AppendRenderingIndex(particleIndex);
    }
}
