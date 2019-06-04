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
    
    enum ShaderKeywordType { Boolean, Enum }
    enum ShaderKeywordDefinition { ShaderFeature, MultiCompile, Predefined }
    enum ShaderKeywordScope { Local, Global }

    [Serializable]
    class ShaderKeyword : ShaderInput
    {
        public ShaderKeyword()
        {
        }

        public ShaderKeyword(ShaderKeywordType keywordType)
        {
            displayName = keywordType.ToString();
            this.keywordType = keywordType;
            
            if(keywordType == ShaderKeywordType.Enum)
            {
                m_Entries = new List<ShaderKeywordEntry>();
                m_Entries.Add(new ShaderKeywordEntry("A", $"{referenceName}_A"));
                m_Entries.Add(new ShaderKeywordEntry("B", $"{referenceName}_B"));
                m_Entries.Add(new ShaderKeywordEntry("C", $"{referenceName}_C"));
            }
        }

        [SerializeField]
        private bool m_IsEditable = true;

        public bool isEditable
        {
            get => m_IsEditable;
            set => m_IsEditable = value;
        }

        public override ConcreteSlotValueType concreteShaderValueType => keywordType.ToConcreteSlotValueType();
        public override bool isExposable => keywordDefinition == ShaderKeywordDefinition.ShaderFeature;
        public override bool isRenamable => keywordType == ShaderKeywordType.Boolean;

        [SerializeField]
        private ShaderKeywordType m_KeywordType = ShaderKeywordType.Boolean;

        public ShaderKeywordType keywordType
        {
            get => m_KeywordType;
            set => m_KeywordType = value;
        }

        [SerializeField]
        private ShaderKeywordDefinition m_KeywordDefinition = ShaderKeywordDefinition.ShaderFeature;

        public ShaderKeywordDefinition keywordDefinition
        {
            get => m_KeywordDefinition;
            set => m_KeywordDefinition = value;
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
                overrideReferenceName = overrideReferenceName,
                generatePropertyBlock = generatePropertyBlock,
                isEditable = isEditable,
                keywordType = keywordType,
                keywordDefinition = keywordDefinition,
                keywordScope = keywordScope,
                entries = entries
            };
        }
    }
}
