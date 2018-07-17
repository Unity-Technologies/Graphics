using UnityEngine.Experimental.Rendering;

namespace RenderGraph
{
    public struct RenderGraphContext
    {
        public T AddNode<T>(T node) where T : struct, IRenderNode
        {
            return default(T);
        }

        public T AddModule<T>(T module) where T : struct, IRenderModule
        {
            return default(T);
        }

        public void Run(ScriptableRenderContext renderContext)
        {

        }
    }
}
