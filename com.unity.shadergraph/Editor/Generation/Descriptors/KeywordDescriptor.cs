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

        public void AppendKeywordDeclarationStrings(ShaderStringBuilder builder)
        {
            if (definition != KeywordDefinition.Predefined)
            {
                if (type == KeywordType.Boolean)
                    KeywordUtil.GenerateBooleanKeywordPragmaStrings(referenceName, definition, scope, stages, str => builder.AppendLine(str));
                else
                    KeywordUtil.GenerateEnumKeywordPragmaStrings(referenceName, definition, scope, stages, entries, str => builder.AppendLine(str));
            }
        }
    }
}
