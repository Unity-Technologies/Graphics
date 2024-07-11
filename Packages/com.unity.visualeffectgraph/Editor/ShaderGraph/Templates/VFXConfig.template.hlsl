//VFXDefineSpace splice
$splice(VFXDefineSpace)

//VFXDefines splice
$splice(VFXDefines)
#define NULL_GEOMETRY_INPUT defined(HAVE_VFX_PLANAR_PRIMITIVE)

// Explicitly defined here for now (similar to how it was done in the previous VFX code-gen)
#define HAS_VFX_ATTRIBUTES 1

#if HAS_STRIPS
// VFX has some internal functions for strips that assume the generically named "Attributes" struct as input.
// For now, override it. TODO: Improve the generic struct name for VFX shader library.
#define VFXAttributes InternalAttributesElement
#endif

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
StructuredBuffer<uint> deadListCount;
#endif

#if HAS_STRIPS
StructuredBuffer<uint> stripDataBuffer;
#endif

#if VFX_FEATURE_MOTION_VECTORS_FORWARD || USE_MOTION_VECTORS_PASS
ByteAddressBuffer elementToVFXBufferPrevious;
#endif

CBUFFER_START(outputParams)
    float4 instancingConstants;
    float3 cameraXRSettings;
CBUFFER_END

UNITY_INSTANCING_BUFFER_START(PerInstance)
UNITY_DEFINE_INSTANCED_PROP(float, _InstanceIndex)
UNITY_DEFINE_INSTANCED_PROP(float, _InstanceActiveIndex)
UNITY_INSTANCING_BUFFER_END(PerInstance)

// Helper macros to always use a valid instanceID
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    #define VFX_DECLARE_INSTANCE_ID     UNITY_VERTEX_INPUT_INSTANCE_ID
    #define VFX_GET_INSTANCE_ID(i)      unity_InstanceID
#else
    #define VFX_DECLARE_INSTANCE_ID     uint instanceID : SV_InstanceID;
    #define VFX_GET_INSTANCE_ID(i)      input.instanceID
#endif

$splice(VFXSRPCommonInclude)
#include "Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.hlsl"

$splice(VFXParameterBuffer)

$splice(VFXGeneratedBlockFunction)

#include "Packages/com.unity.visualeffectgraph/Shaders/VFXCommonOutput.hlsl"

struct AttributesElement
{
    uint index;
    uint instanceIndex;
    uint instanceActiveIndex;
    // Internal attributes sub-struct used by VFX code-gen property mapping.
    InternalAttributesElement attributes;

    // Additional attribute information for particle strips.
#if HAS_STRIPS
    uint relativeIndexInStrip;
    StripData stripData;
#endif

    // Additional parameter information for motion vectors
#if (VFX_FEATURE_MOTION_VECTORS_FORWARD || USE_MOTION_VECTORS_PASS)
    uint currentFrameIndex;
#endif
};

bool ShouldCullElement(uint index, uint vfxInstanceIndex, uint nbMax)
{
    uint deadCount = 0;
#if USE_DEAD_LIST_COUNT
    deadCount = deadListCount[vfxInstanceIndex];
#endif
    return (index >= nbMax - deadCount);
}

float3 GetElementSize(InternalAttributesElement attributes)
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
float3 GetParticlePosition(uint index, uint instanceIndex)
{
    InternalAttributesElement attributes;
    ZERO_INITIALIZE(InternalAttributesElement, attributes);

    // Here we have to explicitly splice in the position (ShaderGraph splice system lacks regex support etc. :(, unlike VFX's).
    $splice(VFXLoadPositionAttribute)

    return attributes.position;
}

float3 GetStripTangent(float3 currentPos, uint instanceIndex, uint relativeIndex, const StripData stripData)
{
    float3 prevTangent = (float3)0.0f;
    if (relativeIndex > 0)
    {
        uint prevIndex = GetParticleIndex(relativeIndex - 1, stripData);
        float3 tangent = currentPos - GetParticlePosition(prevIndex, instanceIndex);
        float sqrLength = dot(tangent, tangent);
        if (sqrLength > VFX_EPSILON)
            prevTangent = tangent * rsqrt(sqrLength);
    }

    float3 nextTangent = (float3)0.0f;
    if (relativeIndex < stripData.nextIndex - 1)
    {
        uint nextIndex = GetParticleIndex(relativeIndex + 1, stripData);
        float3 tangent = GetParticlePosition(nextIndex, instanceIndex) - currentPos;
        float sqrLength = dot(tangent, tangent);
        if (sqrLength > VFX_EPSILON)
            nextTangent = tangent * rsqrt(sqrLength);
    }

    return normalize(prevTangent + nextTangent);
}
#endif

void GetElementData(inout AttributesElement element)
{
    uint index = element.index;
    uint instanceIndex = element.instanceIndex;
    uint instanceActiveIndex = element.instanceActiveIndex;
#if VFX_USE_GRAPH_VALUES
    $splice(VFXLoadGraphValues)
#endif

    InternalAttributesElement attributes;
    ZERO_INITIALIZE(InternalAttributesElement, attributes);

    $splice(VFXLoadAttribute)

    #if HAS_STRIPS
    const StripData stripData = element.stripData;
    const uint relativeIndexInStrip = element.relativeIndexInStrip;
    InitStripAttributes(index, attributes, element.stripData);
    #endif

#if (VFX_FEATURE_MOTION_VECTORS_FORWARD || USE_MOTION_VECTORS_PASS)
    $splice(VFXLoadCurrentFrameIndexParameter)
    element.currentFrameIndex = currentFrameIndex;
#endif

    $splice(VFXProcessBlocks)

    element.attributes = attributes;
}

// Configure the output type-spcific mesh definition and index calculation for the rest of the element data.
$OutputType.Mesh:            $include("VFXConfigMesh.template.hlsl")
$OutputType.PlanarPrimitive: $include("VFXConfigPlanarPrimitive.template.hlsl")

// Loads the element-specific attribute data, as well as fills any interpolator.
bool GetInterpolatorAndElementData(inout VFX_SRP_VARYINGS output, inout AttributesElement element)
{
    GetElementData(element);
    #if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = element.instanceActiveIndex;
    $splice(VFXLoadGraphValues)
    #endif
    InternalAttributesElement attributes = element.attributes;

    #if !HAS_STRIPS
    if (!attributes.alive)
        return false;
    #endif

    $splice(VFXInterpolantsGeneration)

    return true;
}

// Reconstruct the VFX/World to Element matrix provided by interpolator.
void BuildWorldToElement(VFX_SRP_VARYINGS input)
{
#ifdef VARYINGS_NEED_WORLD_TO_ELEMENT
    worldToElement[0] = input.worldToElement0;
    worldToElement[1] = input.worldToElement1;
    worldToElement[2] = input.worldToElement2;
    worldToElement[3] = float4(0,0,0,1);
#endif
}

void BuildElementToWorld(VFX_SRP_VARYINGS input)
{
#ifdef VARYINGS_NEED_ELEMENT_TO_WORLD
    elementToWorld[0] = input.elementToWorld0;
    elementToWorld[1] = input.elementToWorld1;
    elementToWorld[2] = input.elementToWorld2;
    elementToWorld[3] = float4(0,0,0,1);
#endif
}

void SetupVFXMatrices(AttributesElement element, inout VFX_SRP_VARYINGS output)
{
    // Due to a very stubborn compiler bug we cannot refer directly to the redefined UNITY_MATRIX_M / UNITY_MATRIX_I_M here, due to a rare case where the matrix alias
    // is potentially still the constant object matrices (thus complaining about l-value specifying const object). Note even judicious use of preprocessors seems to
    // fix it, so we instead we directly refer to the static matrices.

    // Element -> World
    elementToWorld = GetElementToVFXMatrix(
        element.attributes.axisX,
        element.attributes.axisY,
        element.attributes.axisZ,
        float3(element.attributes.angleX, element.attributes.angleY, element.attributes.angleZ),
        float3(element.attributes.pivotX, element.attributes.pivotY, element.attributes.pivotZ),
        GetElementSize(element.attributes),
        element.attributes.position
    );

#if VFX_LOCAL_SPACE
    elementToWorld = mul(GetSGVFXUnityObjectToWorld(), elementToWorld);
#elif !defined(VFX_HAS_PICKING_MATRIX_CORRECTION)
    elementToWorld = ApplyCameraTranslationToMatrix(elementToWorld);
#endif

    // World -> Element
    worldToElement = GetVFXToElementMatrix(
        element.attributes.axisX,
        element.attributes.axisY,
        element.attributes.axisZ,
        float3(element.attributes.angleX,element.attributes.angleY,element.attributes.angleZ),
        float3(element.attributes.pivotX,element.attributes.pivotY,element.attributes.pivotZ),
        GetElementSize(element.attributes),
        element.attributes.position
    );

#if VFX_LOCAL_SPACE
    worldToElement = mul(worldToElement,GetSGVFXUnityWorldToObject());
#elif !defined(VFX_HAS_PICKING_MATRIX_CORRECTION)
    worldToElement = ApplyCameraTranslationToInverseMatrix(worldToElement);
#endif

#if VFX_APPLY_CAMERA_POSITION_IN_ELEMENT_MATRIX
    //Specific to PickingSpaceTransforms.hlsl (in HDRP so far)
    //SHADEROPTIONS_CAMERA_RELATIVE_RENDERING has been undef at this stage
    //Avoid removing twice _WorldSpaceCameraPos
    elementToWorld = RevertCameraTranslationFromMatrix(elementToWorld);
    worldToElement = RevertCameraTranslationFromInverseMatrix(worldToElement);
#endif

    // Pack matrices into interpolator if requested by any node.
#ifdef VARYINGS_NEED_ELEMENT_TO_WORLD
    output.elementToWorld0 = elementToWorld[0];
    output.elementToWorld1 = elementToWorld[1];
    output.elementToWorld2 = elementToWorld[2];
#endif

#ifdef VARYINGS_NEED_WORLD_TO_ELEMENT
    output.worldToElement0 = worldToElement[0];
    output.worldToElement1 = worldToElement[1];
    output.worldToElement2 = worldToElement[2];
#endif
}

float4 VFXGetPreviousClipPosition(VFX_SRP_ATTRIBUTES input, AttributesElement element, float4 cPositionFallback)
{
    float4 cPreviousPos = cPositionFallback;

#if (VFX_FEATURE_MOTION_VECTORS_FORWARD || USE_MOTION_VECTORS_PASS)
    uint elementIndex = element.index;
    uint vertexId = input.vertexID;
    uint elementToVFXBaseIndex;
    if (TryGetElementToVFXBaseIndex(elementIndex, element.instanceIndex, elementToVFXBaseIndex, element.currentFrameIndex))
    {
        cPreviousPos = VFXGetPreviousClipPosition(elementToVFXBaseIndex, vertexId);
    }
#endif

    return cPreviousPos;
}

VFX_SRP_ATTRIBUTES VFXTransformMeshToPreviousElement(VFX_SRP_ATTRIBUTES input, AttributesElement element)
{
#if (VFX_FEATURE_MOTION_VECTORS_FORWARD || USE_MOTION_VECTORS_PASS)
    uint elementIndex = element.index;
    uint elementToVFXBaseIndex;
    if (TryGetElementToVFXBaseIndex(elementIndex, element.instanceIndex, elementToVFXBaseIndex, element.currentFrameIndex))
    {
        float4x4 previousElementToVFX = VFXGetPreviousElementToVFX(elementToVFXBaseIndex);
        input.positionOS = mul(previousElementToVFX, float4(input.positionOS, 1.0f)).xyz;
    }
    else
#endif//WRITE_MOTION_VECTOR_IN_FORWARD || USE_MOTION_VECTORS_PASS
    {
        float4x4 elementToVFXMatrix = GetElementToVFXMatrix(
            element.attributes.axisX,
            element.attributes.axisY,
            element.attributes.axisZ,
            float3(element.attributes.angleX, element.attributes.angleY, element.attributes.angleZ),
            float3(element.attributes.pivotX, element.attributes.pivotY, element.attributes.pivotZ),
            GetElementSize(element.attributes),
            element.attributes.position);
        input.positionOS = mul(elementToVFXMatrix, float4(input.positionOS, 1.0f)).xyz;
    }

    return input;
}

// Vertex + Pixel Graph Properties Generation
void GetElementVertexProperties(AttributesElement element, inout GraphProperties properties)
{
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = element.instanceActiveIndex;
    $splice(VFXLoadGraphValues)
#endif
    InternalAttributesElement attributes = element.attributes;
    $splice(VFXVertexPropertiesGeneration)
}

void GetElementPixelProperties(VFX_SRP_SURFACE_INPUTS fragInputs, inout GraphProperties properties)
{
    $splice(VFXPixelPropertiesAssign)
}
