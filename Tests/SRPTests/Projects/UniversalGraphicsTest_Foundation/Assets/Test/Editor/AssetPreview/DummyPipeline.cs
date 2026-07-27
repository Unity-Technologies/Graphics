using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Common
{
    public class DummyRenderPipelineAsset : RenderPipelineAsset
    {
        protected override RenderPipeline CreatePipeline()
        {
            return new DummyRenderPipeline();
        }
    }

    public class DummyRenderPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {

        }
    }
}
