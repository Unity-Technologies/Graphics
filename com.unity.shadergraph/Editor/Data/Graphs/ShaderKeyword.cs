using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct ShaderKeywordEntry
    {
        public string displayName;
        public string referenceName;

        public ShaderKeywordEntry(string displayName, string referenceName)
        {
            this.displayName = displayName;
            this.referenceName = referenceName;
        }
    }

    enum ShaderKeywordType { ShaderFeature, MultiCompile, Predefined }
    enum ShaderKeywordScope { Local, Global }

    [Serializable]
    class ShaderKeyword : ShaderInput
    {
        public ShaderKeyword()
        {
            m_Entries = new List<ShaderKeywordEntry>();
        }

        [SerializeField]
        private string m_DefaultReferenceName;

        [SerializeField]
        private string m_OverrideReferenceName;

        public virtual string referenceName
        {
            get
            {
                if (string.IsNullOrEmpty(overrideReferenceName))
                {
                    if (string.IsNullOrEmpty(m_DefaultReferenceName))
                        m_DefaultReferenceName = $"{concreteShaderValueType}_{GuidEncoder.Encode(guid)}";
                    return m_DefaultReferenceName;
                }
                return overrideReferenceName;
            }
        }

        public string overrideReferenceName
        {
            get => m_OverrideReferenceName;
            set => m_OverrideReferenceName = value;
        }

        [SerializeField]
        private bool m_IsEditable = true;

        public bool isEditable
        {
            get => m_IsEditable;
            set => m_IsEditable = value;
        }

        public bool isExposable => keywordType == ShaderKeywordType.ShaderFeature;

        [SerializeField]
        private bool m_GeneratePropertyBlock = false;

        public bool generatePropertyBlock
        {
            get => m_GeneratePropertyBlock;
            set => m_GeneratePropertyBlock = value;
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
        private List<ShaderKeywordEntry> m_Entries;

        public List<ShaderKeywordEntry> entries
        {
            get => m_Entries;
            set => m_Entries = value;
        }

        public override ShaderInput Copy()
        {
            return new ShaderKeyword()
            {
                displayName = displayName,
                isEditable = isEditable,
                generatePropertyBlock = generatePropertyBlock,
                keywordType = keywordType,
                keywordScope = keywordScope,
                entries = entries
            };
        }
    }
}
