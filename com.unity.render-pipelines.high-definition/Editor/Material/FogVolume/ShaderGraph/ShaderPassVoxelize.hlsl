#if SHADERPASS != SHADERPASS_FOGVOLUME_VOXELIZATION
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/HDRenderPipeline.VolumetricLighting.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

RW_TEXTURE3D(float4, _VBufferDensity) : register(u1); // RGB = sqrt(scattering), A = sqrt(extinction)
RW_TEXTURE2D_X(float, _FogVolumeDepth) : register(u2);

float4 _ViewSpaceBounds;
uint _SliceOffset;

struct VertexToFragment
{
    float4 positionCS : SV_POSITION;
    float3 viewDirectionWS : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    uint depthSlice : SV_RenderTargetArrayIndex;
    UNITY_VERTEX_OUTPUT_STEREO
};

uint DepthToSlice(float depth)
{
    // float de = _VBufferRcpSliceCount; // Log-encoded distance between slices
    float vBufferNearPlane = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);

    float t = depth;
    float dt = t - vBufferNearPlane;
    float e1 = EncodeLogarithmicDepthGeneralized(dt, _VBufferDistanceEncodingParams);

    float slice = (e1 - _VBufferRcpSliceCount) / _VBufferRcpSliceCount;

    return uint(slice);
}

float SliceToDepth(uint slice)
{
    float t0 = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);
    float de = _VBufferRcpSliceCount; // Log-encoded distance between slices
    float e1 = slice * de + de; // (slice + 1) / sliceCount
    float t1 = DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
    float dt = t1 - t0;
    return t0 + 0.5 * dt;
}

// float LinearEyeDepth(float depth, float4 zBufferParam)
// {
//     return 1.0 / (zBufferParam.z * depth + zBufferParam.w);
// }

float EyeDepthToLinear(float linearDepth, float4 zBufferParam)
{
    linearDepth = rcp(linearDepth);
    linearDepth -= zBufferParam.w;

    return linearDepth / zBufferParam.z;
}

// TODO: instance id and vertex id in Attributes
VertexToFragment Vert(Attributes input, uint instanceId : SV_INSTANCEID, uint vertexId : SV_VERTEXID)
{
    VertexToFragment output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.depthSlice = _SliceOffset + instanceId;

    output.positionCS = GetQuadVertexPosition(vertexId);
    float distanceViewSpace = SliceToDepth(output.depthSlice);
    // output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.positionCS.xy *= _ViewSpaceBounds.zw;
    output.positionCS.xy += _ViewSpaceBounds.xy;
    output.positionCS.z = EyeDepthToLinear(distanceViewSpace, _ZBufferParams);
    output.positionCS.w = 1;



    output.positionWS = ComputeWorldSpacePosition(output.positionCS, UNITY_MATRIX_I_VP);
    // Encode view direction in texCoord1
    output.viewDirectionWS = GetWorldSpaceViewDir(output.positionWS);

    output.positionWS = mul(output.positionWS, GetWorldToObjectMatrix());

    // Apply view space bounding box to the fullscreen vertex pos:

    return output;
}

FragInputs BuildFragInputs(VertexToFragment v2f)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    PositionInputs posInput = GetPositionInput(v2f.positionCS.xy, _ScreenSize.zw, v2f.positionCS.z, UNITY_MATRIX_I_VP, GetWorldToViewMatrix(), 0);
    output.positionSS = v2f.positionCS;
    output.positionRWS = output.positionPredisplacementRWS = posInput.positionWS;
    output.positionPixel = posInput.positionSS;
    output.texCoord0 = float4(posInput.positionNDC.xy, 0, 0);
    output.tangentToWorld = k_identity3x3;

    return output;
}

void Frag(VertexToFragment v2f, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    // Discard pixels outside of the volume bounds:

    float3 scatteringColor;
    float density;
    
    FragInputs fragInputs = BuildFragInputs(v2f);

    if (any(fragInputs.positionRWS > 5) || any(fragInputs.positionRWS < -5))
        clip(-1);
    
    outColor = float4(fragInputs.positionRWS, 1);
    return;

    GetVolumeData(fragInputs, v2f.viewDirectionWS, scatteringColor, density);
    outColor = float4(scatteringColor, density);

    // input.positionSS is SV_Position

// #ifdef VARYINGS_NEED_POSITION_WS
//     float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
// #else
//     // Unused
//     float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
// #endif

//     float3 scatteringColor;
//     float density;

//     // Sample the depth to know when to stop
//     float startDistance = LOAD_TEXTURE2D_X_LOD(_FogVolumeDepth, input.positionSS.xy, 0);
//     float stopDistance = length(input.positionRWS);

//     // Calculate the number of voxels/froxels to write:
//     int startVoxelIndex = DepthToSlice(startDistance);
//     int stopVoxelIndex = DepthToSlice(stopDistance);

//     float de = _VBufferRcpSliceCount; // Log-encoded distance between slices
//     float t0 = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);

//     // for (int voxelDepthIndex = startVoxelIndex; voxelDepthIndex <= stopVoxelIndex; voxelDepthIndex++)
//     {
//         // It's possible for objects to be outside of the vbuffer bounds (because depth clip is disabled)
//         if (voxelDepthIndex < 0 || voxelDepthIndex >= _VBufferSliceCount)
//             continue;

//         float e1 = voxelDepthIndex * de + de; // (slice + 1) / sliceCount
//         float t1 = DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
//         float dt = t1 - t0;
//         float t = t0 + 0.5 * dt;

//         // compute world pos from voxel depth index:

//         // TODO: patch position input for SG
//         // input.positionRWS = - V * t;
//         float t2 = (float)(voxelDepthIndex - startVoxelIndex) / max(0.0001, (float)(stopVoxelIndex - startVoxelIndex));
//         input.positionRWS = -V * lerp(startDistance, stopDistance, t2);

//         GetVolumeData(input, V, scatteringColor, density);

//         uint3 voxelCoord = uint3(posInput.positionSS.xy, voxelDepthIndex + _VBufferSliceCount * unity_StereoEyeIndex);
//         _VBufferDensity[voxelCoord] += max(0, float4(scatteringColor, density));
//     }

    // TODO: set color mask to 0 and remove this line
}
