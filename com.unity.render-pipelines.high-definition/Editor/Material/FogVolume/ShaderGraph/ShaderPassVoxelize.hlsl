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

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

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

void Frag(PackedVaryingsToPS packedInput, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    float3 scatteringColor;
    float density;

    // Sample the depth to know when to stop
    float startDistance = LOAD_TEXTURE2D_X_LOD(_FogVolumeDepth, input.positionSS.xy, 0);
    float stopDistance = length(input.positionRWS);

    // Calculate the number of voxels/froxels to write:
    int startVoxelIndex = 0; //DepthToSlice(startDistance);
    int stopVoxelIndex = 20; //DepthToSlice(stopDistance);

    float de = _VBufferRcpSliceCount; // Log-encoded distance between slices
    float t0 = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);

    // float3 objPos = TransformWorldToObject(input.positionRWS);

    // uint3 voxelCoord = uint3(posInput.positionSS.xy, 0);
    // _VBufferDensity[voxelCoord] = float4(input.positionRWS, 0.1);
    // return;
    // float3 p = input.positionRWS;

    float4 color = 0;
    // uint3 voxelCoord = 0;

    for (int voxelDepthIndex = startVoxelIndex; voxelDepthIndex <= stopVoxelIndex; voxelDepthIndex++)
    {
        // It's possible for objects to be outside of the vbuffer bounds (because depth clip is disabled)
        if (voxelDepthIndex < 0 || voxelDepthIndex >= _VBufferSliceCount)
            continue;

        float e1 = voxelDepthIndex * de + de; // (slice + 1) / sliceCount
        float t1 = DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
        float dt = t1 - t0;
        float t = t0 + 0.5 * dt;

        // compute world pos from voxel depth index:

        // TODO: patch position input for SG
        // input.positionRWS = - V * t;
        float t2 = (float)(voxelDepthIndex - startVoxelIndex)/max(0.0001,(float)(stopVoxelIndex - startVoxelIndex));
        input.positionRWS = -V * lerp(startDistance, stopDistance, t2);

        GetVolumeData(input, V, scatteringColor, density);

        // voxelCoord = uint3(posInput.positionSS.xy, voxelDepthIndex + _VBufferSliceCount * unity_StereoEyeIndex);
        color += float4(scatteringColor, density);
    }
    _VBufferDensity[uint3(posInput.positionSS.xy, 0)] = color;

    // TODO: set color mask to 0 and remove this line
    outColor = 0;
}
