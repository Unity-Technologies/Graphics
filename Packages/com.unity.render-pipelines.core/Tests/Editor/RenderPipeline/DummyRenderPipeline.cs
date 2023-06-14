using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public class DummyRenderPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            throw new System.NotImplementedException();
        }
    }
}
