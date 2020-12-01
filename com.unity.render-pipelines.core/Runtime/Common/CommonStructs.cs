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

        /// <summary>Clear both color and depth buffers.</summary>
        All = Depth | Color
    }
}
