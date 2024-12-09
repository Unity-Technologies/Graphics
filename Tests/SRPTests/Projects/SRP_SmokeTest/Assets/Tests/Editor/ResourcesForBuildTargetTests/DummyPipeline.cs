using System.Collections.Generic;

namespace UnityEngine.Rendering.DummyPipeline
{
    public class DummyPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras) { }
    }
}
