using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// ReSharper disable StringLiteralTypo

namespace UnityEditor.GraphToolsFoundation.Searcher.Tests
{
    abstract class SearcherTestsBase
    {
        protected Searcher m_Searcher;

        [OneTimeSetUp]
        public void Init()
        {
            SearcherDatabaseBase bookDatabase = Create(SearcherTestsData.BookItems);
            SearcherDatabaseBase foodDatabase = Create(SearcherTestsData.FoodItems);

            m_Searcher = new Searcher(new[] { foodDatabase, bookDatabase }, "Popup Example", searcherName: "SearcherPopupExample");
        }

        protected abstract SearcherDatabaseBase Create(IReadOnlyList<SearcherItem> items);

        [OneTimeTearDown]
        public void Cleanup()
        {
        }
    }

    class SearcherTests : SearcherTestsBase
    {
        protected override SearcherDatabaseBase Create(IReadOnlyList<SearcherItem> items)
        {
            return new SearcherDatabase(items);
        }

        public static IEnumerable<object[]> SingleTermCases()
        {
            yield return new object[] { "Japanese", 1 };
            // VladN: disable these test cases on obsolete database as Searcher behavior is different
            if (!SearcherDatabase.IsOldDatabaseWithoutQuickSearch)
            {
                yield return new object[] { "books", 9 };
                yield return new object[] { "vg", 9 };
            }
        }

        public static IEnumerable<object[]> MultipleTermCases()
        {
            yield return new object[] { "The Time Machine", 1 };
            // VladN: disable these test cases on obsolete database as Searcher behavior is different
            if (!SearcherDatabase.IsOldDatabaseWithoutQuickSearch)
            {
                yield return new object[] { "Books Cook", 5 };
                yield return new object[] { "Food Vegetables Lett", 4 };
            }
        }

        [TestCaseSource(nameof(SingleTermCases))]
        [TestCaseSource(nameof(MultipleTermCases))]
        public void TestSearchingTerms(string term, int expectedResultCount)
        {
            Assert.IsTrue(term != null, "Term must not be null");

            var items = m_Searcher.Search(term).ToList();
            foreach (var item in items)
            {
                Debug.Log(item.Name);
            }

            Assert.AreEqual(expectedResultCount, items.Count,
                "Term '" + term + "' must match at least one data stub.");
        }
    }
}
