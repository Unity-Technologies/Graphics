using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    class CustomRenderPipelineAsset : RenderPipelineAsset<CustomRenderPipeline>
    {
        protected override RenderPipeline CreatePipeline()
            => new CustomRenderPipeline();
    }

    class CustomRenderPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
        }
    }
}
