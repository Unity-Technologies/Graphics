using System;
using System.Diagnostics;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Graphics Buffer resource handle.
    /// </summary>
    [DebuggerDisplay("Buffer ({handle.index})")]
    public struct BufferHandle
    {
        private static BufferHandle s_NullHandle = new BufferHandle();

        /// <summary>
        /// Returns a null graphics buffer handle
        /// </summary>
        /// <returns>A null graphics buffer handle.</returns>
        public static BufferHandle nullHandle { get { return s_NullHandle; } }

        internal ResourceHandle handle;

        internal BufferHandle(int handle, bool shared = false) { this.handle = new ResourceHandle(handle, RenderGraphResourceType.Buffer, shared); }

        /// <summary>
        /// Cast to GraphicsBuffer
        /// </summary>
        /// <param name="buffer">Input BufferHandle</param>
        /// <returns>Resource as a Graphics Buffer.</returns>
        public static implicit operator GraphicsBuffer(BufferHandle buffer) => buffer.IsValid() ? RenderGraphResourceRegistry.current.GetBuffer(buffer) : null;

        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => handle.IsValid();
    }

    /// <summary>
    /// Descriptor used to create graphics buffer resources
    /// </summary>
    public struct BufferDesc
    {
        ///<summary>Number of elements in the buffer..</summary>
        public int count;
        ///<summary>Size of one element in the buffer. Has to match size of buffer type in the shader.</summary>
        public int stride;
        /// <summary>Graphics Buffer name.</summary>
        public string name;
        /// <summary>The intended usage of a GraphicsBuffer.</summary>
        public GraphicsBuffer.Target target;
        /// <summary>The intended update mode of a GraphicsBuffer.</summary>
        public GraphicsBuffer.UsageFlags usageFlags;

        /// <summary>
        /// BufferDesc constructor.
        /// </summary>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="stride">Size of one element in the buffer.</param>
        public BufferDesc(int count, int stride)
            : this()
        {
            this.count = count;
            this.stride = stride;
            this.target = GraphicsBuffer.Target.Structured;
            this.usageFlags = GraphicsBuffer.UsageFlags.None;
        }

        /// <summary>
        /// BufferDesc constructor.
        /// </summary>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="stride">Size of one element in the buffer.</param>
        /// <param name="target">Type of the buffer.</param>
        public BufferDesc(int count, int stride, GraphicsBuffer.Target target)
            : this()
        {
            this.count = count;
            this.stride = stride;
            this.target = target;
            this.usageFlags = GraphicsBuffer.UsageFlags.None;
        }

        /// <summary>
        /// Hash function
        /// </summary>
        /// <returns>The texture descriptor hash.</returns>
        public override int GetHashCode()
        {
            int hashCode = 17;

            hashCode = hashCode * 23 + count;
            hashCode = hashCode * 23 + stride;
            hashCode = hashCode * 23 + (int)target;
            hashCode = hashCode * 23 + (int)usageFlags;

            return hashCode;
        }
    }


    [DebuggerDisplay("BufferResource ({desc.name})")]
    class BufferResource : RenderGraphResource<BufferDesc, GraphicsBuffer>
    {
        public override string GetName()
        {
            if (imported)
                return "ImportedGraphicsBuffer"; // No getter for graphics buffer name.
            else
                return desc.name;
        }

        // NOTE:
        // Next two functions should have been implemented in RenderGraphResource<DescType, ResType> but for some reason,
        // when doing so, it's impossible to break in the Texture version of the virtual function (with VS2017 at least), making this completely un-debuggable.
        // To work around this, we just copy/pasted the implementation in each final class...
        public override void CreatePooledGraphicsResource()
        {
            Debug.Assert(m_Pool != null, "GraphicsBufferResource: CreatePooledGraphicsResource should only be called for regular pooled resources");

            int hashCode = desc.GetHashCode();

            if (graphicsResource != null)
                throw new InvalidOperationException(string.Format("GraphicsBufferResource: Trying to create an already created resource ({0}). Resource was probably declared for writing more than once in the same pass.", GetName()));

            var pool = m_Pool as BufferPool;
            if (!pool.TryGetResource(hashCode, out graphicsResource))
            {
                CreateGraphicsResource(desc.name);
            }

            cachedHash = hashCode;
            pool.RegisterFrameAllocation(cachedHash, graphicsResource);
            graphicsResource.name = desc.name;
        }

        public override void ReleasePooledGraphicsResource(int frameIndex)
        {
            if (graphicsResource == null)
                throw new InvalidOperationException($"BufferResource: Tried to release a resource ({GetName()}) that was never created. Check that there is at least one pass writing to it first.");

            // Shared resources don't use the pool
            var pool = m_Pool as BufferPool;
            if (pool != null)
            {
                pool.ReleaseResource(cachedHash, graphicsResource, frameIndex);
                pool.UnregisterFrameAllocation(cachedHash, graphicsResource);
            }

            Reset(null);
        }

        public override void CreateGraphicsResource(string name = "")
        {
            graphicsResource = new GraphicsBuffer(desc.target, desc.usageFlags, desc.count, desc.stride);
            graphicsResource.name = name == "" ? $"RenderGraphBuffer_{desc.count}_{desc.stride}_{desc.target}" : name;
        }

        public override void ReleaseGraphicsResource()
        {
            if (graphicsResource != null)
                graphicsResource.Release();
            base.ReleaseGraphicsResource();
        }

        public override void LogCreation(RenderGraphLogger logger)
        {
            logger.LogLine($"Created GraphicsBuffer: {desc.name}");
        }

        public override void LogRelease(RenderGraphLogger logger)
        {
            logger.LogLine($"Released GraphicsBuffer: {desc.name}");
        }
    }

    class BufferPool : RenderGraphResourcePool<GraphicsBuffer>
    {
        protected override void ReleaseInternalResource(GraphicsBuffer res)
        {
            res.Release();
        }

        protected override string GetResourceName(GraphicsBuffer res)
        {
            return "GraphicsBufferNameNotAvailable"; // GraphicsBuffer.name is a setter only :(
        }

        protected override long GetResourceSize(GraphicsBuffer res)
        {
            return res.count * res.stride;
        }

        override protected string GetResourceTypeName()
        {
            return "GraphicsBuffer";
        }

        override protected int GetSortIndex(GraphicsBuffer res)
        {
            return res.GetHashCode();
        }

        // Another C# nicety.
        // We need to re-implement the whole thing every time because:
        // - obj.resource.Release is Type specific so it cannot be called on a generic (and there's no shared interface for resources like RTHandle, GraphicsBuffers etc)
        // - We can't use a virtual release function because it will capture this in the lambda for RemoveAll generating GCAlloc in the process.
        override public void PurgeUnusedResources(int currentFrameIndex)
        {
            // Update the frame index for the lambda. Static because we don't want to capture.
            s_CurrentFrameIndex = currentFrameIndex;
            m_RemoveList.Clear();

            foreach (var kvp in m_ResourcePool)
            {
                // WARNING: No foreach here. Sorted list GetEnumerator generates garbage...
                var list = kvp.Value;
                var keys = list.Keys;
                var values = list.Values;
                for (int i = 0; i < list.Count; ++i)
                {
                    var value = values[i];
                    if (ShouldReleaseResource(value.frameIndex, s_CurrentFrameIndex))
                    {
                        value.resource.Release();
                        m_RemoveList.Add(keys[i]);
                    }
                }

                foreach (var key in m_RemoveList)
                    list.Remove(key);
            }
        }
    }
}
