namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class EndXRRenderingPass : ScriptableRenderPass
    {
        public override void Execute(LightweightForwardRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            context.StopMultiEye(camera);
            context.StereoEndRender(camera);
        }
    }
}