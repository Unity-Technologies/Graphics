namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class IScriptableRendererAsset : ScriptableObject
    {
        public abstract ScriptableRenderer Create(LightweightPipelineAsset asset);
    }
}


