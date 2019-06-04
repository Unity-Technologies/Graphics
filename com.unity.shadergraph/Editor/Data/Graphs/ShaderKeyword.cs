using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    enum ShaderKeywordType { ShaderFeature, MultiCompile, None }
    enum ShaderKeywordScope { Local, Global }

    [Serializable]
    class ShaderKeyword : ShaderInput
    {
        public ShaderKeyword()
        {
            m_Entries = new List<KeyValuePair<string, string>>();
        }

        public override ConcreteSlotValueType concreteShaderValueType => ConcreteSlotValueType.Vector1;

        [SerializeField]
        private ShaderKeywordType m_KeywordType = ShaderKeywordType.ShaderFeature;

        public ShaderKeywordType keywordType
        {
            get => m_KeywordType;
            set => m_KeywordType = value;
        }

        [SerializeField]
        private ShaderKeywordScope m_KeywordScope = ShaderKeywordScope.Local;

        public ShaderKeywordScope keywordScope
        {
            get => m_KeywordScope;
            set => m_KeywordScope = value;
        }

        [SerializeField]
        private int m_Value;

        public int value
        {
            get => m_Value;
            set => m_Value = value;
        }

        [SerializeField]
        private List<KeyValuePair<string, string>> m_Entries;

        public List<KeyValuePair<string, string>> entries
        {
            get => m_Entries;
            set => m_Entries = value;
        }

        public override ShaderInput Copy()
        {
            return new ShaderKeyword()
            {
                displayName = displayName,
                value = value
            };
        }
    }
}
