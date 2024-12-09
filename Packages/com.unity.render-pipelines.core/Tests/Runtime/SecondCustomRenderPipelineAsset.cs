using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Tests
{
    class SecondCustomRenderPipelineAsset : RenderPipelineAsset<SecondCustomRenderPipeline>
    {
        protected override RenderPipeline CreatePipeline()
            => new SecondCustomRenderPipeline();
    }

    class SecondCustomRenderPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
        }
    }
}
