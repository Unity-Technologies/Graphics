using System.Diagnostics;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    internal enum RenderGraphResourceType
    {
        Invalid = 0, // Don't change this. We need this to be Zero otherwise default zero initialized RenderGraphResource would have a valid Type
        Texture,
        RendererList
    }

    /// <summary>
    /// Handle to a read-only Render Graph resource.
    /// </summary>
    [DebuggerDisplay("{type} ({handle})")]
    public struct RenderGraphResource
    {
        internal int handle { get; private set; }
        internal RenderGraphResourceType type { get; private set; }

        internal RenderGraphResource(RenderGraphMutableResource mutableResource)
        {
            handle = mutableResource.handle;
            type = mutableResource.type;
        }

        internal RenderGraphResource(int handle, RenderGraphResourceType type)
        {
            this.handle = handle;
            this.type = type;
        }

        public bool IsValid() { return type != RenderGraphResourceType.Invalid; }
    }

    /// <summary>
    /// Handle to a writable Render Graph resource.
    /// </summary>
    [DebuggerDisplay("{type} ({handle})")]
    public struct RenderGraphMutableResource
    {
        internal int handle { get; private set; }
        internal RenderGraphResourceType type { get; private set; }
        internal int version { get; private set; }

        internal RenderGraphMutableResource(int handle, RenderGraphResourceType type)
        {
            this.handle = handle;
            this.type = type;
            this.version = 0;
        }

        internal RenderGraphMutableResource(RenderGraphMutableResource other)
        {
            handle = other.handle;
            type = other.type;
            version = other.version + 1;
        }

        public static implicit operator RenderGraphResource(RenderGraphMutableResource handle)
        {
            return new RenderGraphResource(handle);
        }

        internal bool IsValid() { return type != RenderGraphResourceType.Invalid; }
    }
}
