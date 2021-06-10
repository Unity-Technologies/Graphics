using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    class KeywordCollector
    {
        public readonly List<ShaderKeyword> keywords;
        public readonly List<List<KeyValuePair<ShaderKeyword, int>>> permutations;

        public KeywordCollector()
        {
            keywords = new List<ShaderKeyword>();
            permutations = new List<List<KeyValuePair<ShaderKeyword, int>>>();
        }

        public void AddShaderKeyword(ShaderKeyword chunk)
        {
            if (keywords.Any(x => x.referenceName == chunk.referenceName))
                return;

            keywords.Add(chunk);
        }

        public void GetKeywordsDeclaration(ShaderStringBuilder builder, GenerationMode mode)
        {
            if (keywords.Count == 0)
                return;

            // Declare keywords
            foreach (var keyword in keywords)
            {
                // Hardcode active keywords in preview to reduce compiled variants
                if (mode == GenerationMode.Preview)
                {
                    string declaration = keyword.GetKeywordPreviewDeclarationString();
                    if (!string.IsNullOrEmpty(declaration))
                    {
                        builder.AppendLine(declaration);
                    }
                }
                else
                {
                    keyword.AppendKeywordDeclarationStrings(builder);
                }
            }

            // Declare another keyword per permutation for simpler if/defs in the graph code
            builder.AppendNewLine();
            KeywordUtil.GetKeywordPermutationDeclarations(builder, permutations);
            builder.AppendNewLine();
        }

        public void CalculateKeywordPermutations()
        {
            permutations.Clear();

            // Initialize current permutation
            List<KeyValuePair<ShaderKeyword, int>> currentPermutation = new List<KeyValuePair<ShaderKeyword, int>>();
            for (int i = 0; i < keywords.Count; i++)
            {
                currentPermutation.Add(new KeyValuePair<ShaderKeyword, int>(keywords[i], 0));
            }

            // Recursively permute keywords
            PermuteKeywords(keywords, currentPermutation, 0);
        }

        void PermuteKeywords(List<ShaderKeyword> keywords, List<KeyValuePair<ShaderKeyword, int>> currentPermutation, int currentIndex)
        {
            if (currentIndex == keywords.Count)
                return;

            // Iterate each possible keyword at the current index
            int entryCount = keywords[currentIndex].keywordType == KeywordType.Enum ? keywords[currentIndex].entries.Count : 2;
            for (int i = 0; i < entryCount; i++)
            {
                // Set the index in the current permutation to the correct value
                currentPermutation[currentIndex] = new KeyValuePair<ShaderKeyword, int>(keywords[currentIndex], i);

                // If the current index is the last keyword we are finished with this permutation
                if (currentIndex == keywords.Count - 1)
                {
                    permutations.Add(currentPermutation);
                }
                // Otherwise we continue adding keywords to this permutation
                else
                {
                    PermuteKeywords(keywords, currentPermutation, currentIndex + 1);
                }

                // Duplicate current permutation
                currentPermutation = currentPermutation.Select(item => item).ToList();
            }
        }
    }
}
