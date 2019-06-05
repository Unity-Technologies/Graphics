using System;
using System.Linq;
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
                m_Entries.Add(new ShaderKeywordEntry("A", $"A"));
                m_Entries.Add(new ShaderKeywordEntry("B", $"B"));
                m_Entries.Add(new ShaderKeywordEntry("C", $"C"));
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
        public override bool isRenamable => true;

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
                    return $"#pragma {definitionString} _ {referenceName}";
                case ShaderKeywordType.Enum:
                    string[] enumEntryDefinitions = new string[entries.Count];
                    for(int i = 0; i < enumEntryDefinitions.Length; i++)
                        enumEntryDefinitions[i] = $"{referenceName}_{entries[i].referenceName}";
                    string enumEntriesString = string.Join(" ", enumEntryDefinitions);
                    // string enumEntriesString = $"{string.Join(" ", entries.Select(x => x.referenceName))}";
                    return $"#pragma {definitionString} {enumEntriesString}";
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
