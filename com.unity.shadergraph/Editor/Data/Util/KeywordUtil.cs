using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class BuiltinKeyword
    {
        [BuiltinKeyword]
        public static KeywordDescriptor QualityKeyword()
        {
            return new KeywordDescriptor()
            {
                displayName = "Material Quality",
                referenceName = "MATERIAL_QUALITY",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
                value = 0,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry("High", "HIGH"),
                    new KeywordEntry("Medium", "MEDIUM"),
                    new KeywordEntry("Low", "LOW"),
                },
                stages = KeywordShaderStage.All,
            };
        }
    }

    static class KeywordUtil
    {
        public static IEnumerable<KeywordDescriptor> GetBuiltinKeywordDescriptors() =>
            TypeCache.GetMethodsWithAttribute<BuiltinKeywordAttribute>()
                .Where(method => method.IsStatic && method.ReturnType == typeof(KeywordDescriptor))
                .Select(method =>
                    (KeywordDescriptor)method.Invoke(null, new object[0] { }));

        public static ConcreteSlotValueType ToConcreteSlotValueType(this KeywordType keywordType)
        {
            switch (keywordType)
            {
                case KeywordType.Boolean:
                    return ConcreteSlotValueType.Boolean;
                case KeywordType.Enum:
                    return ConcreteSlotValueType.Vector1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string ToDeclarationSuffix(this KeywordScope scope)
        {
            switch (scope)
            {
                case KeywordScope.Local:
                    return "_local";
                default:
                    return string.Empty;
            }
        }

        public static string ToDeclarationString(this KeywordDefinition keywordDefinition)
        {
            switch (keywordDefinition)
            {
                case KeywordDefinition.MultiCompile:
                    return "multi_compile";
                case KeywordDefinition.ShaderFeature:
                    return "shader_feature";
                default:
                    return string.Empty;
            }
        }

        static void GenerateKeywordPragmaStrings(
            string keywordReferenceName,
            KeywordDefinition keywordDefinition,
            KeywordScope keywordScope,
            KeywordShaderStage keywordStages,
            string keywordVariantsString,
            Action<string> PragmaStringAction)
        {
            string definitionString = keywordDefinition.ToDeclarationString();
            string scopeString = keywordScope.ToDeclarationSuffix();

            // check the active shader stages
            if ((keywordStages == KeywordShaderStage.All) || (keywordStages == 0))  // 0 is a default, so assume that means ALL
            {
                PragmaStringAction($"#pragma {definitionString}{scopeString} {keywordVariantsString}");
            }
            else
            {
                // have to process each stage separately
                for (int shaderStage = (int)KeywordShaderStage.Vertex; shaderStage <= (int)keywordStages; shaderStage = shaderStage * 2)
                {
                    if (((int)keywordStages & shaderStage) != 0)
                    {
                        var keywordStage = (KeywordShaderStage)shaderStage;
                        var stageString = keywordStage.ToKeywordStagesString();
                        PragmaStringAction($"#pragma {definitionString}{scopeString}{stageString} {keywordVariantsString}");
                    }
                }
            }
        }

        public static void GenerateEnumKeywordPragmaStrings(
            string keywordReferenceName,
            KeywordDefinition keywordDefinition,
            KeywordScope keywordScope,
            KeywordShaderStage keywordStages,
            IEnumerable<KeywordEntry> keywordEntries,
            Action<string> pragmaStringAction)
        {
            if (keywordDefinition != KeywordDefinition.Predefined)
            {
                var entryStrings = keywordEntries.Select(x => $"{keywordReferenceName}_{x.referenceName}");
                string variantsString = string.Join(" ", entryStrings);
                GenerateKeywordPragmaStrings(keywordReferenceName, keywordDefinition, keywordScope, keywordStages, variantsString, pragmaStringAction);
            }
        }

        public static void GenerateBooleanKeywordPragmaStrings(
            string keywordReferenceName,
            KeywordDefinition keywordDefinition,
            KeywordScope keywordScope,
            KeywordShaderStage keywordStages,
            Action<string> pragmaStringAction)
        {
            if (keywordDefinition != KeywordDefinition.Predefined)
            {
                string variantsString = $"_ {keywordReferenceName}";
                GenerateKeywordPragmaStrings(keywordReferenceName, keywordDefinition, keywordScope, keywordStages, variantsString, pragmaStringAction);
            }
        }

        public static string ToKeywordStagesString(this KeywordShaderStage stages)
        {
            string outString = "";

            if (stages == KeywordShaderStage.All)
                return outString;
            if (stages == KeywordShaderStage.Vertex)
                outString += "_vertex";
            if (stages == KeywordShaderStage.Fragment)
                outString += "_fragment";
            if (stages == KeywordShaderStage.Geometry)
                outString += "_geometry";
            if (stages == KeywordShaderStage.Hull)
                outString += "_hull";
            if (stages == KeywordShaderStage.Domain)
                outString += "_domain";
            if (stages == KeywordShaderStage.RayTracing)
                outString += "_raytracing";

            return outString;
        }

        public static string ToDefineString(this KeywordDescriptor keyword, int value)
        {
            switch (keyword.type)
            {
                case KeywordType.Boolean:
                    return value == 1 ? $"#define {keyword.referenceName} 1" : string.Empty;
                case KeywordType.Enum:
                    return $"#define {keyword.referenceName}_{keyword.entries[value].referenceName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string GetKeywordPermutationSetConditional(List<int> permutationSet)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("#if ");

            for (int i = 0; i < permutationSet.Count; i++)
            {
                // Subsequent permutation predicates require ||
                if (i != 0)
                    sb.Append(" || ");

                // Append permutation
                sb.Append($"defined(KEYWORD_PERMUTATION_{permutationSet[i]})");
            }

            return sb.ToString();
        }

        public static string GetKeywordPermutationConditional(int permutation)
        {
            return $"#if defined(KEYWORD_PERMUTATION_{permutation})";
        }

        public static void GetKeywordPermutationDeclarations(ShaderStringBuilder sb, List<List<KeyValuePair<ShaderKeyword, int>>> permutations)
        {
            if (permutations.Count == 0)
                return;

            for (int p = 0; p < permutations.Count; p++)
            {
                // ShaderStringBuilder.Append doesnt apply indentation
                sb.TryAppendIndentation();

                // Append correct if
                bool isLast = false;
                if (p == 0)
                {
                    sb.Append("#if ");
                }
                else if (p == permutations.Count - 1)
                {
                    sb.Append("#else");
                    isLast = true;
                }
                else
                {
                    sb.Append("#elif ");
                }

                // Last permutation is always #else
                if (!isLast)
                {
                    // Track whether && is required
                    bool appendAnd = false;

                    // Iterate all keywords that are part of the permutation
                    for (int i = 0; i < permutations[p].Count; i++)
                    {
                        // When previous keyword was inserted subsequent requires &&
                        string and = appendAnd ? " && " : string.Empty;

                        switch (permutations[p][i].Key.keywordType)
                        {
                            case KeywordType.Enum:
                            {
                                sb.Append($"{and}defined({permutations[p][i].Key.referenceName}_{permutations[p][i].Key.entries[permutations[p][i].Value].referenceName})");
                                appendAnd = true;
                                break;
                            }
                            case KeywordType.Boolean:
                            {
                                // HLSL does not support a !value predicate
                                if (permutations[p][i].Value != 0)
                                {
                                    continue;
                                }

                                sb.Append($"{and}defined({permutations[p][i].Key.referenceName})");
                                appendAnd = true;
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                sb.AppendNewLine();

                // Define the matching permutation keyword
                sb.IncreaseIndent();
                sb.AppendLine($"#define KEYWORD_PERMUTATION_{p}");
                sb.DecreaseIndent();
            }

            // End statement
            sb.AppendLine("#endif");
        }
    }
}
