namespace UnityEngine.Rendering.HighDefinition
{
    abstract class HDRenderPipelineResources : RenderPipelineResources
    {
        protected override string packagePath => HDUtils.GetHDRenderPipelinePath();
    }
}
