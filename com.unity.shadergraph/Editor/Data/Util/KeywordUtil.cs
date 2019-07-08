using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class BuiltinKeyword
    {
        public static ShaderKeyword QualityKeyword = new ShaderKeyword(ShaderKeywordType.Enum)
        {
            displayName = "Material Quality",
            overrideReferenceName = "MATERIAL_QUALITY",
            isEditable = false,
            keywordDefinition = ShaderKeywordDefinition.ShaderFeature,
            keywordScope = ShaderKeywordScope.Global,
            neverExpose = true,
            entries = new List<ShaderKeywordEntry>()
            {
                new ShaderKeywordEntry(1, "High", "HIGH"),
                new ShaderKeywordEntry(2, "Medium", "MEDIUM"),
                new ShaderKeywordEntry(3, "Low", "LOW"),
            },
        };
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

        public static string ToKeywordValueString(this ShaderKeywordDefinition keywordDefinition)
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

        public static List<List<KeyValuePair<ShaderKeyword, int>>> GetKeywordPermutations(List<ShaderKeyword> keywords)
        {
            List<KeyValuePair<ShaderKeyword, int>> currentPermutation = new List<KeyValuePair<ShaderKeyword, int>>();
            List<List<KeyValuePair<ShaderKeyword, int>>> results = new List<List<KeyValuePair<ShaderKeyword, int>>>();
            
            for(int i = 0; i < keywords.Count; i++)
            {
                currentPermutation.Add(new KeyValuePair<ShaderKeyword, int>(keywords[i], 0));
            }
            
            PermuteKeywords(keywords, currentPermutation, results, 0);

            return results;
        }

        // Recursively permute the items that are
        // not yet in the current selection.
        static void PermuteKeywords(List<ShaderKeyword> keywords,
            List<KeyValuePair<ShaderKeyword, int>> currentPermutation, List<List<KeyValuePair<ShaderKeyword, int>>> results,
            int keywordIndex)
        {
            if(keywordIndex == keywords.Count)
                return;

            int entryCount = keywords[keywordIndex].keywordType == ShaderKeywordType.Enum ? keywords[keywordIndex].entries.Count : 2;
            for(int i = 0; i < entryCount; i++)
            {
                currentPermutation[keywordIndex] = new KeyValuePair<ShaderKeyword, int>(keywords[keywordIndex], i);

                if(keywordIndex == keywords.Count - 1)
                    results.Add(currentPermutation);
                else
                    PermuteKeywords(keywords, currentPermutation, results, keywordIndex + 1);

                currentPermutation = currentPermutation.Select(item => item).ToList();
            }
        }

        public static string GetKeywordPermutationGroupIfDef(List<int> permutationGroup)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("#if ");

            for(int i = 0; i < permutationGroup.Count; i++)
            {
                if(i != 0)
                    sb.Append(" || ");

                sb.Append($"defined(KEYWORD_PERMUTATION_{permutationGroup[i]})");
            }
            return sb.ToString();
        }

        public static string GetKeywordPermutationDeclaration(ShaderStringBuilder sb, List<List<KeyValuePair<ShaderKeyword, int>>> permutations)
        {
            if (permutations.Count == 0)
                return string.Empty;

            for(int p = 0; p < permutations.Count; p++)
            {
                if(p == 0)
                    sb.Append("#if ");
                else if(p == permutations.Count - 1)
                    sb.Append("#else");
                else
                    sb.Append("#elif ");

                bool appendAndFromPrevious = false;
                if(p != permutations.Count - 1)
                {
                    for(int i = 0; i < permutations[p].Count; i++)
                    {                
                        if(permutations[p][i].Key.keywordType == ShaderKeywordType.Enum)
                        {
                            if(appendAndFromPrevious)
                                sb.Append(" && ");
                            
                            sb.Append($"defined({permutations[p][i].Key.referenceName}_{permutations[p][i].Key.entries[permutations[p][i].Value].referenceName})");
                            appendAndFromPrevious = true;
                        }
                        else if(permutations[p][i].Value == 0)
                        {
                            if(appendAndFromPrevious)
                                sb.Append(" && ");
                            
                            sb.Append($"defined({permutations[p][i].Key.referenceName}_ON)");
                            appendAndFromPrevious = true;
                        }
                    }
                }

                sb.AppendNewLine();
                sb.AppendLine($"#define KEYWORD_PERMUTATION_{p}");
            }
            sb.Append("#endif");
            sb.AppendNewLine();
            sb.AppendNewLine();

            return sb.ToString();
        }
    }
}
