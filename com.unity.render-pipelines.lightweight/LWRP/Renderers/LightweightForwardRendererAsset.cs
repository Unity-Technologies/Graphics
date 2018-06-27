namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CreateAssetMenu()]
    public class LightweightForwardRendererAsset : IScriptableRendererAsset
    {
        public override ScriptableRenderer Create(LightweightPipelineAsset asset)
        {
            return new LightweightForwardRenderer(asset);
        }
    }
}
