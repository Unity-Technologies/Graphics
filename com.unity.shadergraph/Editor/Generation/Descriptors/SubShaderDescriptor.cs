namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct SubShaderDescriptor
    {
        public string pipelineTag;
        public string customTags;
        public string renderType;
        public string renderQueue;
        public bool generatesPreview;
        public PassCollection passes;
    }
}
