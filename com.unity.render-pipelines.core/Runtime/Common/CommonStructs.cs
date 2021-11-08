using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Render Textures clear flag.
    /// This is an legacy alias for RTClearFlags.
    /// </summary>
    [Flags]
    public enum ClearFlag
    {
        /// <summary>Don't clear.</summary>
        None = RTClearFlags.None,
        /// <summary>Clear the color buffer.</summary>
        Color = RTClearFlags.Color,
        /// <summary>Clear the depth buffer.</summary>
        Depth = RTClearFlags.Depth,
        /// <summary>Clear the stencil buffer.</summary>
        Stencil = RTClearFlags.Stencil,
        /// <summary>Clear the depth and stencil buffers.</summary>
        DepthStencil = Depth | Stencil,
        /// <summary>Clear the color and stencil buffers.</summary>
        ColorStencil = Color | Stencil,
        /// <summary>Clear both color, depth and stencil buffers.</summary>
        All = Color | Depth | Stencil
    }
}
