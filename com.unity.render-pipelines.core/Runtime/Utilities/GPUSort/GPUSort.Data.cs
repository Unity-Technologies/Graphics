using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    public partial struct GPUSort
    {
        public struct Args
        {
            public uint             count;
            public uint             maxDepth;
            public int              workGroupCount;
            public GraphicsBuffer   inputKeys;
            public GraphicsBuffer   inputValues;
            public SupportResources resources;
        }

        public struct RenderGraphResources
        {
            public BufferHandle sortBufferKeys;
            public BufferHandle sortBufferValues;

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

        public struct SupportResources
        {
            public GraphicsBuffer sortBufferKeys;
            public GraphicsBuffer sortBufferValues;

            public static SupportResources Load(RenderGraphResources renderGraphResources)
            {
                var resources = new SupportResources
                {
                    sortBufferKeys   = renderGraphResources.sortBufferKeys,
                    sortBufferValues = renderGraphResources.sortBufferValues
                };

                return resources;
            }

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

        public struct SystemResources
        {
            public ComputeShader computeAsset;
        }
    }
}
