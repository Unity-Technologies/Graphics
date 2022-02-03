using System.Linq;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Searcher.Tests
{
    class SearcherDatabaseTest
    {
        [Test]
        public void TestSearchInEmptyDatabase()
        {
            var searcher = new Searcher(new SearcherDatabase(), "testSearcher");
            Assert.DoesNotThrow(() => Assert.AreEqual(searcher.Search("Japanese").ToList().Count, 0));
        }
    }
}
