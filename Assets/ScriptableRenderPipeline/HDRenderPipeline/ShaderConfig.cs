using UnityEngine;
using UnityEngine.Rendering;
using System;
//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------

namespace UnityEngine.Experimental.Rendering.HDPipeline
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
        // const variable produce warning like this one: warning CS0162: Unreachable code detected
        // If we want to avoid them we can add #pragma warning disable 162, however doing that make the debugger shift his line count when debugging which is really annoying
        // so here we decalare two kind of variable, one const that can be use in enum init and one static so the compiler doesn't complain. It mean that the conditional code will stay
        // but it is usually small, so we are fine with it (until someone at microsoft fix the debuggger).
        public const int k_VelocityInGbuffer = (int)ShaderOptions.VelocityInGBuffer;
        public static int s_VelocityInGbuffer = (int)ShaderOptions.VelocityInGBuffer;

        public const int k_PackgbufferInU16 = (int)ShaderOptions.PackGBufferInU16;
        public static int s_PackgbufferInU16 = (int)ShaderOptions.PackGBufferInU16;
    }
}
