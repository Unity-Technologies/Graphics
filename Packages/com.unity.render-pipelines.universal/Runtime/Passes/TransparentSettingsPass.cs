using System;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Applies relevant settings before rendering transparent objects
    /// </summary>

    internal class TransparentSettingsPass : ScriptableRenderPass
    {
        bool m_shouldReceiveShadows;

        public TransparentSettingsPass(RenderPassEvent evt, bool shadowReceiveSupported)
        {
            profilingSampler = new ProfilingSampler("Set Transparent Parameters");
            renderPassEvent = evt;
            m_shouldReceiveShadows = shadowReceiveSupported;
        }

        public bool Setup()
        {
            // Currently we only need to enqueue this pass when the user
            // doesn't want transparent objects to receive shadows
            return !m_shouldReceiveShadows;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Get a command buffer...
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(cmd), m_shouldReceiveShadows);
            }
        }

        public static void ExecutePass(RasterCommandBuffer cmd, bool shouldReceiveShadows)
        {            
            // This pass is only used when transparent objects should not
            // receive shadows using the setting on the URP Renderer.
            MainLightShadowCasterPass.SetEmptyMainLightShadowParams(cmd);
            AdditionalLightsShadowCasterPass.SetEmptyAdditionalLightShadowParams(cmd, AdditionalLightsShadowCasterPass.s_EmptyAdditionalLightIndexToShadowParams);            
        }
    }
}
