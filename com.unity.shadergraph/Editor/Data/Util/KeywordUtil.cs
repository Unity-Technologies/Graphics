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
            overrideReferenceName = "MATERIAL_QUALITY",
            isEditable = false,
            keywordDefinition = ShaderKeywordDefinition.ShaderFeature,
            keywordScope = ShaderKeywordScope.Global,
            entries = new List<ShaderKeywordEntry>()
            {
                new ShaderKeywordEntry("High", "HIGH"),
                new ShaderKeywordEntry("Medium", "MEDIUM"),
                new ShaderKeywordEntry("Low", "LOW"),
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
    }
}
