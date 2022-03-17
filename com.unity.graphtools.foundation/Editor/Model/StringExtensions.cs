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
        /// <param name="baseName">The object's name to format.</param>
        /// <param name="index">The object's index to be displayed next to its name.</param>
        /// <returns>The object's formatted name.</returns>
        public static string FormatWithNamingScheme(this string baseName, int index)
        {
            if (index < 0)
                return baseName;

            var formattedIndex = index.ToString("D" + Math.Max(1, EditorSettings.gameObjectNamingDigits));

            switch (EditorSettings.gameObjectNamingScheme)
            {
                case EditorSettings.NamingScheme.Dot:
                    return baseName + "." + formattedIndex;
                case EditorSettings.NamingScheme.Underscore:
                    return baseName + "_" + formattedIndex;
                default:
                    return baseName + " (" + formattedIndex + ")";
            }
        }

        /// <summary>
        /// Extracts the basename and index from an object's name following Editor Settings' naming scheme and naming digits.
        /// </summary>
        /// <param name="originalName">The name to extract.</param>
        /// <param name="basename">The extracted basename.</param>
        /// <param name="index">The extracted index. Will be <c>0</c> if no index was found.</param>
        public static void ExtractBaseNameAndIndex(this string originalName, out string basename, out int index)
        {
            index = 0;
            basename = originalName;

            var useParenthesis = EditorSettings.gameObjectNamingScheme == EditorSettings.NamingScheme.SpaceParenthesis;
            var minVarLength = useParenthesis ? 4 : 3; // "a.1" "b (2)"
            if (originalName.Length < minVarLength || useParenthesis && originalName[originalName.Length - 1] != ')')
                return;

            char charBeforeNumber;
            int separatorSize = 1;
            switch (EditorSettings.gameObjectNamingScheme)
            {
                case EditorSettings.NamingScheme.Dot:
                    charBeforeNumber = '.';
                    break;
                case EditorSettings.NamingScheme.Underscore:
                    charBeforeNumber = '_';
                    break;
                default:
                    charBeforeNumber = '(';
                    separatorSize = 2; // " )"
                    break;
            }

            var separatorIndex = originalName.LastIndexOf(charBeforeNumber);
            if (separatorIndex < 1
                || useParenthesis && (separatorIndex < 2 || originalName[separatorIndex - 1] != ' '))
                return; // separator not found or first character, return full name

            int numPos = separatorIndex + 1;

            int numLength = originalName.Length - numPos - (useParenthesis ? 1 : 0);

            if (numLength < 1)
                return;

            var numStr = originalName.Substring(numPos, numLength);
            if (!int.TryParse(numStr, out var extractedIndex))
                return;

            basename = originalName.Substring(0, numPos - separatorSize);
            index = extractedIndex;
        }
    }
}
