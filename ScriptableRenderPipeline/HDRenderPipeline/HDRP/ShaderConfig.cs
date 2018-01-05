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
        UseDisneySSS = 1,            // Allow to chose between Burley Normalized Diffusion (Multi + Fix direction single scattering) or Jimenez diffusion approximation (Multiscattering only - More blurry) for Subsurface scattering
        VolumetricLightingPreset = 0 // 0 = disabled, 1 = normal, 2 = ultra
    };

    // Note: #define can't be use in include file in C# so we chose this way to configure both C# and hlsl
    // Changing a value in this enum Config here require to regenerate the hlsl include and recompile C# and shaders
    public class ShaderConfig
    {
        public const  int k_CameraRelativeRendering = (int)ShaderOptions.CameraRelativeRendering;
        public static int s_CameraRelativeRendering = (int)ShaderOptions.CameraRelativeRendering;

        public const  int k_UseDisneySSS = (int)ShaderOptions.UseDisneySSS;
        public static int s_UseDisneySSS = (int)ShaderOptions.UseDisneySSS;

        public const  int k_VolumetricLightingPreset = (int)ShaderOptions.VolumetricLightingPreset;
        public static int s_VolumetricLightingPreset = (int)ShaderOptions.VolumetricLightingPreset;
    }
}
