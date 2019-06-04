using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class BuiltinKeyword
    {
        public static ShaderKeyword QualityKeyword = new ShaderKeyword()
        {
            displayName = "Material Quality",
            isEditable = false,
            keywordType = ShaderKeywordType.ShaderFeature,
            keywordScope = ShaderKeywordScope.Global,
            entries = new List<ShaderKeywordEntry>()
            {
                new ShaderKeywordEntry("High", "MATERIAL_QUALITY_HIGH"),
                new ShaderKeywordEntry("Medium", "_"),
                new ShaderKeywordEntry("Low", "MATERIAL_QUALITY_LOW"),
            },
        };
    }
}
