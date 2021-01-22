namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct KeywordDescriptor
    {
        public string displayName;
        public string referenceName;
        public KeywordType type;
        public KeywordDefinition definition;
        public KeywordScope scope;
        public KeywordShaderStage stages;
        public int value;
        public KeywordEntry[] entries;
    }
}
