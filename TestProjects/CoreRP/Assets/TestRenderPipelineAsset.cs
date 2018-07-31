using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class TestRenderPipelineAsset : RenderPipelineAsset
{
    public TestRenderPipelineResources renderPipelineResources;
    public bool useNewCulling = false;

    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new TestRenderPipeline(this);
    }

    public override Shader GetDefaultShader()
    {
        return renderPipelineResources.defaultShader;
    }

    public override Material GetDefaultMaterial()
    {
        return renderPipelineResources.defaultMaterial;
    }
}
