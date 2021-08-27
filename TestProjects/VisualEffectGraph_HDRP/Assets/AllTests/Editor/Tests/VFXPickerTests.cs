using System.Reflection;

using NUnit.Framework;

using UnityEditor.Search;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXPickerTests
    {
        [Test]
        public void TestReflectionOnQuickSearch()
        {
            var quickSearchType = typeof(Search.SearchService).Assembly.GetType("UnityEditor.Search.QuickSearch");
            Assert.NotNull(quickSearchType);

            var viewStateInfo = quickSearchType?.GetProperty("viewState", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(viewStateInfo);

            var flagsInfo = typeof(SearchViewState).GetField("flags", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(flagsInfo);
        }
    }
}
