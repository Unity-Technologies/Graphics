#if SHADERPASS != SHADERPASS_VISIBILITY_OIT_COUNT && SHADERPASS != SHADERPASS_VISIBILITY_OIT_STORAGE
#error SHADERPASS_is_not_correctly_define
#endif

#ifndef ATTRIBUTES_NEED_VERTEX_ID
    #error Attributes_requires_vertex_id
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityOITResources.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    varyingsType.vpass.batchID = (int)_DeferredMaterialInstanceData.y;
#ifdef ENCODE_VIS_DEPTH
    varyingsType.vpass.depthValue = varyingsType.vmesh.positionCS.zw;
#endif
    return PackVaryingsType(varyingsType);
}

void FragEmpty(PackedVaryingsToPS packedInput)
{
    //empty fragment shader :)
}


RWByteAddressBuffer _OITOutputSublistCounter : register(u1);
RWByteAddressBuffer _OITOutputSamples : register(u2);
RWByteAddressBuffer _OITOutputPixelHash : register(u3);

void FragStoreVis(PackedVaryingsToPS packedInput)
{
#ifdef DOTS_INSTANCING_ON
    uint2 texelCoord = (uint2)packedInput.vmesh.positionCS.xy;
    uint pixelOffset = texelCoord.y * (uint)_ScreenSize.x + texelCoord.x;
    uint listCount = _VisOITListsCounts.Load(pixelOffset << 2);

    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // TODO: There is probably a better hash we can use, but this seems fine for now.
    // Also note: We need to calculate input before calculating the layerHashValue, otherwise
    // we get the same value everywhere. I'm not sure why.
    uint layerHashValue = JenkinsHash(GetDOTSInstanceIndex() + 1);
    uint ignoredHash = 0;
    _OITOutputPixelHash.InterlockedAdd(pixelOffset << 2, layerHashValue, ignoredHash);

    if (listCount == 0)
        return;

    uint globalOffset = _VisOITListsOffsets.Load(pixelOffset << 2);

    uint outputSublistOffset = 0;
    _OITOutputSublistCounter.InterlockedAdd(pixelOffset << 2, 1, outputSublistOffset);

    Visibility::VisibilityData visData;
    visData.valid = true;
    visData.DOTSInstanceIndex = GetDOTSInstanceIndex();
    visData.primitiveID = input.primitiveID;
    visData.batchID = packedInput.vpass.batchID;

    float zValue = 0.0f;
#ifdef ENCODE_VIS_DEPTH
    zValue = (packedInput.vpass.depthValue.x/packedInput.vpass.depthValue.y);
#endif

    uint3 outPackedData;
    VisibilityOIT::PackVisibilityData(visData, texelCoord, zValue, outPackedData);
    _OITOutputSamples.Store3(((globalOffset + outputSublistOffset) * 3) << 2, outPackedData);
#endif
}
