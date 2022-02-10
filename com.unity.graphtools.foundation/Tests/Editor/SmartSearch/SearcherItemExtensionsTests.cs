using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.SmartSearch
{
    sealed class SearcherItemExtensionsTests
    {
        [Test]
        public void TestFind_NotNull()
        {
            var parent = new SearcherItem("parent");
            parent.AddChild(new SearcherItem("child"));

            var child = parent.Find("child");

            Assert.NotNull(child);
            Assert.AreEqual(child.Name, "child");
        }

        [Test]
        public void TestFind_Null()
        {
            var parent = new SearcherItem("parent");
            var child = parent.Find("child");

            Assert.IsNull(child);
        }

        [Test]
        public void TestFind_Recursive()
        {
            var parent = new SearcherItem("parent", "", new List<SearcherItem>
            {
                new SearcherItem("a", "", new List<SearcherItem>
                {
                    new SearcherItem("aa1"),
                    new SearcherItem("aa2")
                }),
                new SearcherItem("b", "", new List<SearcherItem>
                {
                    new SearcherItem("bb1"),
                    new SearcherItem("bb2")
                })
            });

            var found = parent.Find("bb1");
            Assert.NotNull(found);
            Assert.AreEqual(found.Name, "bb1");
        }
    }
}
