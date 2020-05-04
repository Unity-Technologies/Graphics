using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Material quality flags.
    /// </summary>
    [Flags]
    [MovedFrom("Utilities")]
    public enum MaterialQuality
    {
        /// <summary>Low Material Quality.</summary>
        Low = 1 << 0,
        /// <summary>Medium Material Quality.</summary>
        Medium = 1 << 1,
        /// <summary>High Material Quality.</summary>
        High = 1 << 2
    }

    /// <summary>
    /// Material Quality utility class.
    /// </summary>
    [MovedFrom("Utilities")]
    public static class MaterialQualityUtilities
    {
        /// <summary>
        /// Keywords strings for Material Quality levels.
        /// </summary>
        public static string[] KeywordNames =
        {
            "MATERIAL_QUALITY_LOW",
            "MATERIAL_QUALITY_MEDIUM",
            "MATERIAL_QUALITY_HIGH",
        };

        /// <summary>
        /// String representation of the MaterialQuality enum.
        /// </summary>
        public static string[] EnumNames = Enum.GetNames(typeof(MaterialQuality));

        /// <summary>
        /// Keywords for Material Quality levels.
        /// </summary>
        public static ShaderKeyword[] Keywords =
        {
            new ShaderKeyword(KeywordNames[0]),
            new ShaderKeyword(KeywordNames[1]),
            new ShaderKeyword(KeywordNames[2]),
        };

        /// <summary>
        /// Returns the highest available quality level in a MaterialQuality bitfield.
        /// </summary>
        /// <param name="levels">Input MaterialQuality bitfield.</param>
        /// <returns>The highest available quality level.</returns>
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

        /// <summary>
        /// Returns the closest available quality level in a MaterialQuality bitfield.
        /// </summary>
        /// <param name="availableLevels">Available MaterialQuality bitfield.</param>
        /// <param name="requestedLevel">Input MaterialQuality level.</param>
        /// <returns>The closest available quality level.</returns>
        public static MaterialQuality GetClosestQuality(this MaterialQuality availableLevels, MaterialQuality requestedLevel)
        {
            // Special fallback when there are no available quality levels. Needs to match in the shader stripping code
            if (availableLevels == 0)
                return MaterialQuality.Low;

            // First we want to find the closest available quality level below the requested one.
            int requestedLevelIndex = ToFirstIndex(requestedLevel);
            MaterialQuality chosenQuality = (MaterialQuality)0;
            for (int i = requestedLevelIndex; i >= 0; --i)
            {
                var level = FromIndex(i);
                if ((level & availableLevels) != 0)
                {
                    chosenQuality = level;
                    break;
                }
            }

            if (chosenQuality != 0)
                return chosenQuality;

            // If none is found then we fallback to the closest above.
            for (var i = requestedLevelIndex + 1; i < Keywords.Length; ++i)
            {
                var level = FromIndex(i);
                var diff = Math.Abs(requestedLevel - level);
                if ((level & availableLevels) != 0)
                {
                    chosenQuality = level;
                    break;
                }
            }

            Debug.Assert(chosenQuality != 0);
            return chosenQuality;
        }

        /// <summary>
        /// Set the global keyword for the provided MaterialQuality.
        /// </summary>
        /// <param name="level">MaterialQuality level to set the keyword for.</param>
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

        /// <summary>
        /// Set the global keyword for the provided MaterialQuality.
        /// </summary>
        /// <param name="level">MaterialQuality level to set the keyword for.</param>
        /// <param name="cmd">Command Buffer used to setup the keyword.</param>
        public static void SetGlobalShaderKeywords(this MaterialQuality level, CommandBuffer cmd)
        {
            for (var i = 0; i < KeywordNames.Length; ++i)
            {
                if ((level & (MaterialQuality)(1 << i)) != 0)
                    cmd.EnableShaderKeyword(KeywordNames[i]);
                else
                    cmd.DisableShaderKeyword(KeywordNames[i]);
            }
        }

        /// <summary>
        /// Returns the index (in the MaterialQuality enum) of the first available level.
        /// </summary>
        /// <param name="level">MaterialQuality bitfield.</param>
        /// <returns>The index of the first available level.</returns>
        public static int ToFirstIndex(this MaterialQuality level)
        {
            for (var i = 0; i < KeywordNames.Length; ++i)
            {
                if ((level & (MaterialQuality) (1 << i)) != 0)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the enum equivalent of the index in the MaterialQuality enum list.
        /// </summary>
        /// <param name="index">Index of the material quality.</param>
        /// <returns>The equivalent enum.</returns>
        public static MaterialQuality FromIndex(int index) => (MaterialQuality) (1 << index);
    }
}
