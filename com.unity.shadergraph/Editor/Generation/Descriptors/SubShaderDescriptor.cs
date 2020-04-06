namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct SubShaderDescriptor
    {
        public string pipelineTag;
        public string renderQueueOverride;
        public string renderTypeOverride;
        public bool generatesPreview;
        public PassCollection passes;
    }
}
