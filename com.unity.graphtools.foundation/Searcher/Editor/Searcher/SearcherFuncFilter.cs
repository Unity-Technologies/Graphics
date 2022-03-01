using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Basic filter made of collections of functors
    /// </summary>
    public class SearcherFuncFilter : SearcherFilter
    {
        /// <summary>
        /// Empty filter, will not filter anything.
        /// </summary>
        public static SearcherFuncFilter Empty => new SearcherFuncFilter();

        protected List<Func<SearcherItem, bool>> m_FilterFunctions = new List<Func<SearcherItem, bool>>();

        /// <summary>
        /// Instantiates a filter with filtering functions.
        /// </summary>
        /// <param name="functions">Filtering functions that say whether to keep an item or not</param>
        public SearcherFuncFilter(params Func<SearcherItem, bool>[] functions)
        {
            m_FilterFunctions.AddRange(functions);
        }

        /// <summary>
        /// Add a filter functor to a filter in place.
        /// </summary>
        /// <param name="func">filter functor to add</param>
        /// <returns>The filter with the new functor added</returns>
        public SearcherFuncFilter WithFilter(Func<SearcherItem, bool> func)
        {
            m_FilterFunctions.Add(func);
            return this;
        }

        /// <inheritdoc />
        public override bool Match(SearcherItem item)
        {
            return m_FilterFunctions.All(f => f(item));
        }
    }
}
