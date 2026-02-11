using System.Collections;
using System.Collections.Generic;
using System;

using UnityEditor.Search;
using UnityEngine;

using NUnit.Framework;

namespace UnityEditor.ShaderGraph.UnitTests
{
    internal static class CustomIndexerUtilities
    {
        internal static System.Type GetSearchDataBaseType()
        {
            var t = typeof(ObjectIndexer).Assembly.GetType("UnityEditor.Search.SearchDatabase");
            Assert.NotNull(t, "Cannot access UnityEditor.Search.SearchDatabase type by reflection.");
            return t;
        }

        internal static System.Type GetAssetProviderType()
        {
            var t = typeof(ObjectIndexer).Assembly.GetType("UnityEditor.Search.Providers.AssetProvider");
            Assert.NotNull(t, "Cannot access UnityEditor.Search.Providers.AssetProvider type by reflection.");
            return t;
        }

        internal static List<string> GetResultPaths(IEnumerable<SearchResult> results)
        {
            var getAssetPathFunction = GetAssetProviderType().GetMethod("GetAssetPath", new[] { typeof(string) });
            Assert.NotNull(getAssetPathFunction, "Cannot access AssetProvider.GetAssetPath type by reflection.");
            var paths = new List<string>();
            foreach (var item in results)
            {
                var path = (string)getAssetPathFunction.Invoke(null, new object[] { item.id });
                if (!paths.Contains(path))
                    paths.Add(path);
            }

            return paths;
        }

        internal static object CreateSearchDatabaseSettings(string root, string indexType, bool types, bool properties,
            bool dependencies, bool extended, string[] includes = null)
        {
            var searchDataBaseType = GetSearchDataBaseType();
            var settingsType = searchDataBaseType.GetNestedType("Settings");
            Assert.NotNull(settingsType, "Cannot access SearchDatabase.Settings type by reflection.");

            var optionsType = searchDataBaseType.GetNestedType("Options");
            Assert.NotNull(optionsType, "Cannot access SearchDatabase.Options type by reflection.");

            var options = Activator.CreateInstance(optionsType);
            optionsType.GetField("types").SetValue(options, types);
            optionsType.GetField("properties").SetValue(options, properties);
            optionsType.GetField("dependencies").SetValue(options, dependencies);
            optionsType.GetField("extended").SetValue(options, extended);

            var settings = Activator.CreateInstance(settingsType);
            settingsType.GetField("name").SetValue(settings, System.Guid.NewGuid().ToString("N"));
            settingsType.GetField("type").SetValue(settings, indexType);
            settingsType.GetField("roots").SetValue(settings, new[] { root });
            settingsType.GetField("includes").SetValue(settings, includes ?? new string[0]);
            settingsType.GetField("options").SetValue(settings, options);

            return settings;
        }

        internal static ObjectIndexer CreateObjectIndexer(object settings)
        {
            var searchDataBaseType = GetSearchDataBaseType();
            var settingsType = searchDataBaseType.GetNestedType("Settings");
            var createIndexerMethod = searchDataBaseType.GetMethod("CreateIndexer",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null,
                new Type[] { settingsType, typeof(string) }, null);
            Assert.NotNull(createIndexerMethod, "Cannot get SearchDatabase.CreateIndexer function by reflection.");
            return (ObjectIndexer)createIndexerMethod.Invoke(null, new object[] { settings, null });
        }

        internal static ObjectIndexer CreateIndexer(string root, string indexType, bool types, bool properties,
            bool dependencies, bool extended, string[] includes = null)
        {
            var settings =
                CreateSearchDatabaseSettings(root, indexType, types, properties, dependencies, extended, includes);
            return CreateObjectIndexer(settings);
        }

        internal static List<string> GetDependencies(ObjectIndexer indexer)
        {
            var getDependenciesMethod = typeof(ObjectIndexer).GetMethod("GetDependencies",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(getDependenciesMethod, "Cannot get ObjectIndexer.GetDependencies function by reflection.");
            return (List<string>)getDependenciesMethod.Invoke(indexer, new object[0]);
        }

        internal static IEnumerator RunIndexingAsync(ObjectIndexer indexer, bool clear = false)
        {
            indexer.Start(clear);
            var paths = GetDependencies(indexer);
            foreach (var path in paths)
                indexer.IndexDocument(path, false);
            indexer.Finish();
            while (!indexer.IsReady())
                yield return null;
        }

        internal static void RunIndexing(ObjectIndexer indexer, bool clear, Action isDone)
        {
            var enumerator = RunIndexingAsync(indexer, clear);
            Tick(enumerator, isDone);
        }

        internal static void Tick(IEnumerator enumerator, Action isDone)
        {
            if (enumerator.MoveNext())
            {
                EditorApplication.delayCall += () => Tick(enumerator, isDone);
            }
            else
            {
                isDone();
            }
        }

        internal static List<string> Search(ObjectIndexer indexer, string query)
        {
            var results = indexer.Search(query.ToLowerInvariant(), null, null);
            return GetResultPaths(results);
        }

        [MenuItem("Tools/Simulate Indexation")]
        static void SimulateIndexation()
        {
            var asset = Selection.activeObject;
            if (asset == null)
                return;
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return;

            var indexer = CreateIndexer("Assets", "asset", true, true, true, false, new[] { path });
            RunIndexing(indexer, false, () =>
            {
                var results = Search(indexer, "t:material");
                Debug.Log(results);
            });
        }
    }
}
