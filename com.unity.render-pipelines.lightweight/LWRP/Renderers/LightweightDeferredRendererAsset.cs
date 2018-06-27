namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LightweightDeferredRendererAsset : IScriptableRendererAsset
    {
        public override ScriptableRenderer Create(LightweightPipelineAsset asset)
        {
            return new LightweightDeferredRenderer(asset);
        }
    }
}
