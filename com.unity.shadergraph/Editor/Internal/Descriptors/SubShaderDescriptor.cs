namespace UnityEditor.ShaderGraph.Internal
{
    public struct SubShaderDescriptor
    {
        public string pipelineTag;
        public string renderQueueOverride;
        public string renderTypeOverride;
        public bool generatesPreview;
        public PassCollection passes;
        public string customEditorOverride;
        public ShaderPropertyCollection shaderProperties;
    }
}
