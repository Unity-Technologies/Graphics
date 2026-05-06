using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    /// <summary>
    /// Resource class that handles the serialization of UnifiedRayTracing's utility shaders in projects using a Scriptable Render Pipeline.
    /// </summary>
    /// <remarks>
    /// This class is used internally by <see cref="RayTracingResources.LoadFromRenderPipelineResources"/>
    /// </remarks>
    [Scripting.APIUpdating.MovedFrom(
        autoUpdateAPI: true,
        sourceNamespace: "UnityEngine.Rendering.UnifiedRayTracing",
        sourceAssembly: "Unity.Rendering.LightTransport.Runtime"
    )]
    [Serializable]
    [SupportedOnRenderPipeline()]
    [Categorization.CategoryInfo(Name = "R: Unified Ray Tracing", Order = 1000), HideInInspector]
    public sealed class RayTracingRenderPipelineResources : IRenderPipelineResources
    {
        [SerializeField, HideInInspector] int m_Version = 1;

        /// <summary>
        /// The version number of the resources container.
        /// </summary>
        public int version
        {
            get => m_Version;
        }

        [SerializeField, ResourcePath("Runtime/UnifiedRayTracing/Common/GeometryPool/GeometryPoolKernels.compute")]
        ComputeShader m_GeometryPoolKernels;

        [SerializeField, ResourcePath("Runtime/UnifiedRayTracing/Common/Utilities/CopyBuffer.compute")]
        ComputeShader m_CopyBuffer;

        [SerializeField, ResourcePath("Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/copyPositions.compute")]
        ComputeShader m_CopyPositions;

        [SerializeField, ResourcePath("Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/bit_histogram.compute")]
        ComputeShader m_BitHistogram;

        [SerializeField, ResourcePath("Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/block_reduce_part.compute")]
        ComputeShader m_BlockReducePart;

        [SerializeField, ResourcePath("Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/block_scan.compute")]
        ComputeShader m_BlockScan;

        [SerializeField, ResourcePath("Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/build_hlbvh.compute")]
        ComputeShader m_BuildHlbvh;

        [SerializeField, ResourcePath("Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/restructure_bvh.compute")]
        ComputeShader m_RestructureBvh;

        [SerializeField, ResourcePath("Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/scatter.compute")]
        ComputeShader m_Scatter;

        /// <summary>
        /// Compute shader for geometry pool operations.
        /// </summary>
        public ComputeShader GeometryPoolKernels
        {
            get => m_GeometryPoolKernels;
            set => this.SetValueAndNotify(ref m_GeometryPoolKernels, value, nameof(m_GeometryPoolKernels));
        }

        /// <summary>
        /// Compute shader for buffer copy operations.
        /// </summary>
        public ComputeShader CopyBuffer
        {
            get => m_CopyBuffer;
            set => this.SetValueAndNotify(ref m_CopyBuffer, value, nameof(m_CopyBuffer));
        }

        /// <summary>
        /// Compute shader for copying vertex position data.
        /// </summary>
        public ComputeShader CopyPositions
        {
            get => m_CopyPositions;
            set => this.SetValueAndNotify(ref m_CopyPositions, value, nameof(m_CopyPositions));
        }

        /// <summary>
        /// Compute shader for radix sort operations.
        /// </summary>
        public ComputeShader BitHistogram
        {
            get => m_BitHistogram;
            set => this.SetValueAndNotify(ref m_BitHistogram, value, nameof(m_BitHistogram));
        }

        /// <summary>
        /// Compute shader for prefix sum operations.
        /// </summary>
        public ComputeShader BlockReducePart
        {
            get => m_BlockReducePart;
            set => this.SetValueAndNotify(ref m_BlockReducePart, value, nameof(m_BlockReducePart));
        }

        /// <summary>
        /// Compute shader for prefix sum operations.
        /// </summary>
        public ComputeShader BlockScan
        {
            get => m_BlockScan;
            set => this.SetValueAndNotify(ref m_BlockScan, value, nameof(m_BlockScan));
        }

        /// <summary>
        /// Compute shader for building a BVH.
        /// </summary>
        public ComputeShader BuildHlbvh
        {
            get => m_BuildHlbvh;
            set => this.SetValueAndNotify(ref m_BuildHlbvh, value, nameof(m_BuildHlbvh));
        }

        /// <summary>
        /// Compute shader for BVH restructuring.
        /// </summary>
        /// <remarks>
        /// Used to optimize the BVH structure after initial construction.
        /// </remarks>
        public ComputeShader RestructureBvh
        {
            get => m_RestructureBvh;
            set => this.SetValueAndNotify(ref m_RestructureBvh, value, nameof(m_RestructureBvh));
        }

        /// <summary>
        /// Compute shader for radix sort operations.
        /// </summary>
        public ComputeShader Scatter
        {
            get => m_Scatter;
            set => this.SetValueAndNotify(ref m_Scatter, value, nameof(m_Scatter));
        }
    }

    /// <summary>
    /// Utility shaders needed by a <see cref="RayTracingContext"/> to operate.
    /// </summary>
    /// <remarks>
    /// This class holds compute shaders required for unified ray tracing operations,
    /// including geometry pool management and BVH construction kernels.
    /// </remarks>
    public class RayTracingResources
    {
        /// <summary>
        /// Compute shader for geometry pool operations.
        /// </summary>
        public ComputeShader geometryPoolKernels { get; set; }

        /// <summary>
        /// Compute shader for buffer copy operations.
        /// </summary>
        public ComputeShader copyBuffer { get; set; }

        /// <summary>
        /// Compute shader for copying vertex position data.
        /// </summary>
        public ComputeShader copyPositions { get; set; }

        /// <summary>
        /// Compute shader for radix sort operations.
        /// </summary>
        public ComputeShader bitHistogram { get; set; }

        /// <summary>
        /// Compute shader for radix sort operations.
        /// </summary>
        public ComputeShader scatter { get; set; }

        /// <summary>
        /// Compute shader for prefix sum operations.
        /// </summary>
        public ComputeShader blockReducePart { get; set; }

        /// <summary>
        /// Compute shader for prefix sum operations.
        /// </summary>
        public ComputeShader blockScan { get; set; }

        /// <summary>
        /// Compute shader for building a BVH.
        /// </summary>
        public ComputeShader buildHlbvh { get; set; }

        /// <summary>
        /// Compute shader for BVH restructuring.
        /// </summary>
        /// <remarks>
        /// Used to optimize the BVH structure after initial construction.
        /// </remarks>
        public ComputeShader restructureBvh { get; set; }


        

#if UNITY_EDITOR
        /// <summary>
        /// Intializes the RayTracingResources.
        /// </summary>
        /// <remarks>
        /// This API works only in the Unity Editor, not at runtime.
        /// </remarks>
        public void Load()
        {
            const string path = "Packages/com.unity.render-pipelines.core/Runtime/";

            geometryPoolKernels        = AssetDatabase.LoadAssetAtPath<ComputeShader>(path + "UnifiedRayTracing/Common/GeometryPool/GeometryPoolKernels.compute");
            copyBuffer                 = AssetDatabase.LoadAssetAtPath<ComputeShader>(path + "UnifiedRayTracing/Common/Utilities/CopyBuffer.compute");

            copyPositions              = AssetDatabase.LoadAssetAtPath<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/copyPositions.compute");
            bitHistogram               = AssetDatabase.LoadAssetAtPath<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/bit_histogram.compute");
            blockReducePart            = AssetDatabase.LoadAssetAtPath<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/block_reduce_part.compute");
            blockScan                  = AssetDatabase.LoadAssetAtPath<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/block_scan.compute");
            buildHlbvh                 = AssetDatabase.LoadAssetAtPath<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/build_hlbvh.compute");
            restructureBvh             = AssetDatabase.LoadAssetAtPath<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/restructure_bvh.compute");
            scatter                    = AssetDatabase.LoadAssetAtPath<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/scatter.compute");
        }
#endif

#if ENABLE_ASSET_BUNDLE
        /// <summary>
        /// Intializes the RayTracingResources by loading its utility shaders from an AssetBundle.
        /// </summary>
        /// <remarks>
        /// The necessary shaders are configured to belong to the unifiedraytracing AssetBundle which can be built by calling <see cref="UnityEditor.BuildPipeline.BuildAssetBundles"/>
        /// </remarks>
        /// <param name="assetBundle">The AssetBundle to load the shaders from.</param>
        public void LoadFromAssetBundle(AssetBundle assetBundle)
        {
            const string path = "Packages/com.unity.render-pipelines.core/Runtime/";

            geometryPoolKernels = assetBundle.LoadAsset<ComputeShader>(path + "UnifiedRayTracing/Common/GeometryPool/GeometryPoolKernels.compute");
            copyBuffer = assetBundle.LoadAsset<ComputeShader>(path + "UnifiedRayTracing/Common/Utilities/CopyBuffer.compute");

            copyPositions = assetBundle.LoadAsset<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/copyPositions.compute");
            bitHistogram = assetBundle.LoadAsset<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/bit_histogram.compute");
            blockReducePart = assetBundle.LoadAsset<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/block_reduce_part.compute");
            blockScan = assetBundle.LoadAsset<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/block_scan.compute");
            buildHlbvh = assetBundle.LoadAsset<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/build_hlbvh.compute");
            restructureBvh = assetBundle.LoadAsset<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/restructure_bvh.compute");
            scatter = assetBundle.LoadAsset<ComputeShader>(path + "UnifiedRayTracing/Compute/RadeonRays/kernels/scatter.compute");
        }
#endif

        /// <summary>
        /// Intializes the RayTracingResources by loading its utility shaders via GraphicsSettings.
        /// </summary>
        /// <remarks>
        /// This method only works in projects that use Scriptable Render Pipeline.
        /// </remarks>
        /// <returns>Whether the resources were successfully loaded.</returns>
        public bool LoadFromRenderPipelineResources()
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<RayTracingRenderPipelineResources>(out var rpResources))
                return false;

            Debug.Assert(rpResources.GeometryPoolKernels != null);
            Debug.Assert(rpResources.CopyBuffer != null);
            Debug.Assert(rpResources.CopyPositions != null);
            Debug.Assert(rpResources.BitHistogram != null);
            Debug.Assert(rpResources.BlockReducePart != null);
            Debug.Assert(rpResources.BlockScan != null);
            Debug.Assert(rpResources.BuildHlbvh != null);
            Debug.Assert(rpResources.RestructureBvh != null);
            Debug.Assert(rpResources.Scatter != null);

            geometryPoolKernels = rpResources.GeometryPoolKernels;
            copyBuffer = rpResources.CopyBuffer;

            copyPositions = rpResources.CopyPositions;
            bitHistogram = rpResources.BitHistogram;
            blockReducePart = rpResources.BlockReducePart;
            blockScan = rpResources.BlockScan;
            buildHlbvh = rpResources.BuildHlbvh;
            restructureBvh = rpResources.RestructureBvh;
            scatter = rpResources.Scatter;

            return true;
        }
    }

}


