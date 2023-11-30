using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    class SecondCustomRenderPipelineAsset : RenderPipelineAsset<SecondCustomRenderPipeline>
    {
        protected override RenderPipeline CreatePipeline()
            => new SecondCustomRenderPipeline();
    }

    class SecondCustomRenderPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
        }
    }
}
