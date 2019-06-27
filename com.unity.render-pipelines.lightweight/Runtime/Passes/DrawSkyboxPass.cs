using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Draw the skybox into the given color buffer using the given depth buffer for depth testing.
    ///
    /// This pass renders the standard Unity skybox.
    /// </summary>
    internal class DrawSkyboxPass : ScriptableRenderPass
    {
        PhysicalSky m_PhysicalSky;

        public DrawSkyboxPass(RenderPassEvent evt, PhysicalSky physicalSky)
        {
            renderPassEvent = evt;
            m_PhysicalSky = physicalSky;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_PhysicalSky != null && m_PhysicalSky.IsEnabled())
                m_PhysicalSky.DrawSkybox(context, renderingData.cameraData.camera);
            else
                context.DrawSkybox(renderingData.cameraData.camera);
        }
    }
}
