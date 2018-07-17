namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class SetupForwardRenderingPass : ScriptableRenderPass
    {
        public SetupForwardRenderingPass(LightweightForwardRenderer renderer) : base(renderer)
        {}

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            // SetupCameraProperties does the following:
            // Setup Camera RenderTarget and Viewport
            // VR Camera Setup and SINGLE_PASS_STEREO props
            // Setup camera view, proj and their inv matrices.
            // Setup properties: _WorldSpaceCameraPos, _ProjectionParams, _ScreenParams, _ZBufferParams, unity_OrthoParams
            // Setup camera world clip planes props
            // setup HDR keyword
            // Setup global time properties (_Time, _SinTime, _CosTime)
            context.SetupCameraProperties(renderingData.cameraData.camera, renderingData.cameraData.isStereoEnabled);
        }
    }
}
