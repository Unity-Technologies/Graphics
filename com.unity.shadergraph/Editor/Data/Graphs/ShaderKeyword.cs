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
            if (keywordType == KeywordType.Enum)
            {
                m_Entries = new List<KeywordEntry>();
                m_Entries.Add(new KeywordEntry(1, "A", "A"));
                m_Entries.Add(new KeywordEntry(2, "B", "B"));
                m_Entries.Add(new KeywordEntry(3, "C", "C"));
            }
        }

        public static ShaderKeyword CreateBuiltInKeyword(KeywordDescriptor descriptor)
        {
            if (descriptor.entries != null)
            {
                for (int i = 0; i < descriptor.entries.Length; i++)
                {
                    if (descriptor.entries[i].id == -1)
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
        private KeywordShaderStage m_KeywordStages = KeywordShaderStage.All;

        public KeywordShaderStage keywordStages
        {
            get => m_KeywordStages;
            set => m_KeywordStages = value;
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
        private bool m_IsEditable = true;       // this serializes !isBuiltIn

        public bool isBuiltIn
        {
            get => !m_IsEditable;
            set => m_IsEditable = !value;
        }

        internal override bool isExposable => !isBuiltIn && (keywordDefinition != KeywordDefinition.Predefined);

        internal override bool isRenamable => !isBuiltIn;

        internal override ConcreteSlotValueType concreteShaderValueType => keywordType.ToConcreteSlotValueType();

        public override string GetOldDefaultReferenceName()
        {
            // _ON suffix is required for exposing Boolean type to Material
            var suffix = string.Empty;
            if (keywordType == KeywordType.Boolean)
            {
                suffix = "_ON";
            }

            return $"{keywordType.ToString()}_{objectId}{suffix}".ToUpper();
        }

        public void AppendPropertyBlockStrings(ShaderStringBuilder builder)
        {
            if (isExposed)
            {
                switch (keywordType)
                {
                    case KeywordType.Enum:
                        string enumTagString = $"[KeywordEnum({string.Join(", ", entries.Select(x => x.displayName))})]";
                        builder.AppendLine($"{enumTagString}{referenceName}(\"{displayName}\", Float) = {value}");
                        break;
                    case KeywordType.Boolean:
                        if (referenceName.EndsWith("_ON"))
                            builder.AppendLine($"[Toggle]{referenceName.Remove(referenceName.Length - 3, 3)}(\"{displayName}\", Float) = {value}");
                        else
                            builder.AppendLine($"[Toggle({referenceName})]{referenceName}(\"{displayName}\", Float) = {value}");
                        break;
                    default:
                        break;
                }
            }
        }

        public void AppendKeywordDeclarationStrings(ShaderStringBuilder builder)
        {
            if (keywordDefinition != KeywordDefinition.Predefined)
            {
                if (keywordType == KeywordType.Boolean)
                    KeywordUtil.GenerateBooleanKeywordPragmaStrings(referenceName, keywordDefinition, keywordScope, keywordStages, str => builder.AppendLine(str));
                else
                    KeywordUtil.GenerateEnumKeywordPragmaStrings(referenceName, keywordDefinition, keywordScope, keywordStages, entries, str => builder.AppendLine(str));
            }
        }

        public string GetKeywordPreviewDeclarationString()
        {
            switch (keywordType)
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
                value = value,
                isBuiltIn = isBuiltIn,
                keywordType = keywordType,
                keywordDefinition = keywordDefinition,
                keywordScope = keywordScope,
                entries = entries,
                keywordStages = keywordStages
            };
        }

        public override int latestVersion => 1;
        public override void OnAfterDeserialize(string json)
        {
            if (sgVersion == 0)
            {
                // we now allow keywords to control whether they are exposed (for Material control) or not.
                // old exposable keywords set their exposed state to maintain previous behavior
                // (where bool keywords only showed up in the material when ending in "_ON")
                if (isExposable)
                {
                    if (m_KeywordType == KeywordType.Boolean)
                        generatePropertyBlock = referenceName.EndsWith("_ON");
                    else // KeywordType.Enum
                        generatePropertyBlock = true;
                }
                ChangeVersion(1);
            }
        }
    }
}
