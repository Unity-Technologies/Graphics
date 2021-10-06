using System.Reflection;

using NUnit.Framework;

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

            var searchViewStateType = typeof(Search.SearchService).Assembly.GetType("UnityEditor.Search.SearchViewState");
            var flagsInfo = searchViewStateType.GetField("flags", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(flagsInfo);
        }
    }
}
