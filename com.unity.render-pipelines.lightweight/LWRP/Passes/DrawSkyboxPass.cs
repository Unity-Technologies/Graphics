namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DrawSkyboxPass : ScriptableRenderPass
    {
        public override void Execute(ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            context.DrawSkybox(renderingData.cameraData.camera);
        }
    }
}