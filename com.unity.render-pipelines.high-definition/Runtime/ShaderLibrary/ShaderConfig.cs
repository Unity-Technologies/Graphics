using UnityEngine;
using System;
//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL(PackingRules.Exact)]
    public enum ShaderOptions
    {
        CameraRelativeRendering = 1, // Rendering sets the origin of the world to the position of the primary (scene view) camera
        PreExposition = 1,
#if ENABLE_RAYTRACING
        Raytracing = 1,
#else
        Raytracing = 0,
#endif
    };

    // Note: #define can't be use in include file in C# so we chose this way to configure both C# and hlsl
    // Changing a value in this enum Config here require to regenerate the hlsl include and recompile C# and shaders
    public class ShaderConfig
    {
        public static int s_CameraRelativeRendering = (int)ShaderOptions.CameraRelativeRendering;
        public static int s_PreExposition = (int)ShaderOptions.PreExposition;
    }
}
