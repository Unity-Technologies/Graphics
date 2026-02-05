using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class AssetIndexingTests
    {
        public class CustomIndexationTestCase
        {
            public readonly string[] files;
            public readonly string query;
            public readonly string[] expectedFiles;
            public readonly int expectedFileCount;

            public CustomIndexationTestCase(string query, string file, bool shouldBeFound)
            {
                this.query = query;
                this.files = new[] { file };
                this.expectedFileCount = shouldBeFound ? 1 : 0;
            }

            public CustomIndexationTestCase(string query, string[] files, string[] expectedFiles)
            {
                this.query = query;
                this.files = files;
                this.expectedFiles = expectedFiles;
                expectedFileCount = expectedFiles.Length;
            }

            public override string ToString()
            {
                var expectedFilesString = this.expectedFiles != null ? $"[{string.Join(',', this.expectedFiles)}]" : "";
                return $"{query} [{string.Join(',', files)}] => {expectedFileCount} {expectedFilesString}";
            }
        }

        public static IEnumerable<CustomIndexationTestCase> GetCustomIndexationTestCases()
        {
            var template = "Packages/com.unity.shadergraph/GraphTemplates/Cross Pipeline/1_Lit Full.shadergraph";
            yield return new CustomIndexationTestCase("t:shader", template, true);
            yield return new CustomIndexationTestCase("t:shader shadergraph.material=lit", template, true);
            yield return new CustomIndexationTestCase("t:shader shadergraph.category=basics", template, true);

            template = "Packages/com.unity.shadergraph/GraphTemplates/Cross Pipeline/1_Decal Material Volume.shadergraph";
            yield return new CustomIndexationTestCase("t:shader", template, true);
            yield return new CustomIndexationTestCase("t:shader shadergraph.material=decal", template, true);
            yield return new CustomIndexationTestCase("t:shader shadergraph.category=decals", template, true);
        }

        [UnityTest]
        public IEnumerator ValidateCustomIndexation([ValueSource(nameof(GetCustomIndexationTestCases))] CustomIndexationTestCase tc)
        {
            var root = "Assets";
            if (tc.files[0].StartsWith("Packages"))
            {
                var packageNameIndex = tc.files[0].IndexOf("/", "Packages/".Length, StringComparison.Ordinal);
                root = tc.files[0].Substring(0, packageNameIndex);
            }

            using var indexer = CustomIndexerUtilities.CreateIndexer(
                root,
                "asset",
                types: true,
                properties: true,
                dependencies: true,
                extended: false,
                tc.files);
            yield return CustomIndexerUtilities.RunIndexingAsync(indexer, true);

            Assert.IsTrue(indexer.IsReady());
            var results = CustomIndexerUtilities.Search(indexer, tc.query);
            Assert.AreEqual(tc.expectedFileCount, results.Count, $"Query {tc.query} yielded {results.Count} expected was {tc.expectedFileCount}");
            if (tc.expectedFiles != null)
            {
                CollectionAssert.AreEquivalent(tc.expectedFiles, results);
            }
        }
    }
}
