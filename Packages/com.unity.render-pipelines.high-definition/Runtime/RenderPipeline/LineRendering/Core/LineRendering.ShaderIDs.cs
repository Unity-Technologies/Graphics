namespace UnityEngine.Rendering
{
    partial class LineRendering
    {
        internal static class ShaderIDs
        {
            public static readonly int _ConstantBuffer                   = Shader.PropertyToID("ShaderVariables");
            public static readonly int _CounterBuffer                    = Shader.PropertyToID("_CounterBuffer");
            public static readonly int _SegmentRecordBuffer              = Shader.PropertyToID("_SegmentRecordBuffer");
            public static readonly int _ClusterRecordBuffer              = Shader.PropertyToID("_ClusterRecordBuffer");
            public static readonly int _IndexBuffer                      = Shader.PropertyToID("_IndexBuffer");
            public static readonly int _BinOffsetsBuffer                 = Shader.PropertyToID("_BinOffsetsBuffer");
            public static readonly int _BinCountersBuffer                = Shader.PropertyToID("_BinCountersBuffer");
            public static readonly int _BinIndicesBuffer                 = Shader.PropertyToID("_BinIndicesBuffer");
            public static readonly int _WorkQueueBuffer                  = Shader.PropertyToID("_WorkQueueBuffer");
            public static readonly int _WorkQueueBinListBuffer           = Shader.PropertyToID("_WorkQueueBinListBuffer");
            public static readonly int _OutputWorkQueueArgs              = Shader.PropertyToID("_OutputWorkQueueArgsBuffer");
            public static readonly int _ShadingSamplesTexture            = Shader.PropertyToID("_ShadingSamplesTexture");
            public static readonly int _ShadingScratchTexture            = Shader.PropertyToID("_ShadingScratchTexture");
            public static readonly int _SoftwareLineOffscreenAtlasWidth  = Shader.PropertyToID("_SoftwareLineOffscreenAtlasWidth");
            public static readonly int _SoftwareLineOffscreenAtlasHeight = Shader.PropertyToID("_SoftwareLineOffscreenAtlasHeight");
            public static readonly int _ShadingSampleVisibilityBuffer    = Shader.PropertyToID("_ShadingSampleVisibilityBuffer");
            public static readonly int _ShadingSampleVisibilityCount     = Shader.PropertyToID("_ShadingSampleVisibilityCount");
            public static readonly int _ShadingCompactionBuffer          = Shader.PropertyToID("_ShadingCompactionBuffer");
            public static readonly int _ClusterCountersBuffer            = Shader.PropertyToID("_ClusterCountersBuffer");
            public static readonly int _ClusterRangesBuffer              = Shader.PropertyToID("_ClusterRangesBuffer");
            public static readonly int _OutputTargetColor                = Shader.PropertyToID("_OutputTargetColor");
            public static readonly int _OutputTargetDepth                = Shader.PropertyToID("_OutputTargetDepth");
            public static readonly int _OutputTargetMV                   = Shader.PropertyToID("_OutputTargetMV");
            public static readonly int _ViewSpaceDepthRangeBuffer        = Shader.PropertyToID("_ViewSpaceDepthRangeBuffer");
            public static readonly int _Vertex0RecordBuffer              = Shader.PropertyToID("_Vertex0RecordBuffer");
            public static readonly int _Vertex1RecordBuffer              = Shader.PropertyToID("_Vertex1RecordBuffer");
            public static readonly int _Vertex2RecordBuffer              = Shader.PropertyToID("_Vertex2RecordBuffer");
            public static readonly int _Vertex3RecordBuffer              = Shader.PropertyToID("_Vertex3RecordBuffer");
            public static readonly int _ActiveClusterIndices             = Shader.PropertyToID("_ActiveClusterIndices");
            public static readonly int _BinningArgsBuffer                = Shader.PropertyToID("_BinningArgsBuffer");
            public static readonly int _VertexOffset                     = Shader.PropertyToID("_VertexOffset");
            public static readonly int _SegmentOffset                    = Shader.PropertyToID("_SegmentOffset");

            // Shading Atlas
            public static readonly int _SampleCount                    = Shader.PropertyToID("_SampleCount");
            public static readonly int _ShadingAtlasSampleOffset       = Shader.PropertyToID("_ShadingAtlasSampleOffset");
            public static readonly int _SourceShadingAtlasSampleOffset = Shader.PropertyToID("_SourceShadingAtlasSampleOffset");
            public static readonly int _TargetTextureWidth             = Shader.PropertyToID("_TargetTextureWidth");
            public static readonly int _TargetTextureHeight            = Shader.PropertyToID("_TargetTextureHeight");
            public static readonly int _SourceTextureWidth             = Shader.PropertyToID("_SourceTextureWidth");
            public static readonly int _SourceTextureHeight            = Shader.PropertyToID("_SourceTextureHeight");
            public static readonly int _HistogramBuffer                = Shader.PropertyToID("_HistogramBuffer");
            public static readonly int _SampleIDOffset                 = Shader.PropertyToID("_SampleIDOffset");
            public static readonly int _MaxSamplesToShade              = Shader.PropertyToID("_MaxSamplesToShade");
            public static readonly int _PrefixSumBuffer                = Shader.PropertyToID("_PrefixSumBuffer");

            // LOD
            public static readonly int _LODBuffer       = Shader.PropertyToID("_LODBuffer");
            public static readonly int _SegmentsPerLine = Shader.PropertyToID("_SegmentsPerLine");
            public static readonly int _LineCount       = Shader.PropertyToID("_LineCount");
            public static readonly int _LOD             = Shader.PropertyToID("_LOD");
        }
    }
}
