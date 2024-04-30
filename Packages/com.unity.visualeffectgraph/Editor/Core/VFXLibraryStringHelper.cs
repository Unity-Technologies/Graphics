using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace UnityEditor.VFX
{
    static class VFXLibraryStringHelper
    {
        private static readonly Regex s_NodeNameParser = new("(?<label>[|]?[^\\|]*)", RegexOptions.Compiled);

        public static string AppendLabel(this string text, string label, bool nicify = true)
        {
            return nicify
                ? $"{text}|{ObjectNames.NicifyVariableName(label)}"
                : $"{text}|{label}";
        }

        public static string AppendLiteral(this string text, string literal, bool nicify = true)
        {
            return nicify
                ? $"{text}|_{ObjectNames.NicifyVariableName(literal)}"
                : $"{text}|_{literal}";
        }

        public static string AppendSeparator(this string text, string separator, int index)
        {
            return $"{text}#{index}{separator}";
        }

        public static string Literal(this string literal, bool nicify = true)
        {
            return nicify
                ? $"|_{ObjectNames.NicifyVariableName(literal)}"
                : $"|_{literal}";
        }

        public static string Label(this string label, bool nicify = true)
        {
            return nicify
                ? $"|{ObjectNames.NicifyVariableName(label)}"
                : $"|{label}";
        }

        public static string Separator(string separator, int index)
        {
            return $"#{index}{separator}";
        }

        public static string ToHumanReadable(this string text)
        {
            return text.Replace("|_", " ").Replace('|', ' ').TrimStart();
        }

        public static IEnumerable<Label> SplitTextIntoLabels(this string text, string className)
        {
            var matches = s_NodeNameParser.Matches(text);
            if (matches.Count == 0)
            {
                yield return new Label(text);
                yield break;
            }
            foreach (var m in matches)
            {
                var match = (Match)m;
                if (match.Length == 0)
                    continue;
                if (match.Value.StartsWith("|_"))
                {
                    yield return new Label(match.Value.Substring(2, match.Length - 2));
                }
                else if (match.Value.StartsWith('|'))
                {
                    var label = new Label(match.Value.Substring(1, match.Length - 1));
                    label.AddToClassList(className);
                    yield return label;
                }
                else
                {
                    yield return new Label(match.Value);
                }
            }
        }
    }
}
