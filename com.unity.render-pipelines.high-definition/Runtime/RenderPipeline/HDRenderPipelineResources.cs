namespace UnityEngine.Rendering.HighDefinition
{
    abstract class HDRenderPipelineResources : RenderPipelineResources
    {
        const string m_packagePath = "Packages/com.unity.render-pipelines.high-definition/";
        protected override string packagePath => m_packagePath;
    }
}
