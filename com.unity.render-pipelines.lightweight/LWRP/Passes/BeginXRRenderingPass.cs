namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class BeginXRRenderingPass : ScriptableRenderPass
    {
        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            context.StartMultiEye(camera);
        }
    }
}