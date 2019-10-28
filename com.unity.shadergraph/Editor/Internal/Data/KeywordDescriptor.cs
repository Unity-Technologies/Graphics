using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
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

    public class KeywordCollection : IEnumerable<ConditionalKeyword>
    {
        private readonly List<ConditionalKeyword> m_Keywords;

        public KeywordCollection()
        {
            m_Keywords = new List<ConditionalKeyword>();
        }

        public void Add(KeywordDescriptor keyword)
        {
            m_Keywords.Add(new ConditionalKeyword(keyword, null));
        }

        public void Add(KeywordDescriptor keyword, FieldCondition fieldCondition)
        {
            m_Keywords.Add(new ConditionalKeyword(keyword, new FieldCondition[]{ fieldCondition }));
        }

        public void Add(KeywordDescriptor keyword, FieldCondition[] fieldConditions)
        {
            m_Keywords.Add(new ConditionalKeyword(keyword, fieldConditions));
        }

        public IEnumerator<ConditionalKeyword> GetEnumerator()
        {
            return m_Keywords.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ConditionalKeyword : IConditionalShaderString
    {        
        public KeywordDescriptor keyword { get; }
        public FieldCondition[] fieldConditions { get; }
        public string value => keyword.ToDeclarationString();

        public ConditionalKeyword(KeywordDescriptor keyword, FieldCondition[] fieldConditions)
        {
            this.keyword = keyword;
            this.fieldConditions = fieldConditions;
        }
    }

    public class DefineCollection : IEnumerable<ConditionalDefine>
    {
        private readonly List<ConditionalDefine> m_Defines;

        public DefineCollection()
        {
            m_Defines = new List<ConditionalDefine>();
        }

        public void Add(KeywordDescriptor keyword, int index)
        {
            m_Defines.Add(new ConditionalDefine(keyword, index, null));
        }

        public void Add(KeywordDescriptor keyword, int index, FieldCondition fieldCondition)
        {
            m_Defines.Add(new ConditionalDefine(keyword, index, new FieldCondition[]{ fieldCondition }));
        }

        public void Add(KeywordDescriptor keyword, int index, FieldCondition[] fieldConditions)
        {
            m_Defines.Add(new ConditionalDefine(keyword, index, fieldConditions));
        }

        public IEnumerator<ConditionalDefine> GetEnumerator()
        {
            return m_Defines.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ConditionalDefine : IConditionalShaderString
    {        
        public KeywordDescriptor keyword { get; }
        public FieldCondition[] fieldConditions { get; }
        public string value => keyword.ToDefineString(index);
        public int index { get; }

        public ConditionalDefine(KeywordDescriptor keyword, int index, FieldCondition[] fieldConditions)
        {
            this.keyword = keyword;
            this.fieldConditions = fieldConditions;
            this.index = index;
        }
    }
}
