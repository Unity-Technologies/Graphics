using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    partial class LineRendering
    {
        internal struct Arguments
        {
            public Camera         camera;
            public Vector3        cameraPosition;
            public Frustum        cameraFrustum; // TODO: Frustum us HDRP-specific, we should not use it here.
            public RenderGraph    renderGraph;
            public TextureHandle  depthTexture;
            public SystemSettings settings;
            public ShadingAtlas   shadingAtlas;
            public Vector2        viewport;
            public Matrix4x4      matrixIVP;
            public RenderTargets  targets;
            public int            viewCount;
            public int            viewIndex;
        }

        internal struct SystemSettings
        {
            public int             clusterCount;
            public CompositionMode compositionMode;
            public SortingQuality  sortingQuality;
            public float           tileOpacityThreshold;
            public int             debugMode;
            public MemoryBudget    memoryBudget;
            public bool            executeAsync;
        }

        internal struct SystemResources
        {
            public GPUSort      gpuSort;
            public GPUPrefixSum gpuPrefixSum;

            public ComputeShader stagePrepareCS;
            public ComputeShader stageSetupSegmentCS;
            public ComputeShader stageShadingSetupCS;
            public ComputeShader stageRasterBinCS;
            public ComputeShader stageWorkQueue;
            public ComputeShader stageRasterFineCS;
        }

        /// <summary>
        /// Determines the size of graphics memory allocations for high quality line rendering.
        /// </summary>
        [Serializable]
        public enum MemoryBudget
        {
            /// <summary>Low Budget</summary>
            MemoryBudgetLow = 128,
            /// <summary>Medium Budget</summary>
            MemoryBudgetMedium = 256,
            /// <summary>High Budget</summary>
            MemoryBudgetHigh = 512,
        }

        /// <summary>
        /// List of line rendering debug views.
        /// </summary>
        [GenerateHLSL]
        public enum DebugMode
        {
            /// <summary>Draw a heat value per tile representing the number of segments being computed in the tile.</summary>
            SegmentsPerTile,
            /// <summary>Draw the tile's compute index.</summary>
            TileProcessorUV,
            /// <summary>Draw the cluster index for each computed fragment.</summary>
            ClusterDepth,
        }

        /// <summary>
        /// Container for parameters defining a renderable instance for the line rendering system.
        /// </summary>
        [Serializable]
        public struct RendererData
        {
            /// <summary>Mesh with line topology.</summary>
            public Mesh mesh;
            /// <summary>World Matrix.</summary>
            public Matrix4x4 matrixW;
            /// <summary>Previous World Matrix.</summary>
            public Matrix4x4 matrixWP;
            /// <summary>Material to draw the lines.</summary>
            public Material material;
            /// <summary>Compute asset for computing the vertex shader in a compute shader.</summary>
            public ComputeShader vertexSetupCompute;
            /// <summary>Merging group for sorting between multiple renderer datas.</summary>
            public RendererGroup group;
            /// <summary>Spherical harmonic coefficients for probe lighting.</summary>
            public SphericalHarmonicsL2 probe;
            /// <summary>Rendering mask.</summary>
            public uint renderingLayerMask;
            /// <summary>Motion vector parameters.</summary>
            public Vector4 motionVectorParams;
            /// <summary>Offscreen shading pass index.</summary>
            public int offscreenShadingPass;
            /// <summary>Handle to the line topology's index buffer resource.</summary>
            public BufferHandle indexBuffer;
            /// <summary>Distance to camera for sorting purposes.</summary>
            public float distanceToCamera;
            /// <summary>The number of lines in the mesh.</summary>
            public int lineCount;
            /// <summary>The number of segments-per-line.</summary>
            public int segmentsPerLine;
            /// <summary>Handle to a buffer for computing level of detail.</summary>
            public BufferHandle lodBuffer;
            /// <summary>Level of detail mode.</summary>
            public RendererLODMode lodMode;
            /// <summary>Percentage of strands to render.</summary>
            public float lod;
            /// <summary>Percentage of shading samples to compute.</summary>
            public float shadingFraction;
            /// <summary>Unique identifier for the renderer data.</summary>
            public int hash;
            /// <summary>Bounds of the renderer</summary>
            public Bounds bounds;
        }

        /// <summary>
        /// The method by which line renderer's level of detail will be computed.
        /// </summary>
        public enum RendererLODMode
        {
            /// <summary>No level of detail will be computed.</summary>
            None,
            /// <summary>Define level of detail with a fixed value.</summary>
            Fixed,
            /// <summary>Compute level of detail based on bounding box screen coverage.</summary>
            ScreenCoverage,
            /// <summary>Compute level of detail based on camera distance.</summary>
            CameraDistance
        }

        /// <summary>
        /// The group that line renderers will be merged into for better transparent sorting.
        /// </summary>
        public enum RendererGroup
        {
            /// <summary>No merging will occur with other line renderers.</summary>
            None,
            /// <summary>Group 0.</summary>
            Group0,
            /// <summary>Group 1.</summary>
            Group1,
            /// <summary>Group 2.</summary>
            Group2,
            /// <summary>Group 3.</summary>
            Group3,
            /// <summary>Group 4.</summary>
            Group4,
        }

        internal struct RenderTargets
        {
            public TextureHandle color;
            public TextureHandle depth;
            public TextureHandle motion;
        }

        [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
        internal struct ShaderVariables
        {
            // Stage group sizes.
            public const int NumLaneSegmentSetup = 1024;
            public const int NumLaneRasterBin    = 512;

            // Due to structure alignment issues on certain API (Metal, Vulkan) we ensure 16-byte alignment
            // like this to ensure there is no mismatch.
            public Vector4 _Params0; // { Dim Bin X, Dim Bin Y, Segment Count, Bin Count }
            public Vector4 _Params1; // { Size Screen, Inv. Size Screen }
            public Vector4 _Params2; // { Size Bin, Inv. Size Bin }
            public Vector4 _Params3; // { Vertex Count, Vertex Stride, Active Bin Count, Cluster Depth }
            public Vector4 _Params4; // { Shading Atlas Dim, Cluster Count, Tile Opacity Threshold }
            public Vector4 _Params5; // { View Index, Padding }

            // Aliases
            public Vector2 _DimBin
            {
                get => new Vector2(_Params0.x, _Params0.y);

                set
                {
                    _Params0.x = value.x;
                    _Params0.y = value.y;
                }
            }

            public int _SegmentCount
            {
                get => (int)_Params0.z;
                set => _Params0.z = (float)value;
            }

            public int _BinCount
            {
                get => (int)_Params0.w;
                set => _Params0.w = (float)value;
            }

            public Vector4 _SizeScreen
            {
                get => _Params1;
                set => _Params1 = value;
            }

            public Vector4 _SizeBin
            {
                get => _Params2;
                set => _Params2 = value;
            }

            public int _VertexCount
            {
                get => (int)_Params3.x;
                set => _Params3.x = (float)value;
            }

            public int _VertexStride
            {
                get => (int)_Params3.y;
                set => _Params3.y = (float)value;
            }

            public int _ActiveBinCount
            {
                get => (int)_Params3.z;
                set => _Params3.z = (float)value;
            }

            public int _ClusterDepth
            {
                get => (int)_Params3.w;
                set => _Params3.w = (float)value;
            }

            public Vector2 _ShadingAtlasDimensions
            {
                get => new Vector2(_Params4.x, _Params4.y);

                set
                {
                    _Params4.x = value.x;
                    _Params4.y = value.y;
                }
            }

            public int _ClusterCount
            {
                get => (int)_Params4.z;
                set => _Params4.z = (float)value;
            }

            public float _TileOpacityThreshold
            {
                get => _Params4.w;
                set => _Params4.w = value;
            }

            public int _ViewIndex
            {
                get => (int)_Params5.x;
                set => _Params5.x = (float)value;
            }
        }

        static unsafe int GetShaderVariablesSize()
        {
            return sizeof(ShaderVariables);
        }

        [GenerateHLSL(PackingRules.Exact, false)]
        struct VertexRecord
        {
            public Vector4 positionCS;
            public Vector4 previousPositionCS;
            public Vector3 positionRWS;
            public Vector3 tangentWS;
            public Vector3 normalWS;
            public uint    texCoord0;
            public uint    texCoord1;
        }

        [GenerateHLSL(PackingRules.Exact, false)]
        struct SegmentRecord
        {
            public Vector2 positionSS0;
            public Vector2 positionSS1;

            public float depthVS0;
            public float depthVS1;

            public uint vertexIndex0;
            public uint vertexIndex1;
        }

        [GenerateHLSL(PackingRules.Exact, false)]
        struct ClusterRecord
        {
            public uint segmentIndex;
            public uint clusterIndex;
            public uint clusterOffset;
        }

        internal class SharedPassData
        {
            public ShaderVariables                 shaderVariables;
            public ConstantBuffer<ShaderVariables> shaderVariablesBuffer;

            public TextureHandle   depthRT;
            public SystemResources systemResources;
            public Buffers         sharedBuffers;

            internal struct Buffers
            {
                public BufferHandle  vertexStream0;          // Vertex Stream 0: Position CS
                public BufferHandle  vertexStream1;          // Vertex Stream 1: Previous Position CS
                public BufferHandle  vertexStream2;          // Vertex Stream 2: XY Tangent ZW Normal
                public BufferHandle  vertexStream3;          // Vertex Stream 3: Texcoord
                public BufferHandle  viewSpaceDepthRange;
                public BufferHandle  counterBuffer;
                public BufferHandle  recordBufferSegment;
                public TextureHandle groupShadingSampleAtlas;
                public Vector2Int    groupShadingSampleAtlasDimensions;

                internal struct AllocationParameters
                {
                    public int countSegment;
                    public int countVertex;
                }

                public static Buffers Allocate(RenderGraph renderGraph, AllocationParameters parameters)
                {
                    BufferHandle CreateBuffer(int elementCount, int stride, GraphicsBuffer.Target target, string name)
                    {
                        return renderGraph.CreateBuffer(new BufferDesc(elementCount, stride, target) {name = name});
                    }

                    int shadingSampleAtlasWidth =  Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(parameters.countVertex)));
                    shadingSampleAtlasWidth = Math.Max(shadingSampleAtlasWidth, 1);
                    int shadingSampleAtlasHeight = Mathf.NextPowerOfTwo(Mathf.CeilToInt(DivRoundUp(parameters.countVertex, shadingSampleAtlasWidth)));

                    var resource = new Buffers
                    {
                        vertexStream0           = CreateBuffer(16 * parameters.countVertex, sizeof(uint), GraphicsBuffer.Target.Raw, "Record Buffer [Vertex Stream 0]"),
                        vertexStream1           = CreateBuffer(16 * parameters.countVertex, sizeof(uint), GraphicsBuffer.Target.Raw, "Record Buffer [Vertex Stream 1]"),
                        vertexStream2           = CreateBuffer(16 * parameters.countVertex, sizeof(uint), GraphicsBuffer.Target.Raw, "Record Buffer [Vertex Stream 2]"),
                        vertexStream3           = CreateBuffer(8  * parameters.countVertex, sizeof(uint), GraphicsBuffer.Target.Raw, "Record Buffer [Vertex Stream 3]"),
                        counterBuffer           = CreateBuffer(8, sizeof(uint), GraphicsBuffer.Target.Raw, "Counters"),
                        recordBufferSegment     = CreateBuffer(4 * 2 * parameters.countSegment, sizeof(uint), GraphicsBuffer.Target.Raw, "Record Buffer [Segment]"),
                        viewSpaceDepthRange     = CreateBuffer(2, sizeof(float), GraphicsBuffer.Target.Raw, "View Space Depth Range"),

                        groupShadingSampleAtlas = renderGraph.CreateTexture(new TextureDesc(shadingSampleAtlasWidth, shadingSampleAtlasHeight)
                        {
                            format = GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite = true
                        }),
                        groupShadingSampleAtlasDimensions = new Vector2Int(shadingSampleAtlasWidth, shadingSampleAtlasHeight)
                    };

                    return resource;
                }
            }
        }

        internal class GeometryPassData : SharedPassData
        {
            public Buffers               transientBuffers;
            public RendererData[]        rendererData;
            public ShadingAtlas          shadingAtlas;
            public Matrix4x4             matrixIVP;
            public MaterialPropertyBlock materialPropertyBlock;

            // TODO: Move into RendererData?
            public int[] offsetsVertex;
            public int[] offsetsSegment;

            internal new struct Buffers
            {
                public const int SHADING_SAMPLE_HISTOGRAM_SIZE = 512; //needs to match the shader
                public TextureHandle shadingScratchTexture;
                public Vector2Int shadingScratchTextureDimensions;
                public GPUPrefixSum.RenderGraphResources prefixResources;
                public BufferHandle  shadingScratchBuffer;
                public BufferHandle  shadingSampleHistogram;

                internal struct AllocationParameters
                {
                    public int countVertex;
                    public int countVertexMaxPerRenderer;

                }

                public static Buffers Allocate(RenderGraph renderGraph, RenderGraphBuilder builder, AllocationParameters parameters)
                {
                    BufferHandle CreateBuffer(int elementCount, int stride, GraphicsBuffer.Target target, string name)
                    {
                        return builder.CreateTransientBuffer(new BufferDesc(elementCount, stride, target) { name = name });
                    }

                    int scratchTextureDimension =  Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(parameters.countVertexMaxPerRenderer)));
                    int shadingScratchSize = parameters.countVertexMaxPerRenderer + 1;

                    int prefixMaxItems = Mathf.Max(SHADING_SAMPLE_HISTOGRAM_SIZE, shadingScratchSize);

                    var resource = new Buffers
                    {
                        shadingScratchBuffer    = CreateBuffer(shadingScratchSize, sizeof(uint), GraphicsBuffer.Target.Raw, "Shading Scratch"),
                        shadingSampleHistogram  = CreateBuffer(SHADING_SAMPLE_HISTOGRAM_SIZE + 1, sizeof(uint), GraphicsBuffer.Target.Raw, "Shading Sample Histogram"),
                        prefixResources = GPUPrefixSum.RenderGraphResources.Create(prefixMaxItems, renderGraph, builder),
                        shadingScratchTexture = builder.CreateTransientTexture(new TextureDesc(scratchTextureDimension, scratchTextureDimension)
                        {
                            format = GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite = true
                        }),
                        shadingScratchTextureDimensions = new Vector2Int(scratchTextureDimension, scratchTextureDimension),

                    };

                    return resource;
                }
            }
        }

        internal class RasterizationPassData : SharedPassData
        {
            public Buffers         transientBuffers;
            public RenderTargets   renderTargets;

            public int   qualityModeIndex;
            public int   debugModeIndex;
#if UNITY_EDITOR
            public bool  renderDataStillHasShadersCompiling;
#endif

            public int binCount;
            public int clusterCount;
            public int clusterDepth;

            internal new struct Buffers
            {
                public BufferHandle  counterBufferClusters;
                public BufferHandle  binCounters;
                public BufferHandle  binIndices;
                public BufferHandle  workQueueArgs;
                public BufferHandle  fineRasterArgs;
                public BufferHandle  workQueue;
                public BufferHandle  clusterCounters;
                public BufferHandle  clusterRanges;
                public BufferHandle  activeClusterIndices;
                public BufferHandle  binningIndirectArgs;
                public BufferHandle  recordBufferCluster;

                public GPUPrefixSum.RenderGraphResources prefixResources;
                public GPUSort.RenderGraphResources binSortResources;

                internal struct AllocationParameters
                {
                    public int countBin;
                    public int countCluster;
                    public int depthCluster;
                    public int countBinRecords;
                    public int countWorkQUeue;
                }

                public static Buffers Allocate(RenderGraph renderGraph, RenderGraphBuilder builder, AllocationParameters parameters)
                {
                    BufferHandle CreateBuffer(int elementCount, int stride, GraphicsBuffer.Target target, string name)
                    {
                        return builder.CreateTransientBuffer(new BufferDesc(elementCount, stride, target) { name = name });
                    }

                    var resource = new Buffers
                    {
                        clusterCounters         = CreateBuffer(parameters.countCluster, sizeof(uint), GraphicsBuffer.Target.Raw, "Cluster Counters"),
                        recordBufferCluster     = CreateBuffer(parameters.countBinRecords, Marshal.SizeOf<ClusterRecord>(), GraphicsBuffer.Target.Structured, "Record Buffer [Cluster]"),
                        binCounters             = CreateBuffer(parameters.countBin, sizeof(uint), GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, "Bin Counters"),
                        binIndices              = CreateBuffer(parameters.countBin, sizeof(uint), GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, "Bin Indices"),
                        clusterRanges           = CreateBuffer(2 * parameters.depthCluster, sizeof(float), GraphicsBuffer.Target.Raw, "Cluster Ranges"),
                        activeClusterIndices    = CreateBuffer(parameters.countCluster, sizeof(uint), GraphicsBuffer.Target.Raw, "Active Cluster Indices"),
                        workQueueArgs           = CreateBuffer(4, sizeof(uint), GraphicsBuffer.Target.IndirectArguments, "Work Queue Args"),
                        fineRasterArgs          = CreateBuffer(4, sizeof(uint), GraphicsBuffer.Target.IndirectArguments, "Fine Raster Args"),
                        workQueue               = CreateBuffer(parameters.countWorkQUeue, sizeof(uint), GraphicsBuffer.Target.Raw, "Segment Queue"),
                        binningIndirectArgs     = CreateBuffer(4, sizeof(uint), GraphicsBuffer.Target.IndirectArguments, "Binning Args"),
                        prefixResources    = GPUPrefixSum.RenderGraphResources.Create(parameters.countCluster, renderGraph, builder),
                        binSortResources   = GPUSort.RenderGraphResources.Create(parameters.countBin, renderGraph, builder),
                    };

                    return resource;
                }
            }
        }
    }
}
