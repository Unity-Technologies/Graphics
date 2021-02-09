#if defined(HAS_STRIPS) && !defined(VFX_PRIMITIVE_QUAD)
#error VFX_PRIMITIVE_QUAD must be defined when HAS_STRIPS is.
#endif

#define VFX_NON_UNIFORM_SCALE VFX_LOCAL_SPACE

#if HAS_STRIPS
#define PARTICLE_IN_EDGE (id & 1)
float3 GetParticlePosition(uint index)
{
    struct Attributes attributes = (Attributes)0;

    // Here we have to explicitly splice in the position (ShaderGraph splice system lacks regex support etc. :(, unlike VFX's).
    $splice(VFXLoadPositionAttribute)

    return attributes.position;
}

float3 GetStripTangent(float3 currentPos, uint relativeIndex, const StripData stripData)
{
    float3 prevTangent = (float3)0.0f;
    if (relativeIndex > 0)
    {
        uint prevIndex = GetParticleIndex(relativeIndex - 1,stripData);
        prevTangent = normalize(currentPos - GetParticlePosition(prevIndex));
    }

    float3 nextTangent = (float3)0.0f;
    if (relativeIndex < stripData.nextIndex - 1)
    {
        uint nextIndex = GetParticleIndex(relativeIndex + 1,stripData);
        nextTangent = normalize(GetParticlePosition(nextIndex) - currentPos);
    }

    return normalize(prevTangent + nextTangent);
}
#endif

void ApplyVFXModification(AttributesMesh input, inout VaryingsMeshType output)
{
    Attributes attributes = (Attributes)0;

    uint id = input.vertexID;

    // Index Setup
    #if VFX_PRIMITIVE_TRIANGLE
        uint index = id / 3;
    #elif VFX_PRIMITIVE_QUAD
    #if HAS_STRIPS
        id += VFX_GET_INSTANCE_ID(i) * 8192;
        const uint vertexPerStripCount = (PARTICLE_PER_STRIP_COUNT - 1) << 2;
        const StripData stripData = GetStripDataFromStripIndex(id / vertexPerStripCount, PARTICLE_PER_STRIP_COUNT);
        uint relativeIndexInStrip = ((id % vertexPerStripCount) >> 2) + (id & 1); // relative index of particle

        uint maxEdgeIndex = relativeIndexInStrip - PARTICLE_IN_EDGE + 1;

        if (maxEdgeIndex >= stripData.nextIndex)
            return;

        uint index = GetParticleIndex(relativeIndexInStrip, stripData);
    #else
        uint index = (id >> 2) + VFX_GET_INSTANCE_ID(i) * 2048;
    #endif
    #elif VFX_PRIMITIVE_OCTAGON
        uint index = (id >> 3) + VFX_GET_INSTANCE_ID(i) * 1024;
    #endif

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[index];
    #endif

    // Load Attributes
    $splice(VFXLoadAttribute)

    // Initialize built-in needed attributes
    #if HAS_STRIPS
    InitStripAttributes(index, attributes, stripData);
    #endif

    // Process Blocks
    $splice(VFXProcessBlocks)

#if !HAS_STRIPS
    if (!attributes.alive)
        return o;
#endif

    // Generate Vertex Offset
//    #if VFX_PRIMITIVE_QUAD
//        #if HAS_STRIPS
//        #if VFX_STRIPS_UV_STRECHED
//            o.VFX_VARYING_UV.x = (float)(relativeIndexInStrip) / (stripData.nextIndex - 1);
//        #elif VFX_STRIPS_UV_PER_SEGMENT
//            o.VFX_VARYING_UV.x = PARTICLE_IN_EDGE;
//        #else
//            ${VFXLoadParameter:{texCoord}}
//            o.VFX_VARYING_UV.x = texCoord;
//        #endif
//
//            o.VFX_VARYING_UV.y = float((id & 2) >> 1);
//            const float2 vOffsets = float2(0.0f,o.VFX_VARYING_UV.y - 0.5f);
//
//        #if VFX_STRIPS_SWAP_UV
//            o.VFX_VARYING_UV.xy = float2(1.0f - o.VFX_VARYING_UV.y, o.VFX_VARYING_UV.x);
//        #endif
//
//        #else
//            o.VFX_VARYING_UV.x = float(id & 1);
//            o.VFX_VARYING_UV.y = float((id & 2) >> 1);
//            const float2 vOffsets = o.VFX_VARYING_UV.xy - 0.5f;
//        #endif
//    #elif VFX_PRIMITIVE_TRIANGLE
//        const float2 kOffsets[] = {
//            float2(-0.5f,     -0.288675129413604736328125f),
//            float2(0.0f,  0.57735025882720947265625f),
//            float2(0.5f,  -0.288675129413604736328125f),
//        };
//
//        const float kUVScale = 0.866025388240814208984375f;
//
//        const float2 vOffsets = kOffsets[id % 3];
//        // o.VFX_VARYING_UV.xy = (vOffsets * kUVScale) + 0.5f;
//    #elif VFX_PRIMITIVE_OCTAGON
//        const float2 kUvs[8] =
//        {
//            float2(-0.5f, 0.0f),
//            float2(-0.5f, 0.5f),
//            float2(0.0f,  0.5f),
//            float2(0.5f,  0.5f),
//            float2(0.5f,  0.0f),
//            float2(0.5f,  -0.5f),
//            float2(0.0f,  -0.5f),
//            float2(-0.5f, -0.5f),
//        };
//
//        ${VFXLoadParameter:{cropFactor}} // TODO
//        cropFactor = id & 1 ? 1.0f - cropFactor : 1.0f;
//        const float2 vOffsets = kUvs[id & 7] * cropFactor;
//        // o.VFX_VARYING_UV.xy = vOffsets + 0.5f;
//    #endif

    // TEMP: Implement the above
    const float2 vOffsets = float2(0.0f, float((id & 2) >> 1) - 0.5f);

    // Instance to Particle
    float3 size3 = float3(attributes.size,attributes.size,attributes.size);
    #if VFX_USE_SCALEX_CURRENT
    size3.x *= attributes.scaleX;
    #endif
    #if VFX_USE_SCALEY_CURRENT
    size3.y *= attributes.scaleY;
    #endif
    #if VFX_USE_SCALEZ_CURRENT
    size3.z *= attributes.scaleZ;
    #endif
#if HAS_STRIPS
    size3 += size3 < 0.0f ? -VFX_EPSILON : VFX_EPSILON; // Add an epsilon so that size is never 0 for strips
#endif

    const float4x4 elementToVFX = GetElementToVFXMatrix(
        attributes.axisX,
        attributes.axisY,
        attributes.axisZ,
        float3(attributes.angleX,attributes.angleY,attributes.angleZ),
        float3(attributes.pivotX,attributes.pivotY,attributes.pivotZ),
        size3,
        attributes.position);

    float3 inputVertexPosition = float3(vOffsets, 0.0f);
    float3 vPos = mul(elementToVFX,float4(inputVertexPosition,1.0f)).xyz;
    float4 csPos = TransformPositionVFXToClip(vPos);

    output.positionCS = csPos;

    #ifdef VARYINGS_NEED_POSITION_WS
    // Need to overwrite the position with the result from VFX.
    // Warning: Need to be explicit about relative space.
    output.positionRWS = TransformPositionVFXToWorld(vPos);
    #endif

    // Interpolants Generation
    $splice(VFXInterpolantsGeneration)
}
