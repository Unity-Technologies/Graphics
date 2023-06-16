#if SHADERPASS != SHADERPASS_FOGVOLUME_OVERDRAW_DEBUG
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/HDRenderPipeline.VolumetricLighting.cs.hlsl"

uint _VolumetricFogGlobalIndex;
StructuredBuffer<VolumetricMaterialRenderingData> _VolumetricMaterialData;
ByteAddressBuffer _VolumetricGlobalIndirectionBuffer;

float3 GetCubeVertexPosition(uint vertexIndex)
{
    int index = _VolumetricGlobalIndirectionBuffer.Load(_VolumetricFogGlobalIndex << 2);
    return _VolumetricMaterialData[index].obbVertexPositionWS[vertexIndex].xyz;
}

// VertexCubeSlicing needs GetCubeVertexPosition to be declared before
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VertexCubeSlicing.hlsl"

struct VertexToFragment
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

float VBufferDistanceToSliceIndex(uint sliceIndex)
{
    float t0 = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);
    float de = _VBufferRcpSliceCount; // Log-encoded distance between slices

    float e1 = ((float)sliceIndex + 0.5) * de + de;
    return DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
}

VertexToFragment Vert(uint instanceId : INSTANCEID_SEMANTIC, uint vertexId : VERTEXID_SEMANTIC)
{
    VertexToFragment output;

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if USE_VERTEX_CUBE_SLICING

    float sliceDepth = VBufferDistanceToSliceIndex(output.depthSlice);
    float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
    float3 sliceCubeVertexPosition = ComputeCubeSliceVertexPositionRWS(cameraForward, sliceDepth, vertexId);
    output.positionCS = TransformWorldToHClip(float4(sliceCubeVertexPosition, 1.0));

#else

    int materialDataIndex = _VolumetricGlobalIndirectionBuffer.Load(_VolumetricFogGlobalIndex << 2);
    output.positionCS = GetQuadVertexPosition(vertexId);
    output.positionCS.xy = output.positionCS.xy * _VolumetricMaterialData[materialDataIndex].viewSpaceBounds.zw + _VolumetricMaterialData[materialDataIndex].viewSpaceBounds.xy;
    output.positionCS.w = 1;

#endif // USE_VERTEX_CUBE_SLICING

    return output;
}

void Frag(VertexToFragment v2f, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v2f);

    outColor = float4(1, 1, 1, _VBufferRcpSliceCount);
}
