using System;
using UnityEngine.Experimental.Rendering;

namespace RenderGraph
{
    public interface IRenderNode
    {
        void Setup(ref ResourceBuilder builder);
        void Run(ref ResourceManager r, ScriptableRenderContext context);
    }
}
