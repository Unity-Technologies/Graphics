using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Utilities
{
    [Flags]
    public enum MaterialQuality
    {
        Low = 1 << 0,
        Medium = 1 << 1,
        High = 1 << 2
    }

    public static class MaterialQualityUtilities
    {
        public static string[] KeywordNames =
        {
            "MATERIAL_QUALITY_LOW",
            "MATERIAL_QUALITY_MEDIUM",
            "MATERIAL_QUALITY_HIGH",
        };

        public static string[] EnumNames = Enum.GetNames(typeof(MaterialQuality));

        public static ShaderKeyword[] Keywords =
        {
            new ShaderKeyword(KeywordNames[0]),
            new ShaderKeyword(KeywordNames[1]),
            new ShaderKeyword(KeywordNames[2]),
        };

        public static MaterialQuality GetHighestQuality(this MaterialQuality levels)
        {
            for (var i = Keywords.Length - 1; i >= 0; --i)
            {
                var level = (MaterialQuality) (1 << i);
                if ((levels & level) != 0)
                    return level;
            }

            return 0;
        }

        public static void SetGlobalShaderKeywords(this MaterialQuality level)
        {
            for (var i = 0; i < KeywordNames.Length; ++i)
            {
                if ((level & (MaterialQuality) (1 << i)) != 0)
                    Shader.EnableKeyword(KeywordNames[i]);
                else
                    Shader.DisableKeyword(KeywordNames[i]);
            }
        }

        public static int ToFirstIndex(this MaterialQuality level)
        {
            for (var i = 0; i < KeywordNames.Length; ++i)
            {
                if ((level & (MaterialQuality) (1 << i)) != 0)
                    return i;
            }

            return -1;
        }

        public static MaterialQuality FromIndex(int index) => (MaterialQuality) (1 << index);
    }
}
