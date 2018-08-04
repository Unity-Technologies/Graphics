namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DrawSkyboxPass : ScriptableRenderPass
    {
        public override void Execute(LightweightForwardRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            context.DrawSkybox(renderingData.cameraData.camera);
        }
    }
}