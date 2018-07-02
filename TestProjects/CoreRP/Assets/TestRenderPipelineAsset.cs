using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class TestRenderPipelineAsset : RenderPipelineAsset
{
    public bool m_UseNewCulling = false;

    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new TestRenderPipeline(this);
    }
}
