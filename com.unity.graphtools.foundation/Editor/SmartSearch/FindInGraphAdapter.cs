using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Searcher allowing to highlight search results in a graph.
    /// </summary>
    public class FindInGraphAdapter : SimpleSearcherAdapter
    {
        readonly Action<FindSearcherItem> m_OnHighlightDelegate;

        public class FindSearcherItem : SearcherItem
        {
            public FindSearcherItem(string name, INodeModel node)
                :base(name)
            {
                Node = node;
            }

            public INodeModel Node { get; }
        }

        /// <summary>
        /// Initializes a new instance of the FindInGraphAdapter class.
        /// </summary>
        /// <param name="onHighlightDelegate">Delegate called to highlight matching items.</param>
        public FindInGraphAdapter(Action<FindSearcherItem> onHighlightDelegate)
            : base("Find in graph")
        {
            m_OnHighlightDelegate = onHighlightDelegate;
        }

        /// <inheritdoc />
        public override void OnSelectionChanged(IEnumerable<SearcherItem> items)
        {
            var selectedItems = items.ToList();

            if (selectedItems.Count > 0 && selectedItems[0] is FindSearcherItem fsi)
                m_OnHighlightDelegate(fsi);

            base.OnSelectionChanged(selectedItems);
        }
    }
}
