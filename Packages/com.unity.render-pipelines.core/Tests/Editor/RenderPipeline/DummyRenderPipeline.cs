using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class DummyRenderPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            throw new System.NotImplementedException();
        }
    }
}
