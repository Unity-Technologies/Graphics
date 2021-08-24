using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Searcher;

namespace UnityEditor.ShaderGraph
{
    public class SearchWindowAdapter : SearcherAdapter
    {
        readonly VisualTreeAsset m_DefaultItemTemplate;
        public override bool HasDetailsPanel => false;

        public SearchWindowAdapter(string title) : base(title)
        {
            m_DefaultItemTemplate = Resources.Load<VisualTreeAsset>("SearcherItem");
        }

        public override SearcherItem OnSearchResultsFilter(IEnumerable<SearcherItem> searchResults, string searchQuery)
        {
            // Sort results by length so that shorter length results are prioritized
            // prevents entries with short names getting stuck at end of list after entries with longer names when both contain the same word
            searchResults = searchResults.OrderBy(x => x.Name.Length).ToList();

            var scrollToItem = searchResults.First();

            if (scrollToItem.Children.Count != 0)
            {
                SearcherItem childIterator = null;
                // Discard searcher item for selection if it is a category, get next best child item from it instead
                // There is no utility in selecting category headers/titles, only the leaf entries
                childIterator = scrollToItem.Children[0];
                while (childIterator != null && childIterator.Children.Count != 0)
                {
                    childIterator = childIterator.Children[0];
                }

                scrollToItem = childIterator;
            }

            if (searchQuery.Length != 0 && scrollToItem.Parent != null)
            {
                var queryTerms = searchQuery.Split(' ');

                SearcherItem parentItem = scrollToItem.Parent;
                foreach (var word in queryTerms)
                {
                    // Do a check for an exact match within this last tier of search results
                    foreach (var exactMatchItem in parentItem.Children)
                    {
                        // Split the entry name so that we can remove suffix that looks like "Clamp: In(4)"
                        var nameSansSuffix = exactMatchItem.Name.Split(':');
                        if (nameSansSuffix[0].Equals(word, StringComparison.OrdinalIgnoreCase))
                        {
                            scrollToItem = exactMatchItem;
                            break;
                        }
                    }
                }
            }

            return scrollToItem;
        }
    }

    internal class SearchNodeItem : SearcherItem
    {
        public NodeEntry NodeGUID;

        public SearchNodeItem(string name, NodeEntry nodeGUID, string[] synonyms,
                              string help = " ", List<SearchNodeItem> children = null) : base(name)
        {
            NodeGUID = nodeGUID;
            Synonyms = synonyms;
        }
    }
}
