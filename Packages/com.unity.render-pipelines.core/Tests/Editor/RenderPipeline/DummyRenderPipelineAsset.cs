using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public class DummyRenderPipelineAsset : RenderPipelineAsset<DummyRenderPipeline>
    {
        protected override RenderPipeline CreatePipeline()
        {
            throw new System.NotImplementedException();
        }
    }
}
