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

        private SearcherItem GetFirstChildItem(SearcherItem item)
        {
            if (item.Children.Count != 0)
            {
                SearcherItem childIterator = null;
                // Discard searcher item for selection if it is a category, get next best child item from it instead
                // There is no utility in selecting category headers/titles, only the leaf entries
                childIterator = item.Children[0];
                while (childIterator != null && childIterator.Children.Count != 0)
                {
                    childIterator = childIterator.Children[0];
                }

                item = childIterator;
            }

            return item;
        }

        private int ComputeScoreForMatch(string queryTerm, SearcherItem matchItem)
        {
            // Scoring Criteria:
            // - Exact name match is most preferred.
            // - Partial name match is next.
            // - Exact synonym match is next.
            // - Partial synonym match is next.
            // - No match is last.
            int score = 0;

            // Split the entry name so that we can remove suffix that looks like "Clamp: In(4)"
            var nameSansSuffix = matchItem.Name.Split(':');
            if (nameSansSuffix[0].Equals(queryTerm, StringComparison.OrdinalIgnoreCase))
            {
                score += 1000000;
            }
            else if (nameSansSuffix[0].Contains(queryTerm, StringComparison.OrdinalIgnoreCase))
            {
                score += 100000;
                score -= (nameSansSuffix[0].Length - queryTerm.Length);
            }

            if (matchItem.Synonyms == null)
                return score;

            // Check for synonym matches
            foreach (var syn in matchItem.Synonyms)
            {
                if (syn.Equals(queryTerm, StringComparison.OrdinalIgnoreCase))
                {
                    score += 10000;
                }
                else if (syn.Contains(queryTerm, StringComparison.OrdinalIgnoreCase))
                {
                    score += 1000;
                    score -= (syn.Length - queryTerm.Length);
                }
            }

            return score;
        }

        public override SearcherItem OnSearchResultsFilter(IEnumerable<SearcherItem> searchResults, string searchQuery)
        {
            if (searchQuery.Length == 0)
                return searchResults.FirstOrDefault();

            // Sort results by length so that shorter length results are prioritized
            // prevents entries with short names getting stuck at end of list after entries with longer names when both contain the same word
            searchResults = searchResults.OrderBy(x => x.Name.Length).ToList();

            var bestMatch = GetFirstChildItem(searchResults.FirstOrDefault());
            int bestScore = 0;
            List<int> visitedItems = new List<int>();
            var queryTerms = searchQuery.Split(' ');
            foreach (var result in searchResults)
            {
                var currentItem = GetFirstChildItem(result);

                if (currentItem.Parent != null)
                {
                    SearcherItem parentItem = currentItem.Parent;
                    foreach (var matchItem in parentItem.Children)
                    {
                        if (visitedItems.Contains(matchItem.Id))
                            continue;

                        foreach (var word in queryTerms)
                        {
                            int currentScore = ComputeScoreForMatch(word, matchItem);
                            if (currentScore > bestScore)
                            {
                                bestScore = currentScore;
                                bestMatch = matchItem;
                            }
                        }
                        visitedItems.Add(matchItem.Id);
                    }
                }
            }

            return bestMatch;
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
