using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// A base class that can be inherited by render graph implementers. Currently this is only used by URP. In the future this will allow shared SRP rendering features between HDRP and URP through render graph.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public interface IRenderGraphRecorder
    {
        /// <summary>
        /// This function is called during render graph recording and allows the implementing class to register the relevant passes and resources to the passed in render graph.
        /// To access information about the current execution context (current camera, ...) the ContextContainer can be queried. See the render pipeline documentation
        /// for more information on the types that are available in the context container. (The exact types available may differ based on the currently active render pipeline).
        /// This is where custom rendering occurs. Specific details are left to the implementation.
        /// </summary>
        /// <param name="renderGraph">The graph to register resources and passes with.</param>
        /// <param name="frameData">A ContextContainer that allows querying information about the current execution context (.e.g. RenderPipeline camera info).</param>
        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData);
    }
}
