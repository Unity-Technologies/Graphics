using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class ShaderKeyword : ShaderInput
    {
        public ShaderKeyword()
        {
        }

        public ShaderKeyword(ShaderKeywordType keywordType)
        {
            this.displayName = keywordType.ToString();
            this.keywordType = keywordType;
            
            // Add sensible default entries for Enum type
            if(keywordType == ShaderKeywordType.Enum)
            {
                m_Entries = new List<ShaderKeywordEntry>();
                m_Entries.Add(new ShaderKeywordEntry(1, "A", "A"));
                m_Entries.Add(new ShaderKeywordEntry(2, "B", "B"));
                m_Entries.Add(new ShaderKeywordEntry(3, "C", "C"));
            }
        }

        public static ShaderKeyword BuiltinEnumKeyword(ShaderKeywordDescriptor descriptor)
        {
            return new ShaderKeyword()
            {
                m_IsExposable = false,
                m_IsEditable = false,
                displayName = descriptor.displayName,
                overrideReferenceName = descriptor.referenceName,
                keywordType = ShaderKeywordType.Enum,
                keywordDefinition = descriptor.definition,
                keywordScope = descriptor.scope,
                value = descriptor.value,
                entries = descriptor.entries
            };
        }

        public static ShaderKeyword BuiltinBooleanKeyword(ShaderKeywordDescriptor descriptor)
        {
            return new ShaderKeyword()
            {
                m_IsExposable = false,
                m_IsEditable = false,
                displayName = descriptor.displayName,
                overrideReferenceName = descriptor.referenceName,
                keywordType = ShaderKeywordType.Boolean,
                keywordDefinition = descriptor.definition,
                keywordScope = descriptor.scope,
                value = descriptor.value
            };
        }

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

        public override bool isRenamable => isEditable;

        public override ConcreteSlotValueType concreteShaderValueType => keywordType.ToConcreteSlotValueType();

        public override string GetDefaultReferenceName()
        {
            // _ON suffix is required for exposing Boolean type to Material
            var suffix = string.Empty;
            if(keywordType == ShaderKeywordType.Boolean)
            {
                suffix = "_ON";
            }

            return $"{keywordType.ToString()}_{GuidEncoder.Encode(guid)}{suffix}".ToUpper();
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
            // Predefined keywords do not need to be defined
            if(keywordDefinition == ShaderKeywordDefinition.Predefined)
                return string.Empty;

            // Get definition type using scope
            string scopeString = keywordScope == ShaderKeywordScope.Local ? "_local" : string.Empty;
            string definitionString = $"{keywordDefinition.ToDeclarationString()}{scopeString}";

            switch(keywordType)
            {
                case ShaderKeywordType.Boolean:
                    return $"#pragma {definitionString} _ {referenceName}";
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
                    return value == 1 ? $"#define {referenceName}" : string.Empty;
                case ShaderKeywordType.Enum:
                    return $"#define {referenceName}_{entries[value].referenceName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ShaderInput Copy()
        {
            // Keywords copy reference name
            // This is because keywords are copied between graphs
            // When copying dependent nodes
            return new ShaderKeyword()
            {
                displayName = displayName,
                overrideReferenceName = overrideReferenceName,
                generatePropertyBlock = generatePropertyBlock,
                m_IsExposable = isExposable,
                isEditable = isEditable,
                keywordType = keywordType,
                keywordDefinition = keywordDefinition,
                keywordScope = keywordScope,
                entries = entries
            };
        }
    }
}
