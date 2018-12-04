namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class IRendererData : ScriptableObject
    {
        public abstract IRendererSetup Create();
    }
}

