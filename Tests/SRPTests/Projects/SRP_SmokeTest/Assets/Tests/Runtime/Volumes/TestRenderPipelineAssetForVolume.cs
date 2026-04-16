using System.Collections.Generic;

namespace UnityEngine.Rendering.Tests
{
    public class TestRenderPipelineAssetForVolume : RenderPipelineAsset<TestRenderPipeline>
    {
        protected override RenderPipeline CreatePipeline()
        {
            return new TestRenderPipeline();
        }
    }

    public class TestRenderPipeline : RenderPipeline { }
}
