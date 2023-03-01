namespace UnityEngine.Rendering
{
    partial class LineRendering
    {
        internal static class ShaderIDs
        {
            public static int _ConstantBuffer                   = Shader.PropertyToID("ShaderVariables");
            public static int _CounterBuffer                    = Shader.PropertyToID("_CounterBuffer");
            public static int _SegmentRecordBuffer              = Shader.PropertyToID("_SegmentRecordBuffer");
            public static int _ClusterRecordBuffer              = Shader.PropertyToID("_ClusterRecordBuffer");
            public static int _IndexBuffer                      = Shader.PropertyToID("_IndexBuffer");
            public static int _BinOffsetsBuffer                 = Shader.PropertyToID("_BinOffsetsBuffer");
            public static int _BinCountersBuffer                = Shader.PropertyToID("_BinCountersBuffer");
            public static int _BinIndicesBuffer                 = Shader.PropertyToID("_BinIndicesBuffer");
            public static int _WorkQueueBuffer                  = Shader.PropertyToID("_WorkQueueBuffer");
            public static int _WorkQueueBinListBuffer           = Shader.PropertyToID("_WorkQueueBinListBuffer");
            public static int _OutputWorkQueueArgs              = Shader.PropertyToID("_OutputWorkQueueArgsBuffer");
            public static int _ShadingSamplesTexture            = Shader.PropertyToID("_ShadingSamplesTexture");
            public static int _ShadingScratchTexture            = Shader.PropertyToID("_ShadingScratchTexture");
            public static int _SoftwareLineOffscreenAtlasWidth  = Shader.PropertyToID("_SoftwareLineOffscreenAtlasWidth");
            public static int _SoftwareLineOffscreenAtlasHeight = Shader.PropertyToID("_SoftwareLineOffscreenAtlasHeight");
            public static int _ShadingSampleVisibilityBuffer    = Shader.PropertyToID("_ShadingSampleVisibilityBuffer");
            public static int _ShadingSampleVisibilityCount     = Shader.PropertyToID("_ShadingSampleVisibilityCount");
            public static int _ShadingCompactionBuffer          = Shader.PropertyToID("_ShadingCompactionBuffer");
            public static int _ClusterCountersBuffer            = Shader.PropertyToID("_ClusterCountersBuffer");
            public static int _ClusterRangesBuffer              = Shader.PropertyToID("_ClusterRangesBuffer");
            public static int _OutputTargetColor                = Shader.PropertyToID("_OutputTargetColor");
            public static int _OutputTargetDepth                = Shader.PropertyToID("_OutputTargetDepth");
            public static int _OutputTargetMV                   = Shader.PropertyToID("_OutputTargetMV");
            public static int _ViewSpaceDepthRangeBuffer        = Shader.PropertyToID("_ViewSpaceDepthRangeBuffer");
            public static int _Vertex0RecordBuffer              = Shader.PropertyToID("_Vertex0RecordBuffer");
            public static int _Vertex1RecordBuffer              = Shader.PropertyToID("_Vertex1RecordBuffer");
            public static int _Vertex2RecordBuffer              = Shader.PropertyToID("_Vertex2RecordBuffer");
            public static int _Vertex3RecordBuffer              = Shader.PropertyToID("_Vertex3RecordBuffer");
            public static int _ActiveClusterIndices             = Shader.PropertyToID("_ActiveClusterIndices");
            public static int _BinningArgsBuffer                = Shader.PropertyToID("_BinningArgsBuffer");
            public static int _VertexOffset                     = Shader.PropertyToID("_VertexOffset");
            public static int _SegmentOffset                    = Shader.PropertyToID("_SegmentOffset");

            // Shading Atlas
            public static int _SampleCount                    = Shader.PropertyToID("_SampleCount");
            public static int _ShadingAtlasSampleOffset       = Shader.PropertyToID("_ShadingAtlasSampleOffset");
            public static int _SourceShadingAtlasSampleOffset = Shader.PropertyToID("_SourceShadingAtlasSampleOffset");
            public static int _TargetTextureWidth             = Shader.PropertyToID("_TargetTextureWidth");
            public static int _TargetTextureHeight            = Shader.PropertyToID("_TargetTextureHeight");
            public static int _SourceTextureWidth             = Shader.PropertyToID("_SourceTextureWidth");
            public static int _SourceTextureHeight            = Shader.PropertyToID("_SourceTextureHeight");
            public static int _HistogramBuffer                = Shader.PropertyToID("_HistogramBuffer");
            public static int _SampleIDOffset                 = Shader.PropertyToID("_SampleIDOffset");
            public static int _MaxSamplesToShade              = Shader.PropertyToID("_MaxSamplesToShade");
            public static int _PrefixSumBuffer                = Shader.PropertyToID("_PrefixSumBuffer");

            // LOD
            public static int _LODBuffer       = Shader.PropertyToID("_LODBuffer");
            public static int _SegmentsPerLine = Shader.PropertyToID("_SegmentsPerLine");
            public static int _LineCount       = Shader.PropertyToID("_LineCount");
            public static int _LOD             = Shader.PropertyToID("_LOD");
        }
    }
}
