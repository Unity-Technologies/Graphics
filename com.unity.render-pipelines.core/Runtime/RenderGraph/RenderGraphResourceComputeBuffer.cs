using System;
using System.Diagnostics;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Compute Buffer resource handle.
    /// </summary>
    [DebuggerDisplay("ComputeBuffer ({handle.index})")]
    public struct ComputeBufferHandle
    {
        private static ComputeBufferHandle s_NullHandle = new ComputeBufferHandle();

        /// <summary>
        /// Returns a null compute buffer handle
        /// </summary>
        /// <returns>A null compute buffer handle.</returns>
        public static ComputeBufferHandle nullHandle { get { return s_NullHandle; } }

        internal ResourceHandle handle;

        internal ComputeBufferHandle(int handle, bool shared = false) { this.handle = new ResourceHandle(handle, RenderGraphResourceType.ComputeBuffer, shared); }

        /// <summary>
        /// Cast to ComputeBuffer
        /// </summary>
        /// <param name="buffer">Input ComputeBufferHandle</param>
        /// <returns>Resource as a Compute Buffer.</returns>
        public static implicit operator ComputeBuffer(ComputeBufferHandle buffer) => buffer.IsValid() ? RenderGraphResourceRegistry.current.GetComputeBuffer(buffer) : null;

        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => handle.IsValid();
    }

    /// <summary>
    /// Descriptor used to create compute buffer resources
    /// </summary>
    public struct ComputeBufferDesc
    {
        ///<summary>Number of elements in the buffer..</summary>
        public int count;
        ///<summary>Size of one element in the buffer. Has to match size of buffer type in the shader.</summary>
        public int stride;
        ///<summary>Type of the buffer, default is ComputeBufferType.Default (structured buffer).</summary>
        public ComputeBufferType type;
        /// <summary>Compute Buffer name.</summary>
        public string name;

        /// <summary>
        /// ComputeBufferDesc constructor.
        /// </summary>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="stride">Size of one element in the buffer.</param>
        public ComputeBufferDesc(int count, int stride)
            : this()
        {
            this.count = count;
            this.stride = stride;
            type = ComputeBufferType.Default;
        }

        /// <summary>
        /// ComputeBufferDesc constructor.
        /// </summary>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="stride">Size of one element in the buffer.</param>
        /// <param name="type">Type of the buffer.</param>
        public ComputeBufferDesc(int count, int stride, ComputeBufferType type)
            : this()
        {
            this.count = count;
            this.stride = stride;
            this.type = type;
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
            hashCode = hashCode * 23 + (int)type;

            return hashCode;
        }
    }


    [DebuggerDisplay("ComputeBufferResource ({desc.name})")]
    class ComputeBufferResource : RenderGraphResource<ComputeBufferDesc, ComputeBuffer>
    {
        public override string GetName()
        {
            if (imported)
                return "ImportedComputeBuffer"; // No getter for compute buffer name.
            else
                return desc.name;
        }

        // NOTE:
        // Next two functions should have been implemented in RenderGraphResource<DescType, ResType> but for some reason,
        // when doing so, it's impossible to break in the Texture version of the virtual function (with VS2017 at least), making this completely un-debuggable.
        // To work around this, we just copy/pasted the implementation in each final class...
        public override void CreatePooledGraphicsResource()
        {
            Debug.Assert(m_Pool != null, "CreatePooledGraphicsResource should only be called for regular pooled resources");

            int hashCode = desc.GetHashCode();

            if (graphicsResource != null)
                throw new InvalidOperationException(string.Format("Trying to create an already created resource ({0}). Resource was probably declared for writing more than once in the same pass.", GetName()));

            var pool = m_Pool as ComputeBufferPool;
            if (!pool.TryGetResource(hashCode, out graphicsResource))
            {
                CreateGraphicsResource();
            }

            cachedHash = hashCode;
            pool.RegisterFrameAllocation(cachedHash, graphicsResource);
        }

        public override void ReleasePooledGraphicsResource(int frameIndex)
        {
            if (graphicsResource == null)
                throw new InvalidOperationException($"Tried to release a resource ({GetName()}) that was never created. Check that there is at least one pass writing to it first.");

            // Shared resources don't use the pool
            var pool = m_Pool as ComputeBufferPool;
            if (pool != null)
            {
                pool.ReleaseResource(cachedHash, graphicsResource, frameIndex);
                pool.UnregisterFrameAllocation(cachedHash, graphicsResource);
            }

            Reset(null);
        }

        public override void CreateGraphicsResource(string name = "")
        {
            graphicsResource = new ComputeBuffer(desc.count, desc.stride, desc.type);
            graphicsResource.name = name == "" ? $"RenderGraphComputeBuffer_{desc.count}_{desc.stride}_{desc.type}" : name;
        }

        public override void ReleaseGraphicsResource()
        {
            if (graphicsResource != null)
                graphicsResource.Release();
            base.ReleaseGraphicsResource();
        }

        public override void LogCreation(RenderGraphLogger logger)
        {
            logger.LogLine($"Created ComputeBuffer: {desc.name}");
        }

        public override void LogRelease(RenderGraphLogger logger)
        {
            logger.LogLine($"Released ComputeBuffer: {desc.name}");
        }
    }

    class ComputeBufferPool : RenderGraphResourcePool<ComputeBuffer>
    {
        protected override void ReleaseInternalResource(ComputeBuffer res)
        {
            res.Release();
        }

        protected override string GetResourceName(ComputeBuffer res)
        {
            return "ComputeBufferNameNotAvailable"; // ComputeBuffer.name is a setter only :(
        }

        protected override long GetResourceSize(ComputeBuffer res)
        {
            return res.count * res.stride;
        }

        override protected string GetResourceTypeName()
        {
            return "ComputeBuffer";
        }

        // Another C# nicety.
        // We need to re-implement the whole thing every time because:
        // - obj.resource.Release is Type specific so it cannot be called on a generic (and there's no shared interface for resources like RTHandle, ComputeBuffers etc)
        // - We can't use a virtual release function because it will capture this in the lambda for RemoveAll generating GCAlloc in the process.
        override public void PurgeUnusedResources(int currentFrameIndex)
        {
            // Update the frame index for the lambda. Static because we don't want to capture.
            s_CurrentFrameIndex = currentFrameIndex;

            foreach (var kvp in m_ResourcePool)
            {
                var list = kvp.Value;
                list.RemoveAll(obj =>
                {
                    if (ShouldReleaseResource(obj.frameIndex, s_CurrentFrameIndex))
                    {
                        obj.resource.Release();
                        return true;
                    }
                    return false;
                });
            }
        }
    }
}
