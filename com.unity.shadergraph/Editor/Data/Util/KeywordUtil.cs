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
            };
        }
    }

    static class KeywordUtil
    {
        public static IEnumerable<KeywordDescriptor> GetBuiltinKeywordDescriptors() =>
            TypeCache.GetMethodsWithAttribute<BuiltinKeywordAttribute>()
            .Where(method => method.IsStatic && method.ReturnType == typeof(KeywordDescriptor))
            .Select(method =>
                (KeywordDescriptor) method.Invoke(null, new object[0] { }));

        public static ConcreteSlotValueType ToConcreteSlotValueType(this KeywordType keywordType)
        {
            switch(keywordType)
            {
                case KeywordType.Boolean:
                    return ConcreteSlotValueType.Boolean;
                case KeywordType.Enum:
                    return ConcreteSlotValueType.Vector1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string ToDeclarationString(this KeywordDefinition keywordDefinition)
        {
            switch(keywordDefinition)
            {
                case KeywordDefinition.MultiCompile:
                    return "multi_compile";
                case KeywordDefinition.ShaderFeature:
                    return "shader_feature";
                default:
                    return string.Empty;
            }
        }

        public static string ToDeclarationString(this KeywordDescriptor keyword)
        {
            // Get definition type using scope
            string scopeString = keyword.scope == KeywordScope.Local ? "_local" : string.Empty;
            string definitionString = $"{keyword.definition.ToDeclarationString()}{scopeString}";

            switch(keyword.type)
            {
                case KeywordType.Boolean:
                    return $"#pragma {definitionString} _ {keyword.referenceName}";
                case KeywordType.Enum:
                    var enumEntryDefinitions = keyword.entries.Select(x => $"{keyword.referenceName}_{x.referenceName}");
                    string enumEntriesString = string.Join(" ", enumEntryDefinitions);
                    return $"#pragma {definitionString} {enumEntriesString}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string ToDefineString(this KeywordDescriptor keyword, int value)
        {
            switch(keyword.type)
            {
                case KeywordType.Boolean:
                    return value == 1 ? $"#define {keyword.referenceName}" : string.Empty;
                case KeywordType.Enum:
                    return $"#define {keyword.referenceName}_{keyword.entries[value].referenceName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static int GetKeywordPermutationCount(this GraphData graph)
        {
            // Gather all unique keywords from the Graph including Sub Graphs
            IEnumerable<ShaderKeyword> allKeywords = graph.keywords;
            var subGraphNodes = graph.GetNodes<SubGraphNode>();
            foreach(SubGraphNode subGraphNode in subGraphNodes)
            {
                if (subGraphNode.asset == null)
                {
                    continue;
                }
                allKeywords = allKeywords.Union(subGraphNode.asset.keywords);
            }
            allKeywords = allKeywords.Distinct();

            // Get permutation count for all Keywords
            int permutationCount = 1;
            foreach(ShaderKeyword keyword in allKeywords)
            {
                if(keyword.keywordType == KeywordType.Boolean)
                    permutationCount *= 2;
                else
                    permutationCount *= keyword.entries.Count;
            }

            return permutationCount;
        }

        public static string GetKeywordPermutationSetConditional(List<int> permutationSet)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("#if ");

            for(int i = 0; i < permutationSet.Count; i++)
            {
                // Subsequent permutation predicates require ||
                if(i != 0)
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

            for(int p = 0; p < permutations.Count; p++)
            {
                // ShaderStringBuilder.Append doesnt apply indentation
                sb.AppendIndentation();

                // Append correct if
                bool isLast = false;
                if(p == 0)
                {
                    sb.Append("#if ");
                }
                else if(p == permutations.Count - 1)
                {
                    sb.Append("#else");
                    isLast = true;
                }
                else
                {
                    sb.Append("#elif ");
                }

                // Last permutation is always #else
                if(!isLast)
                {
                    // Track whether && is required
                    bool appendAnd = false;

                    // Iterate all keywords that are part of the permutation
                    for(int i = 0; i < permutations[p].Count; i++)
                    {
                        // When previous keyword was inserted subsequent requires &&
                        string and = appendAnd ? " && " : string.Empty;

                        switch(permutations[p][i].Key.keywordType)
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
                                if(permutations[p][i].Value != 0)
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
