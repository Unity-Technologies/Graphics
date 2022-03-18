using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Searcher.Tests
{
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    class SearcherPreferencesTests
    {
        List<string> m_FavoritePrefsToDelete;

        [SetUp]
        public virtual void SetUp()
        {
            m_FavoritePrefsToDelete = new List<string>();
        }

        [TearDown]
        public virtual void TearDown()
        {
            foreach (var prefKey in m_FavoritePrefsToDelete)
            {
                EditorPrefs.DeleteKey(prefKey);
            }
            SearcherPreferences.InvalidateCache();
        }

        /// <summary>
        /// Create a searcher for tests that registers its preferences to get deleted after tests.
        /// </summary>
        /// <param name="toolName">If null will use SearcherTests</param>
        /// <param name="context">If null will use SearcherTestsContext</param>
        /// <returns>A test Searcher with a few items.</returns>
        Searcher MakeSearcher(string toolName = null, string context = null)
        {
            var db = new SearcherDatabase(new List<SearcherItem>
            {
                new SearcherItem("child0") { CategoryPath = "parent0" },
                new SearcherItem("child1") { CategoryPath = "parent0" },
                new SearcherItem("child2") { CategoryPath = "parent0" },
                new SearcherItem("child0") { CategoryPath = "parent1" },
                new SearcherItem("child1") { CategoryPath = "parent1" },
                new SearcherItem("child2") { CategoryPath = "parent1" }
            });
            var searcher = new Searcher(db, "Test Searcher", toolName ?? "SearcherTests", context ?? "SearcherTestsContext");

            if (!m_FavoritePrefsToDelete.Contains(searcher.Preferences.PreferenceKey))
                m_FavoritePrefsToDelete.Add(searcher.Preferences.PreferenceKey);

            return searcher;
        }

        [Test]
        public void TestPreferencesSerialization()
        {
            var searcher = MakeSearcher();
            searcher.Preferences.SetFavorite("favorite1", true);
            searcher.Preferences.SetString("stringValue", "myString");
            searcher.Preferences.SetString("stringEmpty", "");
            searcher.Preferences.SetBool("boolTrueValue", true);
            searcher.Preferences.SetBool("boolFalseValue", false);
            searcher.Preferences.SetInt("int42Value", 42);
            searcher.Preferences.SetInt("int0Value", 0);

            var data = SearcherPreferences.RetrievePrefs(searcher.Preferences.PreferenceKey);
            Assert.That(data.favoritesPerContext.Count, Is.EqualTo(1));
            Assert.That(data.favoritesPerContext[0].key, Is.EqualTo(searcher.Preferences.Context));
            Assert.That(data.favoritesPerContext[0].value.Count, Is.EqualTo(1));
            Assert.That(data.favoritesPerContext[0].value[0], Is.EqualTo("favorite1"));
            Assert.That(data.stringPrefs.Count, Is.EqualTo(2));
            Assert.That(data.stringPrefs[0].key, Is.EqualTo("stringValue"));
            Assert.That(data.stringPrefs[0].value, Is.EqualTo("myString"));
            Assert.That(data.stringPrefs[1].key, Is.EqualTo("stringEmpty"));
            Assert.That(data.stringPrefs[1].value, Is.EqualTo(""));
            Assert.That(data.boolPrefs.Count, Is.EqualTo(2));
            Assert.That(data.boolPrefs[0].key, Is.EqualTo("boolTrueValue"));
            Assert.That(data.boolPrefs[0].value, Is.EqualTo(true));
            Assert.That(data.boolPrefs[1].key, Is.EqualTo("boolFalseValue"));
            Assert.That(data.boolPrefs[1].value, Is.EqualTo(false));
            Assert.That(data.intPrefs.Count, Is.EqualTo(2));
            Assert.That(data.intPrefs[0].key, Is.EqualTo("int42Value"));
            Assert.That(data.intPrefs[0].value, Is.EqualTo(42));
            Assert.That(data.intPrefs[1].key, Is.EqualTo("int0Value"));
            Assert.That(data.intPrefs[1].value, Is.EqualTo(0));

            searcher.Preferences.SetFavorite("favorite1", false);
            searcher.Preferences.SetFavorite("favorite2", true);
            searcher.Preferences.SetFavorite("favorite3", true);

            data = SearcherPreferences.RetrievePrefs(searcher.Preferences.PreferenceKey);
            Assert.That(data.favoritesPerContext.Count, Is.EqualTo(1));
            Assert.That(data.favoritesPerContext[0].key, Is.EqualTo(searcher.Preferences.Context));
            Assert.That(data.favoritesPerContext[0].value.Count, Is.EqualTo(2));
            Assert.That(data.favoritesPerContext[0].value[0], Is.EqualTo("favorite2"));
            Assert.That(data.favoritesPerContext[0].value[1], Is.EqualTo("favorite3"));
        }

        [Test]
        public void TestSetUnsetFavorite()
        {
            var searcher = MakeSearcher();
            var itemNameToSearch = "parent0 child1";
            var foundItem = searcher.Search(itemNameToSearch).First();
            Assert.That(foundItem.SearchableFullName, Is.EqualTo(itemNameToSearch));

            Assert.That(foundItem.SearchableFullName == itemNameToSearch);
            Assert.That(searcher.IsFavorite(foundItem), Is.False);

            searcher.SetFavorite(foundItem);
            Assert.That(searcher.IsFavorite(foundItem), Is.True);

            var otherItem = searcher.Search("parent1 child2").First();
            Assert.That(searcher.IsFavorite(otherItem), Is.False);

            searcher.SetFavorite(foundItem, false);
            Assert.That(searcher.IsFavorite(foundItem), Is.False);
        }

        [Test]
        public void TestClearFavorites()
        {
            var searcher = MakeSearcher();
            var itemNameToSearch = "parent0 child1";
            var itemName2 = "parent1 child2";
            var foundItem = searcher.Search(itemNameToSearch).First();
            var foundItem2 = searcher.Search(itemName2).First();
            Assert.That(searcher.IsFavorite(foundItem), Is.False);
            Assert.That(searcher.IsFavorite(foundItem2), Is.False);
            searcher.SetFavorite(foundItem);
            searcher.SetFavorite(foundItem2);
            Assert.That(searcher.IsFavorite(foundItem), Is.True);
            Assert.That(searcher.IsFavorite(foundItem2), Is.True);
            searcher.ClearFavorites();
            Assert.That(searcher.IsFavorite(foundItem), Is.False);
            Assert.That(searcher.IsFavorite(foundItem2), Is.False);
        }

        [Test]
        public void TestClearFavoritesDoesntClearOtherContexts()
        {
            var searcher = MakeSearcher();
            var itemNameToSearch = "parent0 child1";
            var foundItem = searcher.Search(itemNameToSearch).First();
            Assert.That(searcher.IsFavorite(foundItem), Is.False);
            searcher.SetFavorite(foundItem);
            Assert.That(searcher.IsFavorite(foundItem), Is.True);

            var searcher2 = MakeSearcher(context: "TestOtherContext");
            var itemName2 = "parent1 child2";
            var foundItem2 = searcher2.Search(itemName2).First();
            Assert.That(searcher2.IsFavorite(foundItem2), Is.False);
            searcher2.SetFavorite(foundItem2);
            Assert.That(searcher2.IsFavorite(foundItem2), Is.True);

            searcher.ClearFavorites();
            Assert.That(searcher.IsFavorite(foundItem), Is.False);
            Assert.That(searcher2.IsFavorite(foundItem2), Is.True);
        }

        [Test]
        public void TestClearFavoritesDoesntClearOtherTools()
        {
            var searcher = MakeSearcher();
            var itemNameToSearch = "parent0 child1";
            var foundItem = searcher.Search(itemNameToSearch).First();
            Assert.That(searcher.IsFavorite(foundItem), Is.False);
            searcher.SetFavorite(foundItem);
            Assert.That(searcher.IsFavorite(foundItem), Is.True);

            var searcher2 = MakeSearcher(toolName: "TestOtherTool");
            var itemName2 = "parent1 child2";
            var foundItem2 = searcher2.Search(itemName2).First();
            Assert.That(searcher2.IsFavorite(foundItem2), Is.False);
            searcher2.SetFavorite(foundItem2);
            Assert.That(searcher2.IsFavorite(foundItem2), Is.True);

            searcher.ClearFavorites();
            Assert.That(searcher.IsFavorite(foundItem), Is.False);
            Assert.That(searcher2.IsFavorite(foundItem2), Is.True);
        }

        [Test]
        public void TestFavoriteExistsInNewInstance()
        {
            var searcher0 = MakeSearcher();
            var itemNameToSearch = "parent0 child1";
            var result0 = searcher0.Search(itemNameToSearch).First();

            var searcher1 = MakeSearcher();
            var result1 = searcher1.Search(itemNameToSearch).First();

            Assert.That(result0.CategoryPath == result1.CategoryPath);

            searcher0.SetFavorite(result0);
            Assert.That(searcher0.IsFavorite(result0), Is.True);
            Assert.That(searcher1.IsFavorite(result1), Is.True);

            searcher1.SetFavorite(result1, false);
            Assert.That(searcher0.IsFavorite(result0), Is.False);
            Assert.That(searcher1.IsFavorite(result1), Is.False);
        }
    }
}
