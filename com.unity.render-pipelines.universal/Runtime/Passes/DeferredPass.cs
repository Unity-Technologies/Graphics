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

        public DeferredPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, DeferredLights deferredLights)
        {
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
        }

        // ScriptableRenderPass
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
        {
            // TODO: Cannot currently bind depth texture as read-only!
            ConfigureTarget(m_DeferredLights.m_LightingTexture.Identifier(), m_DeferredLights.m_DepthTexture.Identifier());

            // m_LightingTexture is not initialized in the GBuffer, so we must clear it before accumulating lighting results into it.
            // If m_LightingTexture is part of the gbuffer pass, Clear will not be necessary here anymore, and can be done in GBufferPass.FrameCleanup ...
            ConfigureClear(ClearFlag.Color, Color.black);
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
