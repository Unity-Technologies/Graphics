using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class TestRenderPipelineAsset : RenderPipelineAsset
{
    public TestRenderPipelineResources renderPipelineResources;

    protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()
    {
        return new TestRenderPipeline(this);
    }

    public override Shader defaultShader
    {
        get
        {
            return renderPipelineResources.defaultShader;
        }
    }

    public override Material defaultMaterial
    {
        get
        {
            return renderPipelineResources.defaultMaterial;
        }
    }
}
