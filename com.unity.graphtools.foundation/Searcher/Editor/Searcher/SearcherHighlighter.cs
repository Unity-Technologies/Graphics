using System;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    static class SearcherHighlighter
    {
        const char k_StartHighlightSeparator = '{';
        const char k_EndHighlightSeparator = '}';
        const string k_HighlightedStyleClassName = "Highlighted";

        public static void HighlightTextBasedOnQuery(VisualElement container, string text, string query)
        {
            var formattedText = text;
            var queryParts = query.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            var regex = string.Empty;
            for (var index = 0; index < queryParts.Length; index++)
            {
                var queryPart = queryParts[index];
                regex += $"({Regex.Escape(queryPart)})";
                if (index < queryParts.Length - 1)
                    regex += "|";
            }

            var matches = Regex.Matches(formattedText, regex, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                formattedText = formattedText.Replace(match.Value,
                    $"{k_StartHighlightSeparator}{match.Value}{k_EndHighlightSeparator}");
            }

            BuildHighlightLabels(container, formattedText);
        }

        static void BuildHighlightLabels(VisualElement container, string formattedHighlightText)
        {
            if (string.IsNullOrEmpty(formattedHighlightText))
                return;

            var substring = string.Empty;
            var highlighting = false;
            var skipCount = 0;
            foreach (var character in formattedHighlightText.ToCharArray())
            {
                switch (character)
                {
                    // Skip embedded separators
                    // Ex:
                    // Query: middle e
                    // Text: Middle Eastern
                    // Formatted Text: {Middl{e}} {E}ast{e}rn
                    //                      ^ ^
                    case k_StartHighlightSeparator when highlighting:
                        skipCount++;
                        continue;
                    case k_StartHighlightSeparator:
                    {
                        highlighting = true;
                        if (!string.IsNullOrEmpty(substring))
                        {
                            container.Add(new Label(substring));
                            substring = string.Empty;
                        }

                        continue;
                    }
                    case k_EndHighlightSeparator when skipCount > 0:
                        skipCount--;
                        continue;
                    case k_EndHighlightSeparator:
                    {
                        var label = new Label(substring);
                        label.AddToClassList(k_HighlightedStyleClassName);
                        container.Add(label);

                        highlighting = false;
                        substring = string.Empty;

                        continue;
                    }
                    default:
                        substring += character;
                        break;
                }
            }

            if (!string.IsNullOrEmpty(substring))
            {
                var label = new Label(substring);
                if (highlighting)
                    label.AddToClassList(k_HighlightedStyleClassName);
                container.Add(label);
            }
        }
    }
}
