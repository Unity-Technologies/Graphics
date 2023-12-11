#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/LineRendering/Core/LineRenderingCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

ByteAddressBuffer _Vertex0RecordBuffer;
ByteAddressBuffer _Vertex2RecordBuffer;
ByteAddressBuffer _Vertex3RecordBuffer;

ByteAddressBuffer _SegmentRecordBuffer;

ByteAddressBuffer _ShadingCompactionBuffer;

int _VertexOffset;

int _SoftwareLineOffscreenAtlasWidth;
int _SoftwareLineOffscreenAtlasHeight;
int _ShadingSampleVisibilityCount;
float4x4 _InverseCamMatNoJitter;

#define OffscreenAtlasWidth  (uint)_SoftwareLineOffscreenAtlasWidth
#define OffscreenAtlasHeight (uint)_SoftwareLineOffscreenAtlasHeight

uint SampleIndexFromViewportPosition(uint2 positionViewport)
{
    return mad((uint)OffscreenAtlasWidth, positionViewport.y, positionViewport.x);
}

float4 ClipSpaceToRasterSpacePosition(float4 positionCS)
{
#if UNITY_UV_STARTS_AT_TOP
// Our world space, view space, screen space and NDC space are Y-up.
// Our clip space is flipped upside-down due to poor legacy Unity design.
// The flip is baked into the projection matrix, so we only have to flip
// manually when going from CS to NDC and back.
    positionCS.y = -positionCS.y;
#endif

    float4 positionSS = float4(positionCS.xyz / positionCS.w, positionCS.w);

    positionSS.xy = (positionSS.xy * 0.5 + 0.5) * _ScreenSize.xy;

    return positionSS;
}

uint UnpackCompactedSampleIndex(uint id)
{
    if(id >= (uint)_ShadingSampleVisibilityCount) return INVALID_SHADING_SAMPLE;

    return _ShadingCompactionBuffer.Load(id << 2);
}

void OffscreenShadingFillFragInputs(uint2 positionViewport, inout FragInputs output)
{
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    // See: [NOTE-HQ-LINES-SINGLE-PASS-STEREO]
    // Note: The compiler really does not like if we do this inside the LINE_RENDERING_OFFSCREEN_SHADING define.
    unity_StereoEyeIndex = _ViewIndex;
#endif

#if defined(LINE_RENDERING_OFFSCREEN_SHADING)
    uint sampleIndex = SampleIndexFromViewportPosition(positionViewport);
    sampleIndex = UnpackCompactedSampleIndex(sampleIndex);

    if (sampleIndex == INVALID_SHADING_SAMPLE)
        discard;

    const uint vertexID = _VertexOffset + sampleIndex;

    const float4 positionCS   = asfloat(_Vertex0RecordBuffer.Load4(vertexID << 4));
    const float4 encodedFrame = asfloat(_Vertex2RecordBuffer.Load4(vertexID << 4));

    const float3 N = UnpackNormalOctQuadEncode(encodedFrame.xy);
    const float3 T = UnpackNormalOctQuadEncode(encodedFrame.zw);

    float4 texcoord;
#ifdef FRAG_INPUTS_USE_TEXCOORD0
    uint unnormalizedPackedID = _Vertex3RecordBuffer.Load2(8 * vertexID);

    texcoord = float4
    (
        ((unnormalizedPackedID >>  0) & 0xFF) / 255.0,
        ((unnormalizedPackedID >>  8) & 0xFF) / 255.0,
        ((unnormalizedPackedID >> 16) & 0xFF) / 255.0,
        ((unnormalizedPackedID >> 24) & 0xFF) / 255.0
    );
#endif

    // Configure the fragment.
    {
        output.tangentToWorld = BuildTangentToWorld(float4(T, 1), N);
        output.positionRWS    = ComputeWorldSpacePosition(positionCS, UNITY_MATRIX_UNJITTERED_I_VP);
        output.positionSS     = ClipSpaceToRasterSpacePosition(positionCS);
        output.positionPixel  = output.positionSS.xy;
        output.isFrontFace    = true;
#ifdef FRAG_INPUTS_USE_TEXCOORD0
        output.texCoord0      = texcoord;
#endif
    }
#endif
}
