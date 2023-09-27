using System;
using System.Text.RegularExpressions;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Set of utility functions with <see cref="string"/>
    /// </summary>
    public static class StringExtensions
    {
        private static Regex k_InvalidRegEx = new (string.Format(@"([{0}]*\.+$)|([{0}]+)", Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()))), RegexOptions.Compiled);

        /// <summary>
        /// Replaces invalid characters for a filename or a directory with a given optional replacemenet string
        /// </summary>
        /// <param name="input">The input filename or directory</param>
        /// <param name="replacement">The replacement</param>
        /// <returns>The string with the invalid characters replaced</returns>
        public static string ReplaceInvalidFileNameCharacters(this string input, string replacement = "_") => k_InvalidRegEx.Replace(input, replacement);

        /// <summary>
        /// Checks if the given string ends with the given extension
        /// </summary>
        /// <param name="input">The input string</param>
        /// <param name="extension">The extension</param>
        /// <returns>True if the extension is found on the string path</returns>
        public static bool HasExtension(this string input, string extension) =>
            input.EndsWith(extension, StringComparison.OrdinalIgnoreCase);


        /// <summary>
        /// Checks if a string contains any of the strings given in strings to check and early out if it does
        /// </summary>
        /// <param name="input">The input string</param>
        /// <param name="stringsToCheck">List of strings to check</param>
        /// <returns></returns>
        public static bool ContainsAny(this string input, params string[] stringsToCheck)
        {
            if(string.IsNullOrEmpty(input))
                return false;

            foreach (var value in stringsToCheck)
            {
                if(string.IsNullOrEmpty(value))
                    continue;

                if (input.Contains(value))
                    return true;
            }

            return false;
        }
    }
}
