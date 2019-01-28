using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public abstract class RendererSetup
    {
        public abstract void Setup(ScriptableRenderer renderer, ref RenderingData renderingData);
    }
}
