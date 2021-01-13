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
        public int value;
        public KeywordEntry[] entries;
    }
}
