using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    interface IGeneratesBranch
    {
        Guid keywordGuid { get; set; }

        void GenerateBranchCode(ShaderStringBuilder sb, KeyValuePair<ShaderKeyword, int> permutation, GenerationMode generationMode);
    }
}
