#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    internal class WidgetSearchData
    {
        public List<TextElement> textElements;
        public string aggregatedAdditionalSearchText;

        public WidgetSearchData(List<TextElement> textElements, string aggregatedAdditionalSearchText)
        {
            this.textElements = textElements;
            this.aggregatedAdditionalSearchText = aggregatedAdditionalSearchText;
        }
    }

    sealed partial class DebugWindow
    {
#if ENABLE_RENDERING_DEBUGGER_UI
        readonly Dictionary<DebugUI.Widget, WidgetSearchData> m_WidgetSearchElementCache = new();
        readonly List<TextElement> m_PanelHeaderTextElements = new();
        UIElementSearchFilter m_SearchFilter;

        void BuildSearchCache()
        {
            m_WidgetSearchElementCache.Clear();
            m_PanelHeaderTextElements.Clear();

            foreach (var panelElement in m_RightPaneElement.Children())
            {
                var headerLabel = panelElement.Q<Label>(className: "debug-window-search-filter-target");
                if (headerLabel != null)
                    m_PanelHeaderTextElements.Add(headerLabel);
            }

            DebugManager.instance.ForEachWidget(widget =>
            {
                if (widget.m_VisualElement == null)
                    return;

                List<TextElement> textElements = widget.m_VisualElement.Query()
                        .Descendents<TextElement>(classname: "debug-window-search-filter-target")
                        .ToList();

                string aggregatedText = CollectAggregatedAdditionalSearchText(widget);
                m_WidgetSearchElementCache[widget] = new WidgetSearchData(textElements, aggregatedText);
            });
        }

        void InitializeSearchField()
        {
            m_SearchFilter = new UIElementSearchFilter(
                rootVisualElement,
                searchString =>
                {
                    var visiblePanels = PerformSearch(m_WidgetSearchElementCache, searchString, hideRootElementIfNoMatch: true);

                    foreach (var headerLabel in m_PanelHeaderTextElements)
                        UIElementSearchFilter.ApplySearchAndHighlight(headerLabel, searchString, out _);

                    foreach (var panel in DebugManager.instance.panels)
                    {
                        var tab = m_LeftPaneElement.Q<VisualElement>(name: panel.displayName + "_Tab");
                        if (tab != null)
                        {
                            bool shouldShow = string.IsNullOrEmpty(searchString) || visiblePanels.Contains(panel.displayName);
                            tab.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                    }
                }
            );
            m_SearchFilter.InitializeSearchField("search-field");
        }

        internal static string CollectAggregatedAdditionalSearchText(DebugUI.Widget widget)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(widget.m_AdditionalSearchText))
                parts.Add(widget.m_AdditionalSearchText);

            if (widget is DebugUI.Container container)
            {
                foreach (var child in container.children)
                {
                    string childAggregated = CollectAggregatedAdditionalSearchText(child);
                    if (!string.IsNullOrEmpty(childAggregated))
                        parts.Add(childAggregated);
                }
            }

            return parts.Count > 0 ? string.Join(",", parts) : string.Empty;
        }

        internal static HashSet<string> PerformSearch(Dictionary<DebugUI.Widget, WidgetSearchData> elementCache, string searchString, bool hideRootElementIfNoMatch = false)
        {
            var panelsWithVisibleWidgets = new HashSet<string>();

            foreach (var (widget, searchData) in elementCache)
            {
                bool anyDescendantMatchesSearch = false;
                foreach (var elem in searchData.textElements)
                {
                    UIElementSearchFilter.ApplySearchAndHighlight(elem, searchString, out bool matched);
                    if (matched)
                        anyDescendantMatchesSearch = true;
                }

                if (!string.IsNullOrEmpty(searchData.aggregatedAdditionalSearchText))
                {
                    if (searchData.aggregatedAdditionalSearchText.Contains(searchString, StringComparison.CurrentCultureIgnoreCase))
                        anyDescendantMatchesSearch = true;
                }

                if (hideRootElementIfNoMatch)
                {
                    if (searchString == string.Empty && searchData.textElements.Count == 0)
                    {
                        widget.m_IsHiddenBySearchFilter = false;
                    }
                    else
                    {
                        widget.m_IsHiddenBySearchFilter = !anyDescendantMatchesSearch;
                    }
                }
                else
                {
                    widget.m_IsHiddenBySearchFilter = false;
                }

                if (!widget.m_IsHiddenBySearchFilter && widget.panel != null)
                    panelsWithVisibleWidgets.Add(widget.panel.displayName);
            }

            return panelsWithVisibleWidgets;
        }
#endif
    }
}
