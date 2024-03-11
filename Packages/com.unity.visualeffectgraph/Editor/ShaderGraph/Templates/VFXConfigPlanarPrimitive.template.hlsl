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
        index = (id >> 2) + VFX_GET_INSTANCE_ID(i) * 2048;
    #elif VFX_PRIMITIVE_OCTAGON
        index = (id >> 3) + VFX_GET_INSTANCE_ID(i) * 1024;
    #endif

    $splice(VFXInitInstancing)
    #ifdef UNITY_INSTANCING_ENABLED
    input.instanceID = unity_InstanceID;
    #endif

    $splice(VFXLoadContextData)
    uint systemSeed = contextData.systemSeed;
    uint nbMax = contextData.maxParticleCount;

    if (ShouldCullElement(index, instanceIndex, nbMax))
        return false;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[VFXGetIndirectBufferIndex(index, instanceActiveIndex)];
    #endif

    #if HAS_STRIPS
    StripData stripData;
    uint relativeIndexInStrip = 0;
    if (!FindIndexInStrip(index, id, instanceIndex, relativeIndexInStrip, stripData))
        return false;

    element.relativeIndexInStrip = relativeIndexInStrip;
    element.stripData = stripData;
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
            uv.x = STRIP_PARTICLE_IN_EDGE;
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



#if defined(SHADER_STAGE_RAY_TRACING)
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/VFXGraph/Shaders/VFXRayTracingCommon.hlsl"

    void GetVFXInstancingIndices(out int index, out int instanceIndex, out int instanceActiveIndex)
    {
        #ifdef VFX_RT_DECIMATION_FACTOR
        int rayTracingDecimationFactor = VFX_RT_DECIMATION_FACTOR;
        #else
        int rayTracingDecimationFactor = 1;
        #endif
        index = PrimitiveIndex() * rayTracingDecimationFactor;
        instanceIndex =  asuint(_InstanceIndex);
        instanceActiveIndex = asuint(_InstanceActiveIndex);
        VFXGetInstanceCurrentIndex(index);
    }

    float GetVFXVertexDisplacement(int index, float3 currentWS, float3 inputVertexPosition, uint currentFrameIndex)
    {
        float displacement = 0.0;
        #if VFX_FEATURE_MOTION_VECTORS
        uint elementToVFXBaseIndex = index * 13;
        uint previousFrameIndex = elementToVFXBufferPrevious.Load(elementToVFXBaseIndex++ << 2);
        if (currentFrameIndex - previousFrameIndex == 1u)    //if (dot(previousElementToVFX[0], 1) != 0)
            {
            float4x4 previousElementToVFX = (float4x4)0;
            previousElementToVFX[3] = float4(0,0,0,1);
            UNITY_UNROLL
            for (int itIndexMatrixRow = 0; itIndexMatrixRow < 3; ++itIndexMatrixRow)
            {
                uint4 read = elementToVFXBufferPrevious.Load4((elementToVFXBaseIndex + itIndexMatrixRow * 4) << 2);
                previousElementToVFX[itIndexMatrixRow] = asfloat(read);
            }
            float3 previousWS = TransformPreviousVFXPositionToWorld(mul(previousElementToVFX, float4(inputVertexPosition, 1.0f)).xyz);
            displacement = length(currentWS - previousWS);
            }
        #endif
        return displacement;
    }

    void BuildFragInputsFromVFXIntersection(AttributeData attributeData, out FragInputs output, out uint outCurrentFrameIndex)
    {
        uint index, instanceIndex, instanceActiveIndex;
        GetVFXInstancingIndices(index, instanceIndex, instanceActiveIndex);
        #if VFX_USE_GRAPH_VALUES
        $splice(VFXLoadGraphValues)
        #endif

        InternalAttributesElement attributes;
        ZERO_INITIALIZE(InternalAttributesElement, attributes);
        $splice(VFXLoadAttribute)
        $splice(VFXProcessBlocks)

        float3 size3 = GetElementSizeRT(attributes
#if VFX_USE_GRAPH_VALUES
            , graphValues
#endif
        );

        float3 rayDirection = WorldRayDirection();
        output.positionSS = float4(0.0, 0.0, 0.0, 0.0);
        output.positionRWS = WorldRayOrigin() + rayDirection * RayTCurrent();
        output.texCoord0 = float4(attributeData.barycentrics,0,0);
        output.texCoord1 = float4(attributeData.barycentrics,0,0);
        output.texCoord2 = float4(attributeData.barycentrics,0,0);
        output.texCoord3 = float4(attributeData.barycentrics,0,0);

        output.color = float4(attributes.color, attributes.alpha);

        // Compute the world space normal
        float3 normalWS = normalize(-WorldToPrimitive(attributes, size3)[2].xyz);
        float3 tangentWS = normalize(WorldToPrimitive(attributes, size3)[0].xyz);
        output.tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, /*sign(currentVertex.tangentOS.w)*/1);

        output.isFrontFace = dot(rayDirection, output.tangentToWorld[2]) < 0.0f;

        $splice(VFXSetFragInputsRT)

    #if VFX_FEATURE_MOTION_VECTORS
        $splice(VFXLoadCurrentFrameIndexParameter)
        outCurrentFrameIndex = currentFrameIndex;
    #endif
    }

#endif
