#if SHADERPASS != SHADERPASS_FOGVOLUME_VOXELIZATION
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/HDRenderPipeline.VolumetricLighting.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

uint _VolumeMaterialDataIndex;
uint _ViewIndex;
float3 _CameraRight;
uint _IsObliqueProjectionMatrix;
float4x4 _CameraInverseViewProjection_NO;
StructuredBuffer<VolumetricMaterialRenderingData> _VolumetricMaterialData;

// Jittered ray with screen-space derivatives.
struct JitteredRay
{
    float3 originWS;
    float3 centerDirWS;
    float3 jitterDirWS;
    float3 xDirDerivWS;
    float3 yDirDerivWS;
};


struct VertexToFragment
{
    float4 positionCS : SV_POSITION;
    float3 viewDirectionWS : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
    uint depthSlice : SV_RenderTargetArrayIndex;
};

float VBufferDistanceToSliceIndex(uint sliceIndex)
{
    float t0 = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);
    float de = _VBufferRcpSliceCount; // Log-encoded distance between slices

    float e1 = ((float)sliceIndex + 0.5) * de + de;
    return DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
}

float EyeDepthToLinear(float linearDepth, float4 zBufferParam)
{
    linearDepth = rcp(linearDepth);
    linearDepth -= zBufferParam.w;

    return linearDepth / zBufferParam.z;
}

float3 GetCubeVertexPosition(uint vertexIndex)
{
    return _VolumetricMaterialData[_VolumeMaterialDataIndex].obbVertexPositionWS[vertexIndex].xyz;
}

// VertexCubeSlicing needs GetCubeVertexPosition to be declared before
#define GET_CUBE_VERTEX_POSITION GetCubeVertexPosition
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VertexCubeSlicing.hlsl"

VertexToFragment Vert(uint instanceId : INSTANCEID_SEMANTIC, uint vertexId : VERTEXID_SEMANTIC)
{
    VertexToFragment output;

#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    unity_StereoEyeIndex = _ViewIndex;
#endif

    uint sliceCount = _VolumetricMaterialData[_VolumeMaterialDataIndex].sliceCount;
    uint sliceStartIndex = _VolumetricMaterialData[_VolumeMaterialDataIndex].startSliceIndex;

    uint sliceIndex = sliceStartIndex + (instanceId % sliceCount);
    output.depthSlice = sliceIndex + _ViewIndex * _VBufferSliceCount;

    float sliceDepth = VBufferDistanceToSliceIndex(sliceIndex);

#if USE_VERTEX_CUBE_SLICING

    float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
    float3 sliceCubeVertexPosition = ComputeCubeSliceVertexPositionRWS(cameraForward, sliceDepth, vertexId);
    output.positionCS = TransformWorldToHClip(float4(sliceCubeVertexPosition, 1.0));
    output.viewDirectionWS = GetWorldSpaceViewDir(sliceCubeVertexPosition);
    output.positionOS = mul(UNITY_MATRIX_I_M, sliceCubeVertexPosition);

#else

    output.positionCS = GetQuadVertexPosition(vertexId);
    output.positionCS.xy = output.positionCS.xy * _VolumetricMaterialData[_VolumeMaterialDataIndex].viewSpaceBounds.zw + _VolumetricMaterialData[_VolumeMaterialDataIndex].viewSpaceBounds.xy;
    output.positionCS.z = EyeDepthToLinear(sliceDepth, _ZBufferParams);
    output.positionCS.w = 1;

    float3 positionWS = ComputeWorldSpacePosition(output.positionCS, _IsObliqueProjectionMatrix ? _CameraInverseViewProjection_NO : UNITY_MATRIX_I_VP);
    output.viewDirectionWS = GetWorldSpaceViewDir(positionWS);

    // Calculate object space position
    output.positionOS = mul(UNITY_MATRIX_I_M, float4(positionWS, 1)).xyz;

#endif // USE_VERTEX_CUBE_SLICING

    return output;
}

FragInputs BuildFragInputs(VertexToFragment v2f, float3 voxelPositionOS, float3 voxelClipSpace)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    float3 positionWS = mul(UNITY_MATRIX_M, float4(voxelPositionOS, 1)).xyz;
    output.positionSS = v2f.positionCS;
    output.positionRWS = output.positionPredisplacementRWS = positionWS;
    output.positionPixel = uint2(v2f.positionCS.xy);
    output.texCoord0 = float4(saturate(voxelClipSpace * 0.5 + 0.5), 0);
    output.tangentToWorld = k_identity3x3;

    return output;
}

float ComputeFadeFactor(float3 coordNDC, float distance)
{
    bool exponential = uint(_VolumetricMaterialFalloffMode) == LOCALVOLUMETRICFOGFALLOFFMODE_EXPONENTIAL;

    return ComputeVolumeFadeFactor(
        coordNDC, distance,
        _VolumetricMaterialRcpPosFaceFade.xyz,
        _VolumetricMaterialRcpNegFaceFade.xyz,
        _VolumetricMaterialInvertFade,
        _VolumetricMaterialRcpDistFadeLen,
        _VolumetricMaterialEndTimesRcpDistFadeLen,
        exponential
    );
}

void Frag(VertexToFragment v2f, out float4 outColor : SV_Target0)
{
    // Setup VR storeo eye index manually because we use the SV_RenderTargetArrayIndex semantic which conflicts with XR macros
#if defined(UNITY_SINGLE_PASS_STEREO)
    unity_StereoEyeIndex = _ViewIndex;
#endif

    float3 albedo;
    float extinction;

    float sliceDepth = VBufferDistanceToSliceIndex(v2f.depthSlice % _VBufferSliceCount);
    float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
    float sliceDistance = sliceDepth;// / dot(-v2f.viewDirectionWS, cameraForward);

    // Compute voxel center position and test against volume OBB
    float3 raycenterDirWS = normalize(-v2f.viewDirectionWS); // Normalize
    float3 rayoriginWS    = GetCurrentViewPosition();
    float3 voxelCenterWS = rayoriginWS + sliceDistance * raycenterDirWS;

    float3x3 obbFrame = float3x3(_VolumetricMaterialObbRight.xyz, _VolumetricMaterialObbUp.xyz, cross(_VolumetricMaterialObbRight.xyz, _VolumetricMaterialObbUp.xyz));

    float3 voxelCenterBS = mul(voxelCenterWS - _VolumetricMaterialObbCenter.xyz, transpose(obbFrame));
    float3 voxelCenterCS = (voxelCenterBS * rcp(_VolumetricMaterialObbExtents.xyz));

    // Still need to clip pixels outside of the box because of the froxel buffer shape
    bool overlap = Max3(abs(voxelCenterCS.x), abs(voxelCenterCS.y), abs(voxelCenterCS.z)) <= 1;
    if (!overlap)
        clip(-1);

    FragInputs fragInputs = BuildFragInputs(v2f, voxelCenterBS, voxelCenterCS);
    GetVolumeData(fragInputs, v2f.viewDirectionWS, albedo, extinction);

    // Accumulate volume parameters
    extinction *= _VolumetricMaterialExtinction;
    albedo *= _VolumetricMaterialAlbedo.rgb;

    float3 voxelCenterNDC = saturate(voxelCenterCS * 0.5 + 0.5);
    float fade = ComputeFadeFactor(voxelCenterNDC, sliceDistance);

    // When multiplying fog, we need to handle specifically the blend area to avoid creating gaps in the fog
#if defined FOG_VOLUME_BLENDING_MULTIPLY
    outColor = max(0, lerp(float4(1.0, 1.0, 1.0, 1.0), float4(saturate(albedo * extinction), extinction), fade.xxxx));
#else
    extinction *= fade;
    outColor = max(0, float4(saturate(albedo * extinction), extinction));
#endif
}
