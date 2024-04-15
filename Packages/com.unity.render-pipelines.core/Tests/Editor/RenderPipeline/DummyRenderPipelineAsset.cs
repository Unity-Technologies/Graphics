using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class DummyRenderPipelineAsset : RenderPipelineAsset<DummyRenderPipeline>
    {
        protected override RenderPipeline CreatePipeline()
        {
            throw new System.NotImplementedException();
        }
    }
}
