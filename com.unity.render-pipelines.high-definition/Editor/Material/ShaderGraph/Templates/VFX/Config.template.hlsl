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

#if VFX_FEATURE_MOTION_VECTORS_FORWARD || USE_MOTION_VECTORS_PASS
ByteAddressBuffer elementToVFXBufferPrevious;
#endif

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

struct AttributesElement
{
    uint index;
    Attributes attributes;
#if HAS_STRIPS
    uint relativeIndexInStrip;
    StripData stripData;
#endif
};

bool ShouldCullElement(uint index)
{
    uint deadCount = 0;
#if USE_DEAD_LIST_COUNT
    deadCount = deadListCount.Load(0);
#endif
    return (index >= asuint(nbMax) - deadCount);
}

float3 GetElementSize(Attributes attributes)
{
    float3 size3 = float3(attributes.size,attributes.size,attributes.size);

    // TODO: Currently these do not get defined/generated.
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
     // Add an epsilon so that size is never 0 for strips
     size3 += size3 < 0.0f ? -VFX_EPSILON : VFX_EPSILON;
#endif

    return size3;
}

#if HAS_STRIPS
#define PARTICLE_IN_EDGE (id & 1)
float3 GetParticlePosition(uint index)
{
    const Attributes attributes;
    ZERO_INITIALIZE(Attributes, attributes);

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

void GetElementData(inout AttributesElement element)
{
    uint index = element.index;

    Attributes attributes;
    $splice(VFXLoadAttribute)

    #if HAS_STRIPS
    const StripData stripData = element.stripData;
    const uint relativeIndexInStrip = element.relativeIndexInStrip;
    InitStripAttributes(index, attributes, element.stripData);
    #endif

    $splice(VFXProcessBlocks)

    element.attributes = attributes;
}

// Configure the output type-spcific mesh definition and index calculation for the rest of the element data.
$OutputType.Mesh:            $include("VFX/ConfigMesh.template.hlsl")
$OutputType.PlanarPrimitive: $include("VFX/ConfigPlanarPrimitive.template.hlsl")

// Loads the element-specific attribute data, as well as fills any interpolator.
#define VaryingsMeshType VaryingsMeshToPS

bool GetInterpolatorAndElementData(inout VaryingsMeshType output, inout AttributesElement element)
{
    GetElementData(element);

    // Note: Cannot be const due to some VFX generated function require l-value (out / inout).
    Attributes attributes = element.attributes;

    #if !HAS_STRIPS
    if (!attributes.alive)
        return false;
    #endif

    $splice(VFXInterpolantsGeneration)

    return true;
}

// Reconstruct the VFX/World to Element matrix provided by interpolator.
void BuildWorldToElement(VaryingsMeshType input)
{
#ifdef VARYINGS_NEED_WORLD_TO_ELEMENT
    UNITY_MATRIX_I_M[0] = input.worldToElement0;
    UNITY_MATRIX_I_M[1] = input.worldToElement1;
    UNITY_MATRIX_I_M[2] = input.worldToElement2;
    UNITY_MATRIX_I_M[3] = float4(0,0,0,1);
#endif
}

void BuildElementToWorld(VaryingsMeshType input)
{
#ifdef VARYINGS_NEED_ELEMENT_TO_WORLD
    UNITY_MATRIX_M[0] = input.elementToWorld0;
    UNITY_MATRIX_M[1] = input.elementToWorld1;
    UNITY_MATRIX_M[2] = input.elementToWorld2;
    UNITY_MATRIX_M[3] = float4(0,0,0,1);
#endif
}

void SetupVFXMatrices(AttributesElement element, inout VaryingsMeshType output)
{
    // Element -> World
    UNITY_MATRIX_M = GetElementToVFXMatrix(
        element.attributes.axisX,
        element.attributes.axisY,
        element.attributes.axisZ,
        float3(element.attributes.angleX, element.attributes.angleY, element.attributes.angleZ),
        float3(element.attributes.pivotX, element.attributes.pivotY, element.attributes.pivotZ),
        GetElementSize(element.attributes),
        element.attributes.position);

#if VFX_LOCAL_SPACE
    UNITY_MATRIX_M = mul(ApplyCameraTranslationToMatrix(GetRawUnityObjectToWorld()), UNITY_MATRIX_M);
#else
    UNITY_MATRIX_M = ApplyCameraTranslationToMatrix(UNITY_MATRIX_M);
#endif

    // World -> Element
    UNITY_MATRIX_I_M = GetVFXToElementMatrix(
        element.attributes.axisX,
        element.attributes.axisY,
        element.attributes.axisZ,
        float3(element.attributes.angleX,element.attributes.angleY,element.attributes.angleZ),
        float3(element.attributes.pivotX,element.attributes.pivotY,element.attributes.pivotZ),
        GetElementSize(element.attributes),
        element.attributes.position
    );

#if VFX_LOCAL_SPACE
    UNITY_MATRIX_I_M = mul(UNITY_MATRIX_I_M, ApplyCameraTranslationToInverseMatrix(GetRawUnityWorldToObject()));
#else
    UNITY_MATRIX_I_M = ApplyCameraTranslationToInverseMatrix(UNITY_MATRIX_I_M);
#endif

    // Pack matrices into interpolator if requested by any node.
#ifdef VARYINGS_NEED_ELEMENT_TO_WORLD
    output.elementToWorld0 = UNITY_MATRIX_M[0];
    output.elementToWorld1 = UNITY_MATRIX_M[1];
    output.elementToWorld2 = UNITY_MATRIX_M[2];
#endif

#ifdef VARYINGS_NEED_WORLD_TO_ELEMENT
    output.worldToElement0 = UNITY_MATRIX_I_M[0];
    output.worldToElement1 = UNITY_MATRIX_I_M[1];
    output.worldToElement2 = UNITY_MATRIX_I_M[2];
#endif
}

AttributesMesh TransformMeshToPreviousElement(AttributesMesh input, AttributesElement element)
{
#if VFX_FEATURE_MOTION_VECTORS_FORWARD || USE_MOTION_VECTORS_PASS
    uint elementToVFXBaseIndex = element.index * 13;
    uint previousFrameIndex = elementToVFXBufferPrevious.Load(elementToVFXBaseIndex++ << 2);

    float4x4 previousElementToVFX = (float4x4)0;
    previousElementToVFX[3] = float4(0,0,0,1);

    UNITY_UNROLL
    for (int itIndexMatrixRow = 0; itIndexMatrixRow < 3; ++itIndexMatrixRow)
    {
        uint4 read = elementToVFXBufferPrevious.Load4((elementToVFXBaseIndex + itIndexMatrixRow * 4) << 2);
        previousElementToVFX[itIndexMatrixRow] = asfloat(read);
    }

    input.positionOS = mul(previousElementToVFX, float4(input.positionOS, 1.0f)).xyz;

#ifdef ATTRIBUTES_NEED_NORMAL
    // TODO? Not necesarry for MV?
#endif

#endif//WRITE_MOTION_VECTOR_IN_FORWARD || USE_MOTION_VECTORS_PASS

    return input;
}

// Vertex + Pixel Graph Properties Generation
void GetElementVertexProperties(AttributesElement element, inout GraphProperties properties)
{
    Attributes attributes = element.attributes;
    $splice(VFXVertexPropertiesGeneration)
    $splice(VFXVertexPropertiesAssign)
}

void GetElementPixelProperties(FragInputs fragInputs, inout GraphProperties properties)
{
    $splice(VFXPixelPropertiesAssign)
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
