using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Unity.RenderPipelines.Core.Runtime.Shared
{
    internal class InternalRenderGraphValidation
    {  
        internal static void SetAdditionalValidationLayer(RenderGraph renderGraph, RenderGraphValidationLayer layer)
        {
            renderGraph.validationLayer = layer;
        }
    }
}
