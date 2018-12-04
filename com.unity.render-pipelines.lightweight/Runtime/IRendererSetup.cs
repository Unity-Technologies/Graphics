using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public abstract class IRendererSetup
    {
        public abstract void Setup(ScriptableRenderer renderer, ref RenderingData renderingData);
    }
}
