using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    public partial struct GPUSort
    {
        /// <summary>
        /// Data structure containing runtime dispatch parameters for the sort.
        /// </summary>
        public struct Args
        {
            /// <summary>Count</summary>
            public uint             count;
            /// <summary>Defines the maximum height of the bitonic sort. By default, should be the same as count for a full sort.</summary>
            public uint             maxDepth;
            /// <summary>Input Keys</summary>
            public GraphicsBuffer   inputKeys;
            /// <summary>Input Values</summary>
            public GraphicsBuffer   inputValues;
            /// <summary>Required runtime resources.</summary>
            public SupportResources resources;

            internal int workGroupCount;
        }

        /// <summary>
        /// Utility for adapting to render graph usage.
        /// </summary>
        public struct RenderGraphResources
        {
            /// <summary>Render Graph Buffer Handle to the sorted keys.</summary>
            public BufferHandle sortBufferKeys;
            /// <summary>Render Graph Buffer Handle to the sorted values.</summary>
            public BufferHandle sortBufferValues;

            /// <summary>
            /// Creates the render graph buffer resources from an input count.
            /// </summary>
            /// <param name="count">The number of (key, value) elements.</param>
            /// <param name="renderGraph">Render Graph</param>
            /// <param name="builder">Render Graph Builder</param>
            /// <returns>An initialized RenderGraphResources object containing the created sort buffers.</returns>
            public static RenderGraphResources Create(int count, RenderGraph renderGraph, RenderGraphBuilder builder)
            {
                var targets = GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopyDestination;

                var resources = new RenderGraphResources
                {
                    sortBufferKeys   = builder.CreateTransientBuffer(new BufferDesc(count, 4, targets) { name = "Keys" }),
                    sortBufferValues = builder.CreateTransientBuffer(new BufferDesc(count, 4, targets) { name = "Values" })
                };

                return resources;
            }
        }

        /// <summary>
        /// Data structure containing the runtime resources that are bound by the command buffer.
        /// </summary>
        public struct SupportResources
        {
            /// <summary>Sorted key buffer.</summary>
            public GraphicsBuffer sortBufferKeys;
            /// <summary>Sorted values buffer.</summary>
            public GraphicsBuffer sortBufferValues;

            /// <summary>
            /// Load supporting resources from Render Graph Resources.
            /// </summary>
            /// <param name="renderGraphResources">Render Graph Resources</param>
            /// <returns>A new instance of SupportResources with key and value buffers loaded from the given RenderGraphResources.</returns>
            public static SupportResources Load(RenderGraphResources renderGraphResources)
            {
                var resources = new SupportResources
                {
                    sortBufferKeys   = renderGraphResources.sortBufferKeys,
                    sortBufferValues = renderGraphResources.sortBufferValues
                };

                return resources;
            }

            /// <summary>
            /// Dispose the supporting resources.
            /// </summary>
            public void Dispose()
            {
                if (sortBufferKeys != null)
                {
                    sortBufferKeys.Dispose();
                    sortBufferKeys = null;
                }

                if (sortBufferValues != null)
                {
                    sortBufferValues.Dispose();
                    sortBufferValues = null;
                }
            }
        }

        /// <summary>
        /// Structure defining any required assets used by the GPU sort.
        /// </summary>
        public struct SystemResources
        {
            /// <summary>
            /// The compute asset that defines all of the kernels for the GPU sorting.
            /// </summary>
            public ComputeShader computeAsset;
        }
    }
}
