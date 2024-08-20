using System;
using System.Diagnostics;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Graphics Buffer resource handle.
    /// </summary>
    [DebuggerDisplay("Buffer ({handle.index})")]
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct BufferHandle
    {
        // Minor Warning: This calls the zeroing constructor this means that the embedded ResourceHandle struct will also be zero-ed
        // which then means ResourceHandle.type will be set to zero == Texture. As this is an "invalid" bufferhandle I guess setting it
        // to type texture just makes it even more properly invalid and not a big issue. But something to keep in mind for tooling/logging.
        private static BufferHandle s_NullHandle = new BufferHandle();

        /// <summary>
        /// Returns a null graphics buffer handle
        /// </summary>
        /// <value>A null graphics buffer handle.</value>
        public static BufferHandle nullHandle { get { return s_NullHandle; } }

        internal ResourceHandle handle;

        internal BufferHandle(in ResourceHandle h) { handle = h; }

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
            var hashCode = HashFNV1A32.Create();
            hashCode.Append(count);
            hashCode.Append(stride);
            hashCode.Append((int) target);
            hashCode.Append((int) usageFlags);
            return hashCode.value;
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

        public override int GetDescHashCode() { return desc.GetHashCode(); }

        public override void CreateGraphicsResource()
        {
            var name = GetName();
            graphicsResource = new GraphicsBuffer(desc.target, desc.usageFlags, desc.count, desc.stride);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            graphicsResource.name = name == "" ? $"RenderGraphBuffer_{desc.count}_{desc.stride}_{desc.target}" : name;
#endif
        }

        public override void UpdateGraphicsResource()
        {
            if (graphicsResource != null)
                graphicsResource.name = GetName();
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

        protected override string GetResourceName(in GraphicsBuffer res)
        {
            return "GraphicsBufferNameNotAvailable"; // GraphicsBuffer.name is a setter only :(
        }

        protected override long GetResourceSize(in GraphicsBuffer res)
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
    }
}
