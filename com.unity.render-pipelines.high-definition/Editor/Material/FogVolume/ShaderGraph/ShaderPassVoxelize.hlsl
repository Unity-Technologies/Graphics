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
float3 _RcpPositiveFade;
float3 _RcpNegativeFade;
float _Extinction;
uint _InvertFade;

struct VertexToFragment
{
    float4 positionCS : SV_POSITION;
    float3 viewDirectionWS : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
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
    float depthViewSpace = SliceToDepth(output.depthSlice);

    float4 p = mul(UNITY_MATRIX_P, float4(0, 0, depthViewSpace, 1));

    // output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.positionCS.xy *= _ViewSpaceBounds.zw;
    output.positionCS.xy += _ViewSpaceBounds.xy;
    output.positionCS.z = EyeDepthToLinear(depthViewSpace, _ZBufferParams)/2;
    output.positionCS.w = 1;

    output.positionOS = ComputeWorldSpacePosition(output.positionCS, UNITY_MATRIX_I_VP);
    output.positionOS = GetAbsolutePositionWS(output.positionOS);
    // Encode view direction in texCoord1
    output.viewDirectionWS = GetWorldSpaceViewDir(output.positionOS);

    output.positionOS = mul(output.positionOS, GetWorldToObjectMatrix());

    // Apply view space bounding box to the fullscreen vertex pos:

    return output;
}

FragInputs BuildFragInputs(VertexToFragment v2f)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    float3 positionOS01 = v2f.positionOS / _LocalDensityVolumeExtent;

    PositionInputs posInput = GetPositionInput(v2f.positionCS.xy, _ScreenSize.zw, v2f.positionCS.z, UNITY_MATRIX_I_VP, GetWorldToViewMatrix(), 0);
    output.positionSS = v2f.positionCS;
    output.positionRWS = output.positionPredisplacementRWS = posInput.positionWS;
    output.positionPixel = posInput.positionSS;
    output.texCoord0 = float4(saturate(positionOS01 * 0.5 + 0.5), 0);
    output.tangentToWorld = k_identity3x3;

    return output;
}

float ComputeFadeFactor(float3 positionOS)
{
    float3 p = abs(positionOS) / _LocalDensityVolumeExtent;
    float dstF = max(max(p.x, p.y), p.z);

    float3 coordNDC = (positionOS / _LocalDensityVolumeExtent) * 0.5 + 0.5;
    float3 posF = Remap10(coordNDC, _RcpPositiveFade, _RcpPositiveFade);
    float3 negF = Remap01(coordNDC, _RcpNegativeFade, 0);
    float  fade = posF.x * posF.y * posF.z * negF.x * negF.y * negF.z;

    fade = dstF * (_InvertFade ? (1 - fade) : fade);

    return fade;
}

void Frag(VertexToFragment v2f, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    // Discard pixels outside of the volume bounds:

    float3 scatteringColor;
    float density;
    
    FragInputs fragInputs = BuildFragInputs(v2f);

    if (any(v2f.positionOS > _LocalDensityVolumeExtent) || any(v2f.positionOS < -_LocalDensityVolumeExtent))
        clip(-1);

    // outColor = float4(v2f.positionOS, 1);
    // return;

    GetVolumeData(fragInputs, v2f.viewDirectionWS, scatteringColor, density);

    // Apply volume blending
    float fade = ComputeFadeFactor(v2f.positionOS);
    outColor = float4(scatteringColor, density * fade * _Extinction);
}
