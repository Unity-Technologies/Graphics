using System.Diagnostics;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Renderer List resource handle.
    /// </summary>
    [DebuggerDisplay("RendererList ({handle})")]
    public struct RendererListHandle
    {
        bool m_IsValid;
        internal int handle { get; private set; }
        internal RendererListHandle(int handle) { this.handle = handle; m_IsValid = true; }
        /// <summary>
        /// Conversion to int.
        /// </summary>
        /// <param name="handle">Renderer List handle to convert.</param>
        /// <returns>The integer representation of the handle.</returns>
        public static implicit operator int(RendererListHandle handle) { return handle.handle; }

        /// <summary>
        /// Cast to RendererList
        /// </summary>
        /// <param name="rendererList">Input RendererListHandle.</param>
        /// <returns>Resource as a RendererList.</returns>
        public static implicit operator RendererList(RendererListHandle rendererList) => rendererList.IsValid() ? RenderGraphResourceRegistry.current.GetRendererList(rendererList) : RendererList.nullRendererList;

        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => m_IsValid;
    }

    internal struct RendererListResource
    {
        public RendererListDesc desc;
        public RendererList rendererList;

        internal RendererListResource(in RendererListDesc desc)
        {
            this.desc = desc;
            this.rendererList = new RendererList(); // Invalid by default
        }
    }
}
