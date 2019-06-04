using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class BuiltinKeyword
    {
        public static ShaderKeyword QualityKeyword = new ShaderKeyword(ShaderKeywordType.Enum)
        {
            displayName = "Material Quality",
            isEditable = false,
            keywordDefinition = ShaderKeywordDefinition.ShaderFeature,
            keywordScope = ShaderKeywordScope.Global,
            entries = new List<ShaderKeywordEntry>()
            {
                new ShaderKeywordEntry("High", "MATERIAL_QUALITY_HIGH"),
                new ShaderKeywordEntry("Medium", "_"),
                new ShaderKeywordEntry("Low", "MATERIAL_QUALITY_LOW"),
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
    }
}
