#if !QUICKSEARCH_3_0_0_OR_NEWER && !USE_SEARCH_MODULE && !USE_QUICK_SEARCH_MODULE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Searcher Database using custom search.
    /// Deprecated, please use UnitySearcherDatabase.
    /// </summary>
    [PublicAPI, Obsolete("Not using quick search, please add package com.unity.quicksearch >= 3.0.0 or use a more recent editor.")]
    public class SearcherDatabase : SearcherDatabaseBase
    {
        /// <summary>
        /// Used in tests because the older database doesn't behave quite the same
        /// </summary>
        internal static bool IsOldDatabaseWithoutQuickSearch => true;
        Dictionary<string, IReadOnlyList<ValueTuple<string, float>>> m_Index = new Dictionary<string, IReadOnlyList<ValueTuple<string, float>>>();

        static readonly float k_ScoreCutOff = 0.33f;

        class Result
        {
            public SearcherItem item;
            public float maxScore;
        }

        public Func<SearcherItem, bool> MatchFilter { get; set; }

        public SearcherDatabase()
        {
        }

        public SearcherDatabase(IReadOnlyList<SearcherItem> db)
            : base(db)
        {
        }

        /// <summary>
        /// Creates a database from a serialized file.
        /// </summary>
        /// <param name="directory">Path of the directory where the database is stored.</param>
        /// <returns>A Database with items retrieved from the serialized file.</returns>
        public static SearcherDatabase FromFile(string directory)
        {
            var db = new SearcherDatabase();
            db.LoadFromFile(directory);
            return db;
        }

        public override IEnumerable<SearcherItem> PerformSearch(string query,
            IReadOnlyList<SearcherItem> filteredItems)
        {
            // Match assumes the query is trimmed
            query = query.Trim(' ', '\t');

            if (string.IsNullOrWhiteSpace(query))
            {
                return filteredItems.ToList();
            }

            var results = new List<SearcherItem>(filteredItems.Count) { null };
            var max = new Result();
            var tokenizedQuery = new List<string>();
            foreach (var token in Tokenize(query))
            {
                tokenizedQuery.Add(token.Trim().ToLower());
            }

            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            if (k_UseParallelTasks && filteredItems.Count > 100)
                SearchMultithreaded(query, filteredItems, tokenizedQuery, max, results);
            else
                SearchSingleThreaded(query, filteredItems, tokenizedQuery, max, results);

            if (max.item != null)
                results[0] = max.item;
            else
                results.RemoveAt(0);
            return results;
        }

        protected virtual bool Match(string query, IReadOnlyList<string> tokenizedQuery, SearcherItem item, out float score)
        {
            var filter = MatchFilter?.Invoke(item) ?? true;
            return Match(tokenizedQuery, item, out score) && filter;
        }

        void SearchSingleThreaded(string query, IReadOnlyList<SearcherItem> items,
            IReadOnlyList<string> tokenizedQuery, Result max, ICollection<SearcherItem> finalResults)
        {
            List<Result> results = new List<Result>();

            foreach (var item in items)
            {
                float score = 0;
                if (query.Length == 0 || Match(query, tokenizedQuery, item, out score))
                {
                    if (score > max.maxScore)
                    {
                        max.item = item;
                        max.maxScore = score;
                    }
                    results.Add(new Result() { item = item, maxScore = score });
                }
            }

            PostprocessResults(results, finalResults, max);
        }

        void SearchMultithreaded(string query, IReadOnlyList<SearcherItem> items,
            IReadOnlyList<string> tokenizedQuery, Result max, List<SearcherItem> finalResults)
        {
            var count = Environment.ProcessorCount;
            var tasks = new Task[count];
            var localResults = new Result[count];
            var queue = new ConcurrentQueue<Result>();
            var itemsPerTask = (int)Math.Ceiling(items.Count / (float)count);

            for (var i = 0; i < count; i++)
            {
                var i1 = i;
                localResults[i1] = new Result();
                tasks[i] = Task.Run(() =>
                {
                    var result = localResults[i1];
                    for (var j = 0; j < itemsPerTask; j++)
                    {
                        var index = j + itemsPerTask * i1;
                        if (index >= items.Count)
                            break;
                        var item = items[index];
                        float score = 0;
                        if (query.Length == 0 || Match(query, tokenizedQuery, item, out score))
                        {
                            if (score > result.maxScore)
                            {
                                result.maxScore = score;
                                result.item = item;
                            }

                            queue.Enqueue(new Result { item = item, maxScore = score });
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            for (var i = 0; i < count; i++)
            {
                if (localResults[i].maxScore > max.maxScore)
                {
                    max.maxScore = localResults[i].maxScore;
                    max.item = localResults[i].item;
                }
            }

            PostprocessResults(queue, finalResults, max);
        }

        void PostprocessResults(IEnumerable<Result> results, ICollection<SearcherItem> items, Result max)
        {
            foreach (var result in results)
            {
                var normalizedScore = result.maxScore / max.maxScore;
                if (result.item != null && result.item != max.item && normalizedScore > k_ScoreCutOff)
                {
                    items.Add(result.item);
                }
            }
        }

        public override List<SearcherItem> PerformIndex(IEnumerable<SearcherItem> itemsToIndex, int estimateIndexSize = -1)
        {
            m_Index.Clear();

            var result = base.PerformIndex(itemsToIndex, estimateIndexSize);
            foreach (var item in result)
            {
                if (!m_Index.ContainsKey(item.FullName))
                {
                    List<ValueTuple<string, float>> terms = new List<ValueTuple<string, float>>();

                    // If the item uses synonyms to return results for similar words/phrases, add them to the search terms
                    IList<string> tokens = null;
                    if (item.Synonyms == null)
                        tokens = Tokenize(item.Name);
                    else
                        tokens = Tokenize(string.Format("{0} {1}", item.Name, string.Join(" ", item.Synonyms)));

                    string tokenSuite = "";
                    foreach (var token in tokens)
                    {
                        var t = token.ToLower();
                        if (t.Length > 1)
                        {
                            terms.Add(new ValueTuple<string, float>(t, 0.8f));
                        }

                        if (tokenSuite.Length > 0)
                        {
                            tokenSuite += " " + t;
                            terms.Add(new ValueTuple<string, float>(tokenSuite, 1f));
                        }
                        else
                        {
                            tokenSuite = t;
                        }
                    }

                    // Add a term containing all the uppercase letters (CamelCase World BBox => CCWBB)
                    var initialList = Regex.Split(item.Name, @"\P{Lu}+");
                    var initials = string.Concat(initialList).Trim();
                    if (!string.IsNullOrEmpty(initials))
                        terms.Add(new ValueTuple<string, float>(initials.ToLower(), 0.5f));

                    m_Index.Add(item.FullName, terms);
                }
            }

            return result;
        }

        static IList<string> Tokenize(string s)
        {
            var knownTokens = new HashSet<string>();
            var tokens = new List<string>();

            // Split on word boundaries
            foreach (var t in Regex.Split(s, @"\W"))
            {
                // Split camel case words
                var tt = Regex.Split(t, @"(\p{Lu}+\P{Lu}*)");
                foreach (var ttt in tt)
                {
                    var tttt = ttt.Trim();
                    if (!string.IsNullOrEmpty(tttt) && !knownTokens.Contains(tttt))
                    {
                        knownTokens.Add(tttt);
                        tokens.Add(tttt);
                    }
                }
            }

            return tokens;
        }

        bool Match(IReadOnlyList<string> tokenizedQuery, SearcherItem item, out float score)
        {
            var itemPath = item.FullName.Trim();
            if (itemPath == "")
            {
                if (tokenizedQuery.Count == 0)
                {
                    score = 1;
                    return true;
                }
                else
                {
                    score = 0;
                    return false;
                }
            }

            IReadOnlyList<ValueTuple<string, float>> indexTerms;
            if (!m_Index.TryGetValue(itemPath, out indexTerms))
            {
                score = 0;
                return false;
            }

            float maxScore = 0.0f;
            foreach (var t in indexTerms)
            {
                float scoreForTerm = 0f;
                var querySuite = "";
                var querySuiteFactor = 1.25f;
                foreach (var q in tokenizedQuery)
                {
                    if (t.Item1.StartsWith(q))
                    {
                        scoreForTerm += t.Item2 * q.Length / t.Item1.Length;
                    }

                    if (querySuite.Length > 0)
                    {
                        querySuite += " " + q;
                        if (t.Item1.StartsWith(querySuite))
                        {
                            scoreForTerm += t.Item2 * querySuiteFactor * querySuite.Length / t.Item1.Length;
                        }
                    }
                    else
                    {
                        querySuite = q;
                    }

                    querySuiteFactor *= querySuiteFactor;
                }

                maxScore = Mathf.Max(maxScore, scoreForTerm);
            }

            score = maxScore;
            LastSearchData[item] = new SearchData { Score = (long)score };
            return score > 0;
        }
    }
}
#endif
