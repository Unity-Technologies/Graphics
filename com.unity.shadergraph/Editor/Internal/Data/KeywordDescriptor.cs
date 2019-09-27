using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
    public class ConditionalKeyword : IConditionalShaderString
    {        
        public KeywordDescriptor keyword { get; }
        public FieldCondition[] fieldConditions { get; }
        public string value => keyword.ToDeclarationString();

        public ConditionalKeyword(KeywordDescriptor keyword)
        {
            this.keyword = keyword;
            this.fieldConditions = null;
        }

        public ConditionalKeyword(KeywordDescriptor keyword, FieldCondition fieldCondition)
        {
            this.keyword = keyword;
            this.fieldConditions = new FieldCondition[] { fieldCondition };
        }

        public ConditionalKeyword(KeywordDescriptor keyword, FieldCondition[] fieldConditions)
        {
            this.keyword = keyword;
            this.fieldConditions = fieldConditions;
        }
    }

    public class ConditionalDefine : IConditionalShaderString
    {        
        public KeywordDescriptor keyword { get; }
        public FieldCondition[] fieldConditions { get; }
        public string value => keyword.ToDefineString(index);
        public int index { get; }

        public ConditionalDefine(KeywordDescriptor keyword, int index)
        {
            this.keyword = keyword;
            this.fieldConditions = null;
            this.index = index;
        }

        public ConditionalDefine(KeywordDescriptor keyword, FieldCondition fieldCondition, int index)
        {
            this.keyword = keyword;
            this.fieldConditions = new FieldCondition[] { fieldCondition };
            this.index = index;
        }

        public ConditionalDefine(KeywordDescriptor keyword, FieldCondition[] fieldConditions, int index)
        {
            this.keyword = keyword;
            this.fieldConditions = fieldConditions;
            this.index = index;
        }
    }

    public struct KeywordDescriptor
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
