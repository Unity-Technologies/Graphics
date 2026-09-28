
struct VertexInput
{
    uint instanceID : SV_InstanceID;
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float4 pos : SV_POSITION;
    float4 color : COLOR;
};

Varyings vert(ThreadData threadData, uint vertexId, VertexInput vs_input)
{
    Varyings o = (Varyings)0;

    uint particleIndex = threadData.index;

    if (particleIndex >= ContextData.maxParticleCount)
    {
        return o;
    }

    Attributes particleAttributes;
    particleAttributes.Init();
    ParticleDataBinding.particleAttributeBuffer.LoadData(particleAttributes, particleIndex);

    if (particleAttributes.alive)
    {
        VFXProcessBlocks(particleAttributes);

        if (particleAttributes.alive)
        {
            float4x4 elementToVFXMatrix = GetElementToVFXMatrix(
                particleAttributes.axisX,
                particleAttributes.axisY,
                particleAttributes.axisZ,
                float3(particleAttributes.angleX, particleAttributes.angleY, particleAttributes.angleZ),
                float3(particleAttributes.pivotX, particleAttributes.pivotY, particleAttributes.pivotZ),
                float3(particleAttributes.scaleX, particleAttributes.scaleY, particleAttributes.scaleZ) * particleAttributes.size,
                particleAttributes.position);

            float2 varyingUV;
            varyingUV.x = float(vertexId & 1);
            varyingUV.y = (vertexId & 2) * 0.5f;

            const float2 offsets = varyingUV.xy - 0.5f;
            float3 posVFX = mul(elementToVFXMatrix, float4(offsets, 0.0f, 1.0f)).xyz;

            o.pos = TransformPositionVFXToClip(posVFX);
            o.uv = varyingUV;
            o.color = float4(particleAttributes.color, particleAttributes.alpha);
        }
    }
    return o;
}

float4 frag(Varyings i) : SV_Target
{
    return SampleTexture(VFX_SAMPLER(mainTexture), i.uv) * i.color;
}
