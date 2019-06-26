using UnityEditor.Rendering;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Draw the skybox into the given color buffer using the given depth buffer for depth testing.
    ///
    /// This pass renders the standard Unity skybox.
    /// </summary>
    internal class DrawSkyboxPass : ScriptableRenderPass
    {
        public DrawSkyboxPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var sceneOverrideMode = DebugDisplaySettings.Instance.renderingSettings.sceneOverrides;
            if (sceneOverrideMode == SceneOverrides.Overdraw)
            {
                Material skyboxMaterial = RenderSettings.skybox;
                RenderSettings.skybox = RenderingUtils.replacementMaterial;
                context.Submit();
                context.DrawSkybox(renderingData.cameraData.camera);
                context.Submit();
                RenderSettings.skybox = skyboxMaterial;
            }
            else
            {
                context.DrawSkybox(renderingData.cameraData.camera);
            }
            
        }
    }
}
