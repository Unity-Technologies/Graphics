using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    struct ShaderKeywordDescriptor
    {
        public string displayName;
        public string referenceName;
        public ShaderKeywordDefinition definition;
        public ShaderKeywordScope scope;
        public int value;
        public List<ShaderKeywordEntry> entries;
    }
}
