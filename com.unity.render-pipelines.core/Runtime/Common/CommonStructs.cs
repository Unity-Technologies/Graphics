using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Render Textures clear flag.
    /// </summary>
    [Flags]
    public enum ClearFlag
    {
        /// <summary>Don't clear.</summary>
        None  = 0,
        /// <summary>Clear the color buffer.</summary>
        Color = 1,
        /// <summary>Clear the depth buffer.</summary>
        Depth = 2,
        /// <summary>Clear the stencil buffer.</summary>
        Stencil = 4,
        /// <summary>Clear the depth and stencil buffers.</summary>
        DepthStencil = Depth | Stencil,
        /// <summary>Clear the color and stencil buffers.</summary>
        ColorStencil = Color | Stencil,
        /// <summary>Clear both color, depth and stencil buffers.</summary>
        All = Color | Depth | Stencil
    }
}
