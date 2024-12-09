using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Tests
{
    class CustomRenderPipelineAsset : RenderPipelineAsset<CustomRenderPipeline>
    {
        protected override RenderPipeline CreatePipeline()
            => new CustomRenderPipeline();
    }

    class CustomRenderPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
        }
    }
}
