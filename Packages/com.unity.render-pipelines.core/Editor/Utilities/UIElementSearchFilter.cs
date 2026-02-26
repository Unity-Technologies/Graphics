using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    // Utility class for adding search filtering functionality to UITK views.
    // Provides text highlighting, search input throttling, and common search operations.
    class UIElementSearchFilter
    {
        internal const string k_SelectionColorBeginTag = "<mark=#3169ACAB>";
        internal const string k_SelectionColorEndTag = "</mark>";
        const int k_SearchStringLimit = 15;

        IVisualElementScheduledItem m_PreviousSearch;
        string m_PendingSearchString;
        readonly VisualElement m_RootElement;
        readonly Action<string> m_PerformSearchCallback;

        public UIElementSearchFilter(VisualElement rootElement, Action<string> performSearchCallback)
        {
            m_RootElement = rootElement;
            m_PerformSearchCallback = performSearchCallback;
        }

        public void InitializeSearchField(string searchFieldName)
        {
            var searchField = m_RootElement.Q<ToolbarSearchField>(searchFieldName);
            searchField.placeholderText = L10n.Tr("Search");
            searchField.RegisterValueChangedCallback(evt => OnSearchFilterChanged(evt.newValue));

            // Apply empty search once in case the entire window was reloaded with a non-empty search. Search
            // modifies the UI document directly, and its state is not reset when triggering the Reload Window option.
            OnSearchFilterChanged(string.Empty);
        }

        void OnSearchFilterChanged(string searchString)
        {
            // Ensure the search string is within the allowed length limit
            if (searchString.Length > k_SearchStringLimit)
            {
                searchString = searchString[..k_SearchStringLimit];
                Debug.LogWarning($"Search string limit exceeded: {k_SearchStringLimit}");
            }

            // Sanitize to not match rich text tags
            searchString = searchString.Replace("<", string.Empty).Replace(">", string.Empty);

            // If the search string hasn't changed, avoid repeating the same search
            if (m_PendingSearchString == searchString)
                return;

            m_PendingSearchString = searchString;

            if (m_PreviousSearch != null && m_PreviousSearch.isActive)
                m_PreviousSearch.Pause();

            m_PreviousSearch = m_RootElement
                .schedule
                .Execute(() => m_PerformSearchCallback(searchString))
                .StartingIn(5);  // Avoid spamming multiple search if the user types really fast
        }

        static bool IsInsideTag(string input, int index)
        {
            int openTagIndex = input.LastIndexOf('<', index);
            int closeTagIndex = input.LastIndexOf('>', index);
            return openTagIndex > closeTagIndex;
        }

        static bool IsSearchFilterMatch(string str, string searchString, out int startIndex, out int endIndex)
        {
            startIndex = -1;
            endIndex = -1;

            if (searchString.Length == 0)
                return true;

            int searchStartIndex = 0;
            for (;;)
            {
                startIndex = str.IndexOf(searchString, searchStartIndex, StringComparison.CurrentCultureIgnoreCase);

                // If we found a match but it is inside another tag, ignore it and continue
                if (startIndex != -1 && IsInsideTag(str, startIndex))
                {
                    searchStartIndex = startIndex + 1;
                    continue;
                }

                // Either valid match (not inside another tag) or no match
                break;
            }

            if (startIndex == -1)
                return false;

            endIndex = startIndex + searchString.Length - 1;
            return true;
        }

        public static void ApplySearchAndHighlight(TextElement textElement, string searchString, out bool isMatch)
        {
            var text = textElement.text;

            // Remove existing match highlight tags
            var hasHighlight = text.IndexOf(k_SelectionColorBeginTag, StringComparison.Ordinal) >= 0;
            if (hasHighlight)
            {
                text = text.Replace(k_SelectionColorBeginTag, string.Empty);
                text = text.Replace(k_SelectionColorEndTag, string.Empty);
            }

            if (!IsSearchFilterMatch(text, searchString, out int startHighlight, out int endHighlight))
            {
                // Reset original text
                textElement.text = text;
                isMatch = false;
                return;
            }

            if (startHighlight >= 0 && endHighlight >= 0)
            {
                text = text.Insert(startHighlight, k_SelectionColorBeginTag);
                text = text.Insert(endHighlight + k_SelectionColorBeginTag.Length + 1, k_SelectionColorEndTag);
            }
            textElement.text = text;
            isMatch = true;
        }
    }
}
