using System;
using System.Text.RegularExpressions;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    [Obsolete("0.10+ This class will be removed from GTF public API")]
    public static class StringExtensions
    {
        static readonly Regex k_CodifyRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        [Obsolete("0.10+ This method will be removed from GTF public API")]
        public static string CodifyString(this string str)
        {
            return CodifyStringInternal(str);
        }

        internal static string CodifyStringInternal(this string str)
        {
            return k_CodifyRegex.Replace(str, "_");
        }
    }
}
