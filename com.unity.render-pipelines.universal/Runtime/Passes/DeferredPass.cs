using System.Collections.Generic;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Profiling;
using Unity.Collections;

// cleanup code
// listMinDepth and maxDepth should be stored in a different uniform block?
// Point lights stored as vec4
// RelLightIndices should be stored in ushort instead of uint.
// TODO use Unity.Mathematics
// TODO Check if there is a bitarray structure (with dynamic size) available in Unity

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.      
    internal class DeferredPass : ScriptableRenderPass
    {
        DeferredLights m_DeferredLights;

        public DeferredPass(RenderPassEvent evt, DeferredLights deferredLights)
        {
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
        }

        // ScriptableRenderPass
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
        {
            // TODO: Cannot currently bind depth texture as read-only!
            ConfigureTarget(m_DeferredLights.m_GbufferColorAttachments[3], m_DeferredLights.m_DepthTexture);
            m_ColorAttachments.Clear();
//            ConfigureColorAttachment(m_DeferredLights.m_GbufferColorAttachments[3], true, true, false, 0);
           // ConfigureDepthAttachment(m_DeferredLights.m_DepthTexture, true, true);
            ConfigureRenderPassDescriptor(cameraTextureDescripor.width, cameraTextureDescripor.height, cameraTextureDescripor.msaaSamples);
        }

        // ScriptableRenderPass
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_DeferredLights.ExecuteDeferredPass(context, ref renderingData);
        }

        // ScriptableRenderPass
        public override void FrameCleanup(CommandBuffer cmd)
        {
            m_DeferredLights.FrameCleanup(cmd);
        }
    }
}
