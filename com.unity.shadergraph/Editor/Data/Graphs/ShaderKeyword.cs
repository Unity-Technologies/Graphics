using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class ShaderKeyword : ShaderInput
    {
        public const string kVariantLimitWarning = "Graph is generating too many variants. Either delete Keywords, reduce Keyword variants or increase the Shader Variant Limit in Preferences > Shader Graph.";

        public ShaderKeyword()
        {
        }

        public ShaderKeyword(KeywordType keywordType)
        {
            this.displayName = keywordType.ToString();
            this.keywordType = keywordType;

            // Add sensible default entries for Enum type
            if(keywordType == KeywordType.Enum)
            {
                m_Entries = new List<KeywordEntry>();
                m_Entries.Add(new KeywordEntry(1, "A", "A"));
                m_Entries.Add(new KeywordEntry(2, "B", "B"));
                m_Entries.Add(new KeywordEntry(3, "C", "C"));
            }
        }

        public static ShaderKeyword CreateBuiltInKeyword(KeywordDescriptor descriptor)
        {
            if(descriptor.entries != null)
            {
                for(int i = 0; i < descriptor.entries.Length; i++)
                {
                    if(descriptor.entries[i].id == -1)
                        descriptor.entries[i].id = i + 1;
                }
            }

            return new ShaderKeyword()
            {
                isBuiltIn = true,
                displayName = descriptor.displayName,
                overrideReferenceName = descriptor.referenceName,
                keywordType = descriptor.type,
                keywordDefinition = descriptor.definition,
                keywordScope = descriptor.scope,
                value = descriptor.value,
                entries = descriptor.entries.ToList(),
            };
        }

        [SerializeField]
        private KeywordType m_KeywordType = KeywordType.Boolean;

        public KeywordType keywordType
        {
            get => m_KeywordType;
            set => m_KeywordType = value;
        }

        [SerializeField]
        private KeywordDefinition m_KeywordDefinition = KeywordDefinition.ShaderFeature;

        public KeywordDefinition keywordDefinition
        {
            get => m_KeywordDefinition;
            set => m_KeywordDefinition = value;
        }

        [SerializeField]
        private KeywordScope m_KeywordScope = KeywordScope.Local;

        public KeywordScope keywordScope
        {
            get => m_KeywordScope;
            set => m_KeywordScope = value;
        }

        [SerializeField]
        private List<KeywordEntry> m_Entries;

        public List<KeywordEntry> entries
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
        private bool m_IsEditable = true; // Only Built-In Keywords are uneditable

        public bool isBuiltIn
        {
            get => !m_IsEditable;
            set => m_IsEditable = !value;
        }

        internal override bool isExposable => !isBuiltIn
            && (keywordType == KeywordType.Enum || referenceName.EndsWith("_ON"));

        internal override bool isRenamable => !isBuiltIn;

        internal override ConcreteSlotValueType concreteShaderValueType => keywordType.ToConcreteSlotValueType();

        public override string GetDefaultReferenceName()
        {
            // _ON suffix is required for exposing Boolean type to Material
            var suffix = string.Empty;
            if(keywordType == KeywordType.Boolean)
            {
                suffix = "_ON";
            }

            return $"{keywordType.ToString()}_{objectId}{suffix}".ToUpper();
        }

        public string GetPropertyBlockString()
        {
            switch(keywordType)
            {
                case KeywordType.Enum:
                    string enumTagString = $"[KeywordEnum({string.Join(", ", entries.Select(x => x.displayName))})]";
                    return $"{enumTagString}{referenceName}(\"{displayName}\", Float) = {value}";
                case KeywordType.Boolean:
                    // Reference name must be appended with _ON but must be removed when generating block
                    if(referenceName.EndsWith("_ON"))
                        return $"[Toggle]{referenceName.Remove(referenceName.Length - 3, 3)}(\"{displayName}\", Float) = {value}";
                    else
                        return string.Empty;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string GetKeywordDeclarationString()
        {
            // Predefined keywords do not need to be defined
            if(keywordDefinition == KeywordDefinition.Predefined)
                return string.Empty;

            // Get definition type using scope
            string scopeString = keywordScope == KeywordScope.Local ? "_local" : string.Empty;
            string definitionString = $"{keywordDefinition.ToDeclarationString()}{scopeString}";

            switch(keywordType)
            {
                case KeywordType.Boolean:
                    return $"#pragma {definitionString} _ {referenceName}";
                case KeywordType.Enum:
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
                case KeywordType.Boolean:
                    return value == 1 ? $"#define {referenceName}" : string.Empty;
                case KeywordType.Enum:
                    return $"#define {referenceName}_{entries[value].referenceName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal override ShaderInput Copy()
        {
            // Keywords copy reference name
            // This is because keywords are copied between graphs
            // When copying dependent nodes
            return new ShaderKeyword()
            {
                displayName = displayName,
                overrideReferenceName = overrideReferenceName,
                generatePropertyBlock = generatePropertyBlock,
                isBuiltIn = isBuiltIn,
                keywordType = keywordType,
                keywordDefinition = keywordDefinition,
                keywordScope = keywordScope,
                entries = entries,
                value = value,
            };
        }
    }
}
