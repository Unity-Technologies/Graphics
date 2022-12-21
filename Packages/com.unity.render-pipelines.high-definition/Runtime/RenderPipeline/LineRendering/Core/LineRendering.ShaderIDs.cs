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
        }
    }
}
