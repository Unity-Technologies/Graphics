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
            RasterCommandBuffer rasterCommandBuffer = CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer);
            using (new ProfilingScope(rasterCommandBuffer, profilingSampler))
            {
                ExecutePass(rasterCommandBuffer);
            }
        }

        public static void ExecutePass(RasterCommandBuffer rasterCommandBuffer)
        {
            // -----------------------------------------------------------
            // This pass is only used when transparent objects should not
            // receive shadows using the setting on the URP Renderer.
            // This is controlled in the public bool Setup() function above.
            // -----------------------------------------------------------

            MainLightShadowCasterPass.SetShadowParamsForEmptyShadowmap(rasterCommandBuffer);
            AdditionalLightsShadowCasterPass.SetShadowParamsForEmptyShadowmap(rasterCommandBuffer);
        }
    }
}
