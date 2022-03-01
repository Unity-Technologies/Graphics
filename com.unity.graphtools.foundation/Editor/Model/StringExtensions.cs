using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// String extension methods.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Makes a displayable name for a variable.
        /// </summary>
        /// <remarks>This is merely a wrapper for <see cref="ObjectNames.NicifyVariableName"/>.</remarks>
        /// <param name="value">The variable name to nicify.</param>
        /// <returns>The nicified variable name.</returns>
        public static string Nicify(this string value)
        {
            return ObjectNames.NicifyVariableName(value);
        }

        /// <summary>
        /// Formats an object's name to make it compatible with the Editor Settings' naming scheme and naming digits.
        /// </summary>
        /// <param name="value">The object's name to format.</param>
        /// <param name="index">The object's index to be displayed next to its name.</param>
        /// <returns>The object's formatted name.</returns>
        public static string FormatWithNamingScheme(this string value, int index)
        {
            if (index < 0)
                return value;

            var formattedIndex = index.ToString("D" + Math.Max(1, EditorSettings.gameObjectNamingDigits));

            switch (EditorSettings.gameObjectNamingScheme)
            {
                case EditorSettings.NamingScheme.Dot:
                    return value + "." + formattedIndex;
                case EditorSettings.NamingScheme.Underscore:
                    return value + "_" + formattedIndex;
                default:
                    return value + " (" + formattedIndex + ")";
            }
        }
    }
}
