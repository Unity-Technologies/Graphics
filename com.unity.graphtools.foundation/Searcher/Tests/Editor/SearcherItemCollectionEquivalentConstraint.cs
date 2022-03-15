using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework.Constraints;

namespace UnityEditor.GraphToolsFoundation.Searcher.Tests
{
    [PublicAPI]
    class Is : NUnit.Framework.Is
    {
        public static SearcherItemCollectionEquivalentConstraint SearcherItemCollectionEquivalent(IEnumerable<SearcherItem> expected)
        {
            return new SearcherItemCollectionEquivalentConstraint(expected);
        }
    }

    class SearcherItemCollectionEquivalentConstraint : CollectionItemsEqualConstraint
    {
        readonly List<SearcherItem> m_Expected;

        public SearcherItemCollectionEquivalentConstraint(IEnumerable<SearcherItem> expected)
            : base(expected)
        {
            m_Expected = expected.ToList();
        }

        protected override bool Matches(IEnumerable actual)
        {
            if (m_Expected == null)
            {
                Description = "Expected is not a valid collection";
                return false;
            }

            if (!(actual is IEnumerable<SearcherItem> actualCollection))
            {
                Description = "Actual is not a valid collection";
                return false;
            }

            var actualList = actualCollection.ToList();
            if (actualList.Count != m_Expected.Count)
            {
                Description = $"Collections lengths are not equal. \nExpected length: {m_Expected.Count}, " +
                    $"\nBut was: {actualList.Count}";
                return false;
            }

            for (var i = 0; i < m_Expected.Count; ++i)
            {
                var res1 = m_Expected[i].Name;
                var res2 = actualList[i].Name;
                if (!string.Equals(res1, res2))
                {
                    Description = $"Object at index {i} are not the same.\nExpected: {res1},\nBut was: {res2}";
                    return false;
                }
            }

            return true;
        }
    }
}
