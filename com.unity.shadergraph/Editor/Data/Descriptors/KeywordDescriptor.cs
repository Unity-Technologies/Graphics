using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    struct KeywordDescriptor
    {
        public string displayName;
        public string referenceName;
        public KeywordType type;
        public KeywordDefinition definition;
        public KeywordScope scope;
        public int value;
        public List<KeywordEntry> entries;
    }
}
