using UnityEngine;
using UnityEngine.Rendering;
using System;
//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [GenerateHLSL(PackingRules.Exact)]
    public enum ShaderOptions
    {
        // TODO: Currently it is not yet possible to use this feature, we need to provide previousPositionCS to the vertex shader as part of Attribute for GBuffer pass
        // TODO: How to enable this feature only on mesh that effectively require it like skinned and moving mesh (other can be done with depth reprojection. But TAA can be an issue)
        VelocityInGBuffer = 0, // Change to 1 to enable the feature, then regenerate hlsl headers.
        PackGBufferInU16 = 0
    };

    // Note: #define can't be use in include file in C# so we chose this way to configure both C# and hlsl
    // Changing a value in this enum Config here require to regenerate the hlsl include and recompile C# and shaders
    public class ShaderConfig
    {
        public const int VelocityInGbuffer = (int)ShaderOptions.VelocityInGBuffer;
        public const int PackgbufferInU16 = (int)ShaderOptions.PackGBufferInU16;
    }
}

//#define VELOCITY_IN_GBUFFER