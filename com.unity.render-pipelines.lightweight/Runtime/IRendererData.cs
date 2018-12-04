namespace UnityEngine.Experimental.Rendering.LWRP
{
    public abstract class IRendererData : ScriptableObject
    {
        public abstract IRendererSetup Create();
    }
}

