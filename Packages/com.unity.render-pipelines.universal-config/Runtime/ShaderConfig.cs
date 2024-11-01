//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------
using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Project-wide shader configuration options.
    /// </summary>
    /// <remarks>This enum will generate the proper shader defines.</remarks>
    ///<seealso cref="ShaderConfig"/>
    [GenerateHLSL]
    public static class ShaderOptions
    {
        /// <summary>Max number of lights supported on mobile with OpenGL 3.0 and below.</summary>
        public const int k_MaxVisibleLightCountLowEndMobile = 16;

        /// <summary>Max number of lights supported on mobile, OpenGL, and WebGPU platforms.</summary>
        public const int k_MaxVisibleLightCountMobile = 32;

        /// <summary>Max number of lights supported on desktop platforms.</summary>
        public const int k_MaxVisibleLightCountDesktop = 256;

        /// <summary> Switch fog keywords (FOG_LINEAR, FOG_EXP and FOG_EXP2) to dynamic branching.</summary>
        /// <remarks> For more information on dynamic branches, refer to [Shader Branching](https://docs.unity3d.com/Manual/shader-branching.html).</remarks>
        public const int k_UseDynamicBranchFogKeyword = 0;
    };
}
