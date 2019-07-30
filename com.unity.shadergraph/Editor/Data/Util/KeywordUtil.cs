using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class BuiltinKeyword
    {
        public static ShaderKeyword QualityKeyword = ShaderKeyword.BuiltinEnumKeyword(new ShaderKeywordDescriptor()
        {
            displayName = "Material Quality",
            referenceName = "MATERIAL_QUALITY",
            definition = ShaderKeywordDefinition.ShaderFeature,
            scope = ShaderKeywordScope.Global,
            value = 0,
            entries = new List<ShaderKeywordEntry>()
            {
                new ShaderKeywordEntry(1, "High", "HIGH"),
                new ShaderKeywordEntry(2, "Medium", "MEDIUM"),
                new ShaderKeywordEntry(3, "Low", "LOW"),
            },
        });
    }

    static class KeywordUtil
    {
        public static ConcreteSlotValueType ToConcreteSlotValueType(this ShaderKeywordType keywordType)
        {
            switch(keywordType)
            {
                case ShaderKeywordType.Boolean:
                    return ConcreteSlotValueType.Boolean;
                case ShaderKeywordType.Enum:
                    return ConcreteSlotValueType.Vector1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string ToDeclarationString(this ShaderKeywordDefinition keywordDefinition)
        {
            switch(keywordDefinition)
            {
                case ShaderKeywordDefinition.MultiCompile:
                    return "multi_compile";
                case ShaderKeywordDefinition.ShaderFeature:
                    return "shader_feature";
                default:
                    return string.Empty;
            }
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
                            case ShaderKeywordType.Enum:
                            {
                                sb.Append($"{and}defined({permutations[p][i].Key.referenceName}_{permutations[p][i].Key.entries[permutations[p][i].Value].referenceName})");
                                appendAnd = true;
                                break;
                            }
                            case ShaderKeywordType.Boolean:
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
                sb.AppendIndentation();
                sb.AppendLine($"#define KEYWORD_PERMUTATION_{p}");
            }

            // End statement
            sb.AppendLine("#endif");
            sb.AppendNewLine();
        }
    }
}
