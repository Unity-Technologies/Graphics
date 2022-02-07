using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        private RenderGraph m_RenderGraph = new RenderGraph("URPRenderGraph");

        static void RecordRenderGraph()
        {

        }

        static void RecordAndExecuteRenderGraph()
        {
            RecordRenderGraph();
        }
    }
}
