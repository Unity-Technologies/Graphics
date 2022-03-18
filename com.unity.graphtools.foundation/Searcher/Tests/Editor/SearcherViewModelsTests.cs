using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Searcher.Tests
{
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    class SearcherViewModelTests
    {
        static string Indent(int depth) => new String('.', depth * 2);
        static string PrintHierarchy(ISearcherCategoryView category, int depth = -1)
        {
            List<string> lines = new List<string>();
            if (depth >= 0)
                lines.Add(Indent(depth) + category.Name);

            foreach (var subCategory in category.SubCategories)
            {
                lines.Add(PrintHierarchy(subCategory, depth + 1));
            }

            foreach (var item in category.Items)
            {
                lines.Add(Indent(depth + 1) + item.Name);
            }
            return string.Join("\n", lines);
        }

        static object[] s_TestHierarchyCases =
        {
            new object[] { new SearcherItem[] {}, "" },
            new object[]
            {
                new [] {
                    "Food/Fruits/Apple"
                }.Select(s => new SearcherItem { FullName = s }).ToArray(),
                string.Join("\n", new [] {
                    "Food",
                    "..Fruits",
                    "....Apple"
                })
            },
            new object[]
            {
                new [] {
                    "Food/Fruits/Apple",
                    "Books/Phone book",
                    "Food/Vegetables/Carrot",
                    "Books/SF/Dune",
                    "Food/Chewing Gum",
                    "Food/Fruits/Banana"
                }.Select(s => new SearcherItem { FullName = s }).ToArray(),
                string.Join("\n", new [] {
                "Food",
                "..Fruits",
                "....Apple",
                "....Banana",
                "..Vegetables",
                "....Carrot",
                "..Chewing Gum",
                "Books",
                "..SF",
                "....Dune",
                "..Phone book"
                })
            }
        };

        [TestCaseSource(nameof(s_TestHierarchyCases))]
        public void TestBuildHierarchyFromItems(SearcherItem[] items, string expectedHierarchy)
        {
            var hierarchy = SearcherCategoryView.BuildViewModels(items, SearcherResultsViewMode.Hierarchy);
            Assert.That(PrintHierarchy(hierarchy), Is.EqualTo(expectedHierarchy));
        }

        static object[] s_TestFlatListCases =
        {
            new object[] { new SearcherItem[] {}, "" },
            new object[]
            {
                new [] {
                    "Food/Fruits/Apple"
                }.Select(s => new SearcherItem { FullName = s }).ToArray(),
                string.Join("\n", new [] {
                    "Apple"
                })
            },
            new object[]
            {
                new [] {
                    "Food/Fruits/Apple",
                    "Food/Fruits/Banana",
                    "Food/Vegetables/Carrot",
                    "Food/Chewing Gum",
                    "Books/SF/Dune",
                    "Books/Phone book"
                }.Select(s => new SearcherItem { FullName = s }).ToArray(),
                string.Join("\n", new [] {
                    "Apple",
                    "Banana",
                    "Carrot",
                    "Chewing Gum",
                    "Dune",
                    "Phone book"
                })
            }
        };

        [TestCaseSource(nameof(s_TestFlatListCases))]
        public void TestBuildFlatListFromItems(SearcherItem[] items, string expectedHierarchy)
        {
            var hierarchy = SearcherCategoryView.BuildViewModels(items, SearcherResultsViewMode.Flat);
            Assert.That(PrintHierarchy(hierarchy), Is.EqualTo(expectedHierarchy));
        }
    }
}
