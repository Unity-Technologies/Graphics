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
            
#if SHADERGRAPH_EXPLICITBRANCH
            for(int i = 0; i < keywords.Count; i++)
            {
                currentPermutation.Add(new KeyValuePair<ShaderKeyword, int>(keywords[i], 0));
            }
            
            PermuteKeywords(keywords, currentPermutation, results, 0);
#endif
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

        public static string GetKeywordPermutationString(List<KeyValuePair<ShaderKeyword, int>> permutation)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < permutation.Count; i++)
            {
                if(permutation[i].Key.keywordType == ShaderKeywordType.Enum)
                {
                    sb.Append($"defined({permutation[i].Key.referenceName}_{permutation[i].Key.entries[permutation[i].Value].referenceName})");
                    if(i != permutation.Count - 1)
                        sb.Append(" && ");
                }
                else if(permutation[i].Value == 0)
                {
                    sb.Append($"defined({permutation[i].Key.referenceName}_ON)");
                    if(i != permutation.Count - 1)
                        sb.Append(" && ");
                }
            }
            return sb.ToString();
        }
    }
}
