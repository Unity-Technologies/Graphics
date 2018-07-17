using System;
using UnityEngine.Experimental.Rendering;

namespace RenderGraph
{
    public interface IRenderNode
    {
        // Maybe Declare or DeclareResources
        void Setup(ref ResourceBuilder builder);
        // Maybe Record or RecordCommands
        void Run(ref ResourceContext r, ScriptableRenderContext context);
    }
}
