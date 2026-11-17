using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // The node searcher's adapter, which sets the Locals category apart.
    class LocalsSearchWindowAdapter : SearchWindowAdapter
    {
        internal LocalsSearchWindowAdapter(string title) : base(title)
        {
        }

        public override VisualElement Bind(VisualElement element, SearcherItem item, ItemExpanderState expanderState, string query)
        {
            var expander = base.Bind(element, item, expanderState, query);
            StyleLocalsCategory(element, item);
            return expander;
        }

        // Rows are reused, so every row sets the style.
        static void StyleLocalsCategory(VisualElement row, SearcherItem item)
        {
            var labels = row.Q<VisualElement>("labelsContainer");
            if (labels == null)
                return;
            bool isLocals = item.Depth == 0 && item.Name == LocalProviderSearchEntries.kCategory;
            labels.style.unityFontStyleAndWeight = isLocals ? new StyleEnum<FontStyle>(FontStyle.Italic) : new StyleEnum<FontStyle>(StyleKeyword.Null);
        }
    }
}
