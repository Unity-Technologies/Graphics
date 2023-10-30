//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef PROBEREFERENCEVOLUME_STREAMING_CS_HLSL
#define PROBEREFERENCEVOLUME_STREAMING_CS_HLSL
// Generated from UnityEngine.Rendering.ProbeReferenceVolume+CellStreamingScratchBufferLayout
// PackingRules = Exact
CBUFFER_START(CellStreamingScratchBufferLayout)
    int _SharedDestChunksOffset;
    int _L0L1rxOffset;
    int _L1GryOffset;
    int _L1BrzOffset;
    int _ValidityOffset;
    int _SkyOcclusionOffset;
    int _SkyShadingDirectionOffset;
    int _L2_0Offset;
    int _L2_1Offset;
    int _L2_2Offset;
    int _L2_3Offset;
    int _L0Size;
    int _L0ProbeSize;
    int _L1Size;
    int _L1ProbeSize;
    int _ValiditySize;
    int _ValidityProbeSize;
    int _SkyOcclusionSize;
    int _SkyOcclusionProbeSize;
    int _SkyShadingDirectionSize;
    int _SkyShadingDirectionProbeSize;
    int _L2Size;
    int _L2ProbeSize;
    int _ProbeCountInChunkLine;
    int _ProbeCountInChunkSlice;
CBUFFER_END


#endif
