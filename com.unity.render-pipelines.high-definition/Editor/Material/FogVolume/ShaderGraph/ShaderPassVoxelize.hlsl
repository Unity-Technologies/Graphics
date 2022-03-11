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
float3 _LocalDensityVolumeExtent;
uint _SliceOffset;
uint _SliceCount;
float3 _RcpPositiveFade;
float3 _RcpNegativeFade;
float _Extinction;
uint _InvertFade;
float _MinDepth;
float _MaxDepth;
uint _FalloffMode;
float4x4 _WorldToLocal; // UNITY_MATRIX_I_M isn't set when doing a DrawProcedural
float _RcpDistanceFadeLength;
float _EndTimesRcpDistanceFadeLength;
float4 _AlbedoMask;

struct VertexToFragment
{
    float4 positionCS : SV_POSITION;
    float3 viewDirectionWS : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
    float depth : TEXCOORD2; // TODO: packing
    uint depthSlice : SV_RenderTargetArrayIndex;
    // TODO: figure out what to do for VR because UNITY_VERTEX_OUTPUT_STEREO uses SV_RenderTargetArrayIndex on some platforms
};

// uint DepthToSlice(float depth)
// {
//     // float de = _VBufferRcpSliceCount; // Log-encoded distance between slices
//     float vBufferNearPlane = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);

//     float t = depth;
//     float dt = t - vBufferNearPlane;
//     float e1 = EncodeLogarithmicDepthGeneralized(dt, _VBufferDistanceEncodingParams);

//     float slice = (e1 - _VBufferRcpSliceCount) / _VBufferRcpSliceCount;

//     return uint(slice);
// }

float EyeDepthToLinear(float linearDepth, float4 zBufferParam)
{
    linearDepth = rcp(linearDepth);
    linearDepth -= zBufferParam.w;

    return linearDepth / zBufferParam.z;
}

// TODO: instance id and vertex id in Attributes
VertexToFragment Vert(Attributes input, uint instanceId : INSTANCEID_SEMANTIC, uint vertexId : VERTEXID_SEMANTIC)
{
    VertexToFragment output;

    // UNITY_SETUP_INSTANCE_ID(input);
    // UNITY_TRANSFER_INSTANCE_ID(input, output);
    // UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.depthSlice = _SliceOffset + instanceId;

    output.positionCS = GetQuadVertexPosition(vertexId);

    float s = float(instanceId) / float(_SliceCount);
    float depthViewSpace = lerp(_MinDepth, _MaxDepth, s);

    // float3 viewSpacevertex = float3(GetQuadVertexPosition(vertexId).xy * _ViewSpaceBounds.zw + _ViewSpaceBounds.xy, -depthViewSpace);

    output.positionCS.xy *= _ViewSpaceBounds.zw;
    output.positionCS.xy += _ViewSpaceBounds.xy;
    output.positionCS.z = EyeDepthToLinear(depthViewSpace, _ZBufferParams);
    output.positionCS.w = 1;

    // float3 positionWS = TransformViewToWorld(viewSpacevertex);
    float3 positionWS = ComputeWorldSpacePosition(output.positionCS, UNITY_MATRIX_I_VP);
    output.viewDirectionWS = GetWorldSpaceViewDir(positionWS);
    // output.positionCS = TransformWorldToHClip(positionWS);

    output.positionOS = mul(_WorldToLocal, float4(GetAbsolutePositionWS(positionWS), 1));

    output.depth = depthViewSpace;

    return output;
}

FragInputs BuildFragInputs(VertexToFragment v2f)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    float3 positionOS01 = v2f.positionOS / _LocalDensityVolumeExtent;

    float3 positionWS = mul(UNITY_MATRIX_M, float4(v2f.positionOS, 1));
    output.positionSS = v2f.positionCS;
    output.positionRWS = output.positionPredisplacementRWS = positionWS;
    output.positionPixel = uint2(v2f.positionCS.xy);
    output.texCoord0 = float4(saturate(positionOS01 * 0.5 + 0.5), 0);
    output.tangentToWorld = k_identity3x3;

    return output;
}

float ComputeFadeFactorPositionOS(float3 positionOS)
{
    float3 coordNDC = (positionOS / _LocalDensityVolumeExtent) * 0.5 + 0.5;
    float distance = length(positionOS);

    return ComputeVolumeFadeFactor(
        coordNDC, distance, _RcpPositiveFade, _RcpNegativeFade,
        _InvertFade, _RcpDistanceFadeLength, _EndTimesRcpDistanceFadeLength, _FalloffMode
    );
}

void Frag(VertexToFragment v2f, out float4 outColor : SV_Target0)
{
    // UNITY_SETUP_INSTANCE_ID(v2f);
    // UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v2f);

    // Discard pixels outside of the volume bounds:

    float3 albedo;
    float extinction;
    
    FragInputs fragInputs = BuildFragInputs(v2f);

    if (any(v2f.positionOS > _LocalDensityVolumeExtent) || any(v2f.positionOS < -_LocalDensityVolumeExtent))
        clip(-1);

    // outColor = float4(v2f.positionOS, 1);
    // return;

    GetVolumeData(fragInputs, v2f.viewDirectionWS, albedo, extinction);

    extinction *= _Extinction;

    float3 scatteringColor = (albedo * _AlbedoMask) * extinction;

    // Apply volume blending
    float fade = ComputeFadeFactorPositionOS(v2f.positionOS);
    outColor = float4(scatteringColor, extinction * fade);
}
