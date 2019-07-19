using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    internal struct ShaderKeywordEntry
    {
        public int id;
        public string displayName;
        public string referenceName;

        public ShaderKeywordEntry(int id, string displayName, string referenceName)
        {
            this.id = id;
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

        public ShaderKeyword(ShaderKeywordType keywordType, bool isExposable = true)
        {
            this.displayName = keywordType.ToString();
            this.keywordType = keywordType;
            m_IsExposable = isExposable;
            
            if(keywordType == ShaderKeywordType.Enum)
            {
                m_Entries = new List<ShaderKeywordEntry>();
                m_Entries.Add(new ShaderKeywordEntry(1, "A", "A"));
                m_Entries.Add(new ShaderKeywordEntry(2, "B", "B"));
                m_Entries.Add(new ShaderKeywordEntry(3, "C", "C"));
            }
        }

        [SerializeField]
        private bool m_IsEditable = true;

        public bool isEditable
        {
            get => m_IsEditable;
            set => m_IsEditable = value;
        }

        [SerializeField]
        private bool m_IsExposable;
        
        public override bool isExposable => m_IsExposable;

        public override ConcreteSlotValueType concreteShaderValueType => keywordType.ToConcreteSlotValueType();
        public override bool isRenamable => isEditable;

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

        [SerializeField]
        private int m_Value;

        public int value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public string GetPropertyBlockString()
        {
            switch(keywordType)
            {
                case ShaderKeywordType.Enum:
                    string enumTagString = $"[KeywordEnum({string.Join(", ", entries.Select(x => x.displayName))})]";
                    return $"{enumTagString}{referenceName}(\"{displayName}\", Float) = {value}";
                case ShaderKeywordType.Boolean:
                    return $"[Toggle]{referenceName}(\"{displayName}\", Float) = {value}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string GetKeywordDeclarationString()
        {
            if(keywordDefinition == ShaderKeywordDefinition.Predefined)
                return string.Empty;

            string scopeString = keywordScope == ShaderKeywordScope.Local ? "_local" : string.Empty;
            string definitionString = $"{keywordDefinition.ToKeywordValueString()}{scopeString}";

            switch(keywordType)
            {
                case ShaderKeywordType.Boolean:
                    return $"#pragma {definitionString} _ {referenceName}_ON";
                case ShaderKeywordType.Enum:
                    var enumEntryDefinitions = entries.Select(x => $"{referenceName}_{x.referenceName}");
                    string enumEntriesString = string.Join(" ", enumEntryDefinitions);
                    return $"#pragma {definitionString} {enumEntriesString}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string GetKeywordPreviewDeclarationString()
        {
            switch(keywordType)
            {
                case ShaderKeywordType.Boolean:
                    return value == 1 ? $"#define {referenceName}_ON" : string.Empty;
                case ShaderKeywordType.Enum:
                    return $"#define {referenceName}_{entries[value].referenceName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ShaderInput Copy()
        {
            return new ShaderKeyword()
            {
                displayName = displayName,
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
