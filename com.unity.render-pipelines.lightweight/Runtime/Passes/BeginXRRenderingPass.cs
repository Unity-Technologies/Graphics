namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class BeginXRRenderingPass : ScriptableRenderPass
    {
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            context.StartMultiEye(camera);
        }
    }
}
