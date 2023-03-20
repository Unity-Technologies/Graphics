// Output Type: Planar Primitive (Triangle, Quad, Octagon)

#if defined(HAS_STRIPS) && !defined(VFX_PRIMITIVE_QUAD)
#error VFX_PRIMITIVE_QUAD must be defined when HAS_STRIPS is.
#endif

#define VFX_NON_UNIFORM_SCALE VFX_LOCAL_SPACE

#define HAVE_VFX_PLANAR_PRIMITIVE

bool GetMeshAndElementIndex(inout VFX_SRP_ATTRIBUTES input, inout AttributesElement element)
{
    uint id = input.vertexID;

    // Index Setup
    uint index = 0;
    #if VFX_PRIMITIVE_TRIANGLE
        index = id / 3;
    #elif VFX_PRIMITIVE_QUAD
    #if HAS_STRIPS
        id += VFX_GET_INSTANCE_ID(i) * 8192;
        const uint vertexPerStripCount = (PARTICLE_PER_STRIP_COUNT - 1) << 2;

        index = id / vertexPerStripCount; // stripIndex. Needed by VFXInitInstancing
        $splice(VFXInitInstancing)

        const StripData stripData = GetStripDataFromStripIndex(index, instanceIndex);
        uint relativeIndexInStrip = ((id % vertexPerStripCount) >> 2) + (id & 1); // relative index of particle

        uint maxEdgeIndex = relativeIndexInStrip - PARTICLE_IN_EDGE + 1;

        if (maxEdgeIndex >= stripData.nextIndex)
            return false;

        element.stripData = stripData;
        element.relativeIndexInStrip = relativeIndexInStrip;

        index = GetParticleIndex(relativeIndexInStrip, stripData);
    #else
        index = (id >> 2) + VFX_GET_INSTANCE_ID(i) * 2048;
    #endif
    #elif VFX_PRIMITIVE_OCTAGON
        index = (id >> 3) + VFX_GET_INSTANCE_ID(i) * 1024;
    #endif

    #if !HAS_STRIPS
    $splice(VFXInitInstancing)
    #ifdef UNITY_INSTANCING_ENABLED
    input.instanceID = unity_InstanceID;
    #endif
    #endif

    ContextData contextData = instancingContextData[instanceActiveIndex];
    uint systemSeed = contextData.systemSeed;
    uint nbMax = contextData.maxParticleCount;

    if (ShouldCullElement(index, instanceIndex, nbMax))
        return false;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[VFXGetIndirectBufferIndex(index, instanceActiveIndex)];
    #endif

    element.index = index;
    element.instanceIndex = instanceIndex;
    element.instanceActiveIndex = instanceActiveIndex;

    // Configure planar Primitive
    float4 uv = 0;

    #if VFX_PRIMITIVE_QUAD
        #if HAS_STRIPS
        #if VFX_STRIPS_UV_STRECHED
            uv.x = (float)(relativeIndexInStrip) / (stripData.nextIndex - 1);
        #elif VFX_STRIPS_UV_PER_SEGMENT
            uv.x = PARTICLE_IN_EDGE;
        #else
            GetElementData(element);
            const InternalAttributesElement attributes = element.attributes;
            $splice(VFXLoadGraphValues)
            $splice(VFXLoadTexcoordParameter)
            uv.x = texCoord;
        #endif

            uv.y = (id & 2) * 0.5f;
            const float2 vOffsets = float2(0.0f, uv.y - 0.5f);

        #if VFX_STRIPS_SWAP_UV
            uv.xy = float2(1.0f - uv.y, uv.x);
        #endif

        #else
            uv.x = float(id & 1);
            uv.y = (id & 2) * 0.5f;
            const float2 vOffsets = uv.xy - 0.5f;
        #endif
    #elif VFX_PRIMITIVE_TRIANGLE
        const float2 kOffsets[] = {
            float2(-0.5f,     -0.288675129413604736328125f),
            float2(0.0f,  0.57735025882720947265625f),
            float2(0.5f,  -0.288675129413604736328125f),
        };

        const float kUVScale = 0.866025388240814208984375f;

        const float2 vOffsets = kOffsets[id % 3];
        uv.xy = (vOffsets * kUVScale) + 0.5f;
    #elif VFX_PRIMITIVE_OCTAGON
        const float2 kUvs[8] =
        {
            float2(-0.5f, 0.0f),
            float2(-0.5f, 0.5f),
            float2(0.0f,  0.5f),
            float2(0.5f,  0.5f),
            float2(0.5f,  0.0f),
            float2(0.5f,  -0.5f),
            float2(0.0f,  -0.5f),
            float2(-0.5f, -0.5f),
        };

        GetElementData(element);
        const InternalAttributesElement attributes = element.attributes;

        $splice(VFXLoadGraphValues)

        // Here we have to explicitly splice in the crop factor.
        $splice(VFXLoadCropFactorParameter)

        const float correctedCropFactor = id & 1 ? 1.0f - cropFactor : 1.0f;
        const float2 vOffsets = kUvs[id & 7] * correctedCropFactor;
        uv.xy = vOffsets + 0.5f;
    #endif

    input.positionOS = float3(vOffsets, 0.0);
#ifdef ATTRIBUTES_NEED_NORMAL
    input.normalOS = float3(0, 0, -1);
#endif
#ifdef ATTRIBUTES_NEED_TANGENT
    input.tangentOS = float4(1, 0, 0, 1);
#endif
#ifdef ATTRIBUTES_NEED_COLOR
    input.color = float4(1, 1, 1, 1);
#endif

#ifdef ATTRIBUTES_NEED_TEXCOORD0
    input.uv0 = uv;
#endif

    return true;
}
