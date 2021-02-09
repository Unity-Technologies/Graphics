$splice(VFXDefineSpace)

$splice(VFXDefines)

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

#if WRITE_MOTION_VECTOR_IN_FORWARD || USE_MOTION_VECTORS_PASS
ByteAddressBuffer elementToVFXBufferPrevious;
#endif

CBUFFER_START(outputParams)
    float nbMax;
    float systemSeed;
CBUFFER_END

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/VFXGraph/Shaders/VFXCommon.hlsl"
#include "Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.hlsl"

$splice(VFXParameterBuffer)

// VFX Graph Block Functions
$splice(VFXGeneratedBlockFunction)

#define VaryingsMeshType VaryingsMeshToPS

// Support the various VFX Primitive types.
// TODO: Add the VFX template directory?
$PrimitiveType.Mesh:            $include("VFX/ConfigMesh.template.hlsl")
$PrimitiveType.PlanarPrimitive: $include("VFX/ConfigPlanarPrimitive.template.hlsl")


// #ifndef VFX_PRIMITIVE_DEFINED
// #error Error: No Primitive Defined.
// #endif
