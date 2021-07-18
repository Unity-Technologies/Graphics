using System.Diagnostics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

// Typedefs for the in-engine RendererList API (to avoid conflicts with the experimental version)
using CoreRendererList = UnityEngine.Rendering.RendererUtils.RendererList;
using CoreRendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

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
        public static implicit operator CoreRendererList(RendererListHandle rendererList) => rendererList.IsValid() ? RenderGraphResourceRegistry.current.GetRendererList(rendererList) : CoreRendererList.nullRendererList;

        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => m_IsValid;
    }

    internal struct RendererListResource
    {
        public CoreRendererListDesc desc;
        public CoreRendererList rendererList;

        internal RendererListResource(in CoreRendererListDesc desc)
        {
            this.desc = desc;
            this.rendererList = new CoreRendererList(); // Invalid by default
        }
    }
}
