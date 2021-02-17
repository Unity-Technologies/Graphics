$splice(VFXDefineSpace)

$splice(VFXDefines)

// Explicitly defined here for now (similar to how it was done in the previous VFX code-gen)
#define HAS_ATTRIBUTES 1

#define VFX_NEEDS_COLOR_INTERPOLATOR (VFX_USE_COLOR_CURRENT || VFX_USE_ALPHA_CURRENT)
#if HAS_STRIPS
#define VFX_OPTIONAL_INTERPOLATION
#else//
#define VFX_OPTIONAL_INTERPOLATION nointerpolation
#endif

ByteAddressBuffer attributeBuffer;

#if VFX_HAS_INDIRECT_DRAW
StructuredBuffer<uint> indirectBuffer;
#endif

#if USE_DEAD_LIST_COUNT
ByteAddressBuffer deadListCount;
#endif

#if HAS_STRIPS
Buffer<uint> stripDataBuffer;
#endif

// #if WRITE_MOTION_VECTOR_IN_FORWARD || USE_MOTION_VECTORS_PASS
ByteAddressBuffer elementToVFXBufferPrevious;
// #endif

CBUFFER_START(outputParams)
    float nbMax;
    float systemSeed;
CBUFFER_END

// Helper macros to always use a valid instanceID
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    #define VFX_DECLARE_INSTANCE_ID     UNITY_VERTEX_INPUT_INSTANCE_ID
    #define VFX_GET_INSTANCE_ID(i)      unity_InstanceID
#else
    #define VFX_DECLARE_INSTANCE_ID     uint instanceID : SV_InstanceID;
    #define VFX_GET_INSTANCE_ID(i)      input.instanceID
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/VFXGraph/Shaders/VFXCommon.hlsl"
#include "Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.hlsl"

$splice(VFXParameterBuffer)

$splice(VFXGeneratedBlockFunction)

#define VaryingsMeshType VaryingsMeshToPS

void GetElementData(uint index, inout Attributes attributes, inout VaryingsMeshType output)
{
    uint deadCount = 0;
    #if USE_DEAD_LIST_COUNT
    deadCount = deadListCount.Load(0);
    #endif
    if (index >= asuint(nbMax) - deadCount)
        return;

    $splice(VFXLoadAttribute)

    #if HAS_STRIPS
    InitStripAttributes(index, attributes, stripData);
    #endif

    $splice(VFXProcessBlocks)

    #if !HAS_STRIPS
    if (!attributes.alive)
        return;
    #endif

    $splice(VFXInterpolantsGeneration)
}

$OutputType.Mesh:            $include("VFX/ConfigMesh.template.hlsl")
$OutputType.PlanarPrimitive: $include("VFX/ConfigPlanarPrimitive.template.hlsl")

float3 TransformElementToWorld(float3 positionOS, Attributes attributes)
{
    // TODO: Collapse
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

    float4x4 elementToVFX = GetElementToVFXMatrix(
        attributes.axisX,
        attributes.axisY,
        attributes.axisZ,
        float3(attributes.angleX, attributes.angleY, attributes.angleZ),
        float3(attributes.pivotX, attributes.pivotY, attributes.pivotZ),
        size3,
        attributes.position);

    float3 positionPS = mul(elementToVFX, float4(positionOS, 1.0f)).xyz;
    float3 positionWS = TransformPositionVFXToWorld(positionPS);

#ifdef VFX_WORLD_SPACE
    positionWS = GetCameraRelativePositionWS(positionWS);
#endif

    return positionWS;
}

float3 TransformElementToWorldNormal(float3 normalOS, Attributes attributes)
{
    float3 size3 = float3(attributes.size,attributes.size,attributes.size);

    float3x3 elementToVFX_N = GetElementToVFXMatrixNormal(
        attributes.axisX,
        attributes.axisY,
        attributes.axisZ,
        float3(attributes.angleX,attributes.angleY,attributes.angleZ),
        size3);

    float3 normalPS = normalize(mul(elementToVFX_N, normalOS));
    return TransformObjectToWorldNormal(normalPS);
}

float3 TransformPreviousElementToWorld(float3 positionOS, uint index)
{
    uint elementToVFXBaseIndex = index * 13;
    uint previousFrameIndex = elementToVFXBufferPrevious.Load(elementToVFXBaseIndex++ << 2);

   if (asuint(currentFrameIndex) - previousFrameIndex == 1u)    //if (dot(previousElementToVFX[0], 1) != 0)
   {
        float4x4 previousElementToVFX = (float4x4)0;
        previousElementToVFX[3] = float4(0,0,0,1);

        UNITY_UNROLL
        for (int itIndexMatrixRow = 0; itIndexMatrixRow < 3; ++itIndexMatrixRow)
        {
            uint4 read = elementToVFXBufferPrevious.Load4((elementToVFXBaseIndex + itIndexMatrixRow * 4) << 2);
            previousElementToVFX[itIndexMatrixRow] = asfloat(read);
        }

        float3 positionPS = mul(previousElementToVFX, float4(positionOS, 1.0f)).xyz;
        float3 positionWS = TransformPositionVFXToWorld(positionPS);

    #ifdef VFX_WORLD_SPACE
        positionWS = GetCameraRelativePositionWS(positionWS);
    #endif
        return positionWS;
    }

    return TransformPreviousObjectToWorld(positionOS);
}

// Need to redefine GetVaryingsDataDebug since we omit FragInputs.hlsl and generate one procedurally.
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/MaterialDebug.cs.hlsl"

void GetVaryingsDataDebug(uint paramId, FragInputs input, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEWVARYING_TEXCOORD0:
        result = input.texCoord0.xyz;
        break;
    case DEBUGVIEWVARYING_TEXCOORD1:
        result = input.texCoord1.xyz;
        break;
    case DEBUGVIEWVARYING_TEXCOORD2:
        result = input.texCoord2.xyz;
        break;
    case DEBUGVIEWVARYING_TEXCOORD3:
        result = input.texCoord3.xyz;
        break;
    case DEBUGVIEWVARYING_VERTEX_TANGENT_WS:
        result = input.tangentToWorld[0].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_BITANGENT_WS:
        result = input.tangentToWorld[1].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_NORMAL_WS:
        result = IsNormalized(input.tangentToWorld[2].xyz) ?  input.tangentToWorld[2].xyz * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR:
        result = input.color.rgb; needLinearToSRGB = true;
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR_ALPHA:
        result = input.color.aaa;
        break;
    }
}
