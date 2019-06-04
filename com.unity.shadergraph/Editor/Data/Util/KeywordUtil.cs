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
            keywordType = ShaderKeywordType.MultiCompile,
            keywordScope = ShaderKeywordScope.Global,
            entries = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("High", "MATERIAL_QUALITY_HIGH"),
                new KeyValuePair<string, string>("Medium", "_"),
                new KeyValuePair<string, string>("Low", "MATERIAL_QUALITY_LOW"),
            },
        };
    }
}
