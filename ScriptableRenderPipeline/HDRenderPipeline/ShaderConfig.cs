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
        // TODO: It is not possible to use VelocityInGBuffer feature yet. This feature allow to render motion vectors during Gbuffer pass. However Unity have limitation today that forbid to do that.
        // 1) Currently previousPositionCS is provide to the vertex shader with a hard coded NORMAL semantic (in the vertex declaration - See MeshRenderingData.cpp "pSecondaryFormat = gMotionVectorRenderFormat.GetVertexFormat();") mean it will overwrite the normal
        // 2) All current available semantic (see ShaderChannelMask) are used in our Lit shader. Mean just changing the semantic is not enough, Unity need to unlock other Texcoord semantic
        // 3) When this is solve (i.e move previousPositionCS to a free attribute semantic), Unity only support one pSecondaryFormat. Mean if we ahve a vertex color instance stream and motion vector, motion vector will overwrite vertex color stream. See MeshRenderingData.cpp
        // All this could be fix we a new Mesh API not ready yet. Note that this feature only affect animated mesh (vertex or skin) as others use depth reprojection.
        VelocityInGBuffer = 0, // Change to 1 to enable the feature, then regenerate hlsl headers.
        CameraRelativeRendering = 1 // Rendering sets the origin of the world to the position of the primary (scene view) camera
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

        public const int k_CameraRelativeRendering = (int)ShaderOptions.CameraRelativeRendering;
        public static int s_CameraRelativeRendering = (int)ShaderOptions.CameraRelativeRendering;
    }
}
