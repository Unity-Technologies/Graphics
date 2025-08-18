#if SHADERPASS != SHADERPASS_FOG_VOLUME_VOXELIZATION
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Editor/Material/FogVolume/ShaderGraph/VolumetricMaterialUtils.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/HDRenderPipeline.VolumetricLighting.cs.hlsl"

uint _VolumetricFogGlobalIndex;
StructuredBuffer<VolumetricMaterialRenderingData> _VolumetricMaterialData;
ByteAddressBuffer _VolumetricGlobalIndirectionBuffer;

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
    nointerpolation float viewIndex : TEXCOORD2;
    nointerpolation uint depthSlice : SV_RenderTargetArrayIndex;
};

float3 GetCubeVertexPosition(uint vertexIndex)
{
    int index = _VolumetricGlobalIndirectionBuffer.Load(_VolumetricFogGlobalIndex << 2);
    return _VolumetricMaterialData[index].obbVertexPositionWS[vertexIndex].xyz;
}

// VertexCubeSlicing needs GetCubeVertexPosition to be declared before
#define GET_CUBE_VERTEX_POSITION GetCubeVertexPosition
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VertexCubeSlicing.hlsl"

VertexToFragment Vert(uint instanceId : INSTANCEID_SEMANTIC, uint vertexId : VERTEXID_SEMANTIC)
{
    VertexToFragment output;

    int materialDataIndex = _VolumetricGlobalIndirectionBuffer.Load(_VolumetricFogGlobalIndex << 2);


    uint sliceCount = _VolumetricMaterialData[materialDataIndex].sliceCount;
    uint viewIndex = instanceId / sliceCount;
    // In VR sliceCount needs to be the same for each eye to be able to retrieve correctly the view index
    // Patch the mater data index to read the correct view index dependent data
    materialDataIndex += viewIndex * _VolumeCount;

    uint sliceStartIndex = _VolumetricMaterialData[materialDataIndex].startSliceIndex;

    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        unity_StereoEyeIndex = viewIndex;
    #endif
    output.viewIndex = viewIndex;

    uint sliceIndex = sliceStartIndex + (instanceId % sliceCount);
    output.depthSlice = sliceIndex + viewIndex * _VBufferSliceCount;

    float sliceDepth = VBufferDistanceToSliceIndex(sliceIndex);

#if USE_VERTEX_CUBE_SLICING

    float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
    float3 sliceCubeVertexPosition = ComputeCubeSliceVertexPositionRWS(cameraForward, sliceDepth, vertexId);
    output.positionCS = TransformWorldToHClip(float4(sliceCubeVertexPosition, 1.0));
    output.viewDirectionWS = GetWorldSpaceViewDir(sliceCubeVertexPosition);
    output.positionOS = mul(UNITY_MATRIX_I_M, sliceCubeVertexPosition);

#else

    output.positionCS = GetQuadVertexPosition(vertexId);
    output.positionCS.xy = output.positionCS.xy * _VolumetricMaterialData[materialDataIndex].viewSpaceBounds.zw + _VolumetricMaterialData[materialDataIndex].viewSpaceBounds.xy;
    output.positionCS.z = EyeDepthToLinear(sliceDepth, _ZBufferParams);
    output.positionCS.w = 1;

    float3 positionWS = ComputeWorldSpacePosition(output.positionCS, _IsObliqueProjectionMatrix ? _CameraInverseViewProjection_NO : UNITY_MATRIX_I_VP);
    output.viewDirectionWS = GetWorldSpaceViewDir(positionWS);

    // Calculate object space position
    output.positionOS = mul(UNITY_MATRIX_I_M, float4(positionWS, 1)).xyz;

#endif // USE_VERTEX_CUBE_SLICING

    return output;
}

FragInputs BuildFragInputs(VertexToFragment v2f, float3 voxelPositionWS, float3 voxelClipSpace)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    output.positionSS = v2f.positionCS;
    output.positionRWS = output.positionPredisplacementRWS = voxelPositionWS;
    output.positionPixel = uint2(v2f.positionCS.xy);
    output.texCoord0 = float4(saturate(voxelClipSpace * 0.5 + 0.5), 0);
    output.tangentToWorld = k_identity3x3;

    return output;
}

float ComputeFadeFactor(float3 coordNDC, float distance)
{
    bool exponential = uint(_VolumetricMaterialFalloffMode) == LOCALVOLUMETRICFOGFALLOFFMODE_EXPONENTIAL;
    bool multiplyBlendMode = _FogVolumeBlendMode == LOCALVOLUMETRICFOGBLENDINGMODE_MULTIPLY;

    return ComputeVolumeFadeFactor(
        coordNDC, distance,
        _VolumetricMaterialRcpPosFaceFade.xyz,
        _VolumetricMaterialRcpNegFaceFade.xyz,
        _VolumetricMaterialInvertFade,
        _VolumetricMaterialRcpDistFadeLen,
        _VolumetricMaterialEndTimesRcpDistFadeLen,
        exponential,
        multiplyBlendMode
    );
}

void Frag(VertexToFragment v2f, out float4 outColor : SV_Target0)
{
    // We don't need the stereo eye index in this shader and ShaderGraph don't have access to this
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
    unity_StereoEyeIndex = v2f.viewIndex;
    #endif

    float3 albedo;
    float extinction;

    float sliceDistance = VBufferDistanceToSliceIndex(v2f.depthSlice % _VBufferSliceCount);

    // Compute voxel center position and test against volume OBB
    float3 raycenterDirWS = normalize(-v2f.viewDirectionWS); // Normalize
    float3 rayoriginWS    = GetCurrentViewPosition();
    float3 voxelCenterWS = rayoriginWS + sliceDistance * raycenterDirWS;

    // Build rotation matrix from normalized OBB axes to transform the world space position
    float3x3 obbFrame = float3x3(_VolumetricMaterialObbRight.xyz, _VolumetricMaterialObbUp.xyz, cross(_VolumetricMaterialObbRight.xyz, _VolumetricMaterialObbUp.xyz));

    // Rotate world position around the center of the local fog OBB
    float3 voxelCenterBS = mul(GetAbsolutePositionWS(voxelCenterWS - _VolumetricMaterialObbCenter.xyz), transpose(obbFrame));
    float3 voxelCenterCS = (voxelCenterBS * rcp(_VolumetricMaterialObbExtents.xyz));

    // Still need to clip pixels outside of the box because of the froxel buffer shape
    bool overlap = Max3(abs(voxelCenterCS.x), abs(voxelCenterCS.y), abs(voxelCenterCS.z)) <= 1;
    if (!overlap)
        clip(-1);

    FragInputs fragInputs = BuildFragInputs(v2f, voxelCenterWS, voxelCenterCS);
    GetVolumeData(fragInputs, v2f.viewDirectionWS, albedo, extinction);

    // Accumulate volume parameters
    extinction *= ExtinctionFromMeanFreePath(_FogVolumeFogDistanceProperty);
    albedo *= _FogVolumeSingleScatteringAlbedo.rgb;

    float3 voxelCenterNDC = saturate(voxelCenterCS * 0.5 + 0.5);
    float fade = ComputeFadeFactor(voxelCenterNDC, sliceDistance);

    // When multiplying fog, we need to handle specifically the blend area to avoid creating gaps in the fog
    if (_FogVolumeBlendMode == LOCALVOLUMETRICFOGBLENDINGMODE_MULTIPLY)
    {
        outColor = max(0, lerp(float4(1.0, 1.0, 1.0, 1.0), float4(albedo * extinction, extinction), fade.xxxx));
    }
    else
    {
        extinction *= fade;
        outColor = max(0, float4(saturate(albedo * extinction), extinction));
    }
}
