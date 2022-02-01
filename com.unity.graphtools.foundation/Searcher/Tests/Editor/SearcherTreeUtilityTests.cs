using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Searcher.Tests
{
    class SearcherTreeUtilityTests
    {
        List<SearcherItem> m_SearchTree = new List<SearcherItem>();

        [OneTimeSetUp]
        public void Init()
        {
            List<SearcherItem> items = new List<SearcherItem>();
            items.Add(new SearcherItem("Fantasy/J. R. R. Tolkien/The Fellowship of the Ring"));
            items.Add(new SearcherItem("Fantasy/J. R. R. Tolkien/The Two Towers", userData: 5));
            items.Add(new SearcherItem("Fantasy/J. R. R. Tolkien/The Return of the King"));
            items.Add(new SearcherItem("Fantasy/Dragonlance/Dragons of Winter Night"));
            items.Add(new SearcherItem("Health & Fitness/Becoming a Supple Leopard"));
            items.Add(new SearcherItem("Some Uncategorized Book", userData: 2));

            m_SearchTree = SearcherTreeUtility.CreateFromFlatList(items);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
        }

        [Test]
        public void ValidateSearcherTreeUtilityTests()
        {
            Assert.AreEqual(3, m_SearchTree.Count);
            Assert.AreEqual(2, m_SearchTree[0].Children.Count);
            Assert.AreEqual(3, m_SearchTree[0].Children[0].Children.Count);
            Assert.AreEqual(1, m_SearchTree[0].Children[1].Children.Count);
            Assert.AreEqual(1, m_SearchTree[1].Children.Count);
            Assert.AreEqual(0, m_SearchTree[1].Children[0].Children.Count);
            Assert.AreEqual("Fantasy", m_SearchTree[0].Name);
            Assert.AreEqual("J. R. R. Tolkien", m_SearchTree[0].Children[0].Name);
            Assert.AreEqual("The Fellowship of the Ring", m_SearchTree[0].Children[0].Children[0].Name);
            Assert.AreEqual("The Two Towers", m_SearchTree[0].Children[0].Children[1].Name);
            Assert.AreEqual("The Return of the King", m_SearchTree[0].Children[0].Children[2].Name);
            Assert.AreEqual("Dragonlance", m_SearchTree[0].Children[1].Name);
            Assert.AreEqual("Dragons of Winter Night", m_SearchTree[0].Children[1].Children[0].Name);

            Assert.AreEqual("Health & Fitness", m_SearchTree[1].Name);
            Assert.AreEqual("Becoming a Supple Leopard", m_SearchTree[1].Children[0].Name);
            Assert.AreEqual("Some Uncategorized Book", m_SearchTree[2].Name);

            Assert.AreNotEqual("Fantasy", m_SearchTree[2].Name);
            Assert.AreNotEqual("Some Uncategorized Book", m_SearchTree[0].Children[0].Children[0].Name);

            // Change for User Data:
            Assert.AreEqual(5, m_SearchTree[0].Children[0].Children[1].UserData);
            Assert.AreEqual(2, m_SearchTree[2].UserData);
            Assert.AreEqual(null, m_SearchTree[0].UserData);
            Assert.AreEqual(null, m_SearchTree[0].Children[0].Children[2].UserData);
        }
    }
}
