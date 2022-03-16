using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Searcher.Tests
{
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    class SearcherItemTests
    {
        [TestCase(null, "")]
        [TestCase(null, "Apple")]
        [TestCase("", null)]
        [TestCase("Apple", null)]
        [TestCase(null, null)]
        public void TestSearcherItemNamePathFullNameAreNeverNull(string path, string name)
        {
            var item = new SearcherItem(name) { CategoryPath = path };
            Assert.That(item.Name, Is.Not.Null);
            Assert.That(item.CategoryPath, Is.Not.Null);
            Assert.That(item.FullName, Is.Not.Null);
        }

        [TestCase("", "", "")]
        [TestCase("", "Apple", "Apple")]
        [TestCase("Food/Fruits", "", "Food/Fruits")]
        [TestCase("Food/Fruits", "Apple", "Food/Fruits/Apple")]
        public void TestSearcherItemFullName(string path, string name, string expectedFullName)
        {
            var item = new SearcherItem(name) { CategoryPath = path };
            Assert.That(item.FullName, Is.EqualTo(expectedFullName));
        }

        [TestCase("", "", "")]
        [TestCase("", "Apple", "Apple")]
        [TestCase("Food/Fruits", "", "Food Fruits")]
        [TestCase("Food/Fruits", "Apple", "Food Fruits Apple")]
        public void TestSearcherItemSearchableFullName(string path, string name, string expectedFullName)
        {
            var item = new SearcherItem(name) { CategoryPath = path };
            Assert.That(item.SearchableFullName, Is.EqualTo(expectedFullName));
        }

        [TestCase("", "", "")]
        [TestCase("Apple", "", "Apple")]
        [TestCase("Food/Fruits", "Food", "Fruits")]
        [TestCase("Food/Fruits/", "Food/Fruits", "")]
        [TestCase("Food/Fruits/Apple", "Food/Fruits", "Apple")]
        public void TestSearcherItemCreatedByFullName(string fullName, string expectedPath, string expectedName)
        {
            var item = new SearcherItem { FullName = fullName };
            Assert.That(item.Name, Is.EqualTo(expectedName));
            Assert.That(item.CategoryPath, Is.EqualTo(expectedPath));
        }
    }
}
