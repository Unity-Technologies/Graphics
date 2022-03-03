#if SHADERPASS != SHADERPASS_VISIBILITY
#error SHADERPASS_is_not_correctly_define
#endif

#ifndef ATTRIBUTES_NEED_VERTEX_ID
    #error Attributes_requires_vertex_id
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;

#if defined(HAVE_RECURSIVE_RENDERING)
    // If we have a recursive raytrace object, we will not render it.
    // As we don't want to rely on renderqueue to exclude the object from the list,
    // we cull it by settings position to NaN value.
    // TODO: provide a solution to filter dyanmically recursive raytrace object in the DrawRenderer
    if (_EnableRecursiveRayTracing && _RayTracing > 0.0)
    {
        ZERO_INITIALIZE(VaryingsType, varyingsType); // Divide by 0 should produce a NaN and thus cull the primitive.
    }
    else
#endif
    {
        varyingsType.vmesh = VertMesh(inputMesh);
        varyingsType.vpass.batchID = (int)_DeferredMaterialInstanceData.y;
    }

    return PackVaryingsType(varyingsType);
}

void Frag(
    PackedVaryingsToPS packedInput,
    out uint outVisibility0 : SV_Target0,
    out uint2 outVisibility1 : SV_Target1)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);
    #ifdef DOTS_INSTANCING_ON
        Visibility::VisibilityData visData;
        visData.valid = true;
        visData.DOTSInstanceIndex = GetDOTSInstanceIndex();
        visData.primitiveID = input.primitiveID;
        visData.batchID = packedInput.vpass.batchID;
        Visibility::PackVisibilityData(visData, outVisibility0, outVisibility1);
    #else
        outVisibility0 = 0;
        outVisibility1 = 0;
    #endif
}
