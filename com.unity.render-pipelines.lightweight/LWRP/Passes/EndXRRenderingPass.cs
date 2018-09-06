namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class EndXRRenderingPass : ScriptableRenderPass
    {
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            context.StopMultiEye(camera);
            context.StereoEndRender(camera);
        }
    }
}
