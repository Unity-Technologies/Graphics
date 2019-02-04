namespace UnityEngine.Experimental.Rendering.LWRP
{
    public abstract class IRendererData : ScriptableObject
    {
        public abstract IRendererSetup Create();
        public virtual Material default2DMaterial { get => null; }
    }
}

