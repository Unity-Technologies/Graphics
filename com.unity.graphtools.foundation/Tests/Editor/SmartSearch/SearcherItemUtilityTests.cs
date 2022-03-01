using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.SmartSearch
{
    sealed class SearcherItemUtilityTests
    {
        [TestCase("foo")]
        [TestCase("/foo")]
        [TestCase("foo/")]
        [TestCase("/foo/")]
        public void TestGetItemFromPath_OneLevel(string path)
        {
            var items = new List<SearcherItem>();
            var foo = items.GetItemFromPath(path);

            Assert.NotNull(foo);
            Assert.AreEqual(foo.Name, "foo");
        }

        [TestCase("foo/bar/child")]
        [TestCase("foo/bar/child/")]
        [TestCase("/foo/bar/child")]
        [TestCase("/foo/bar/child/")]
        public void TestGetItemFromPath_DeepLevel_NotCreated(string path)
        {
            var items = new List<SearcherItem>();
            var child = items.GetItemFromPath(path);

            Assert.NotNull(child);

            var foo = items[0];
            Assert.NotNull(foo);
            Assert.AreEqual(foo.Name, "foo");

            var bar = foo.Children[0];
            Assert.NotNull(bar);
            Assert.AreEqual(bar.Name, "bar");
            Assert.AreSame(bar.Parent, foo);

            Assert.AreEqual(bar.Children[0].Name, "child");
            Assert.AreSame(bar.Children[0], child);
            Assert.AreSame(bar.Children[0].Parent, bar);
        }

        [TestCase("foo/bar")]
        [TestCase("foo/bar/")]
        [TestCase("/foo/bar")]
        [TestCase("/foo/bar/")]
        public void TestGetItemFromPath_DeepLevel_AlreadyCreated(string path)
        {
            var items = new List<SearcherItem>
            {
                new SearcherItem("foo", "",
                    new List<SearcherItem>
                    {
                        new SearcherItem("bar", "",
                            new List<SearcherItem> { new SearcherItem("child") })
                    })
            };

            var bar = items.GetItemFromPath(path);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("foo", items[0].Name);
            Assert.AreEqual(1, items[0].Children.Count);

            Assert.NotNull(bar);
            Assert.AreEqual("bar", bar.Name);
            Assert.AreEqual(1, bar.Children.Count);

            Assert.AreEqual("child", bar.Children[0].Name);
        }
    }
}
