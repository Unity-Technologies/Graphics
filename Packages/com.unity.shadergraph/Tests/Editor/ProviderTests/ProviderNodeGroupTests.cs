using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    // Locks in how a provider node shows its group and versions from the provider library.
    [TestFixture]
    class ProviderNodeGroupTests
    {
        const string kTestPath = "Assets/generated-ProviderNodeGroupTest.hlsl";

        static string Function(string name, params (string key, string value)[] hints)
        {
            var sb = new System.Text.StringBuilder("/// <funchints>\n");
            foreach (var (key, value) in hints)
                sb.Append($"///\t <{key}>{value}</{key}>\n");
            sb.Append("/// </funchints>\n");
            sb.Append($"UNITY_EXPORT_REFLECTION float {name}(float a) {{ return a; }}\n");
            return sb.ToString();
        }

        [OneTimeSetUp]
        public void Setup()
        {
            var source =
                Function("GroupMain", ("sg:ProviderKey", "GroupTest.Main"), ("sg:GroupKey", "GroupTest, 0, Grouped, Mode"), ("sg:HelpURL", "https://example.com/main"), ("sg:SearchTerms", "main")) +
                Function("GroupOther", ("sg:ProviderKey", "GroupTest.Other"), ("sg:GroupKey", "GroupTest, 1, Other Group, Other Label"), ("sg:SearchTerms", "other")) +
                Function("VersionOne", ("sg:ProviderKey", "GroupTest.Versioned"), ("sg:Version", "1")) +
                Function("VersionTwo", ("sg:ProviderKey", "GroupTest.Versioned"), ("sg:Version", "2"));
            FileUtilities.WriteToDisk(kTestPath, source);
            AssetDatabase.ImportAsset(kTestPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            AssetDatabase.DeleteAsset(kTestPath);
        }

        static ProviderNode NodeFor(string providerKey, string version = null)
        {
            Assert.IsTrue(ProviderLibrary.TryGetInstance(out var lib), "Provider library must be available in the main editor.");
            foreach (var provider in lib.ProvidersByVersion<IShaderFunction>(providerKey))
            {
                if (version != null && provider.Version != version)
                    continue;
                var node = new ProviderNode();
                new GraphData().AddNode(node);
                node.InitializeFromProvider(provider);
                return node;
            }
            Assert.Fail($"No provider for '{providerKey}'.");
            return null;
        }

        static readonly Regex k_PackageVersion = new("@[0-9]+\\.[0-9]+");

        static string Snapshot(ProviderNode node)
        {
            var header = node.Header;
            List<string> candidates = new();
            foreach (var (provider, label) in header.groupCandidates ?? new (IProvider<IShaderFunction>, string)[0])
                candidates.Add($"{provider.ProviderKey}={label}");
            List<string> versions = new();
            foreach (var (provider, version) in header.versionCandidates ?? new (IProvider<IShaderFunction>, string)[0])
                versions.Add($"{provider.Definition.Name}={version}");
            var help = k_PackageVersion.Replace(node.documentationURL ?? "", "@<version>");
            return $"name={node.name}; search={header.searchName}; synonyms={string.Join(",", node.synonyms ?? new string[0])}; help={help}; " +
                $"label={header.groupLabel}; groups=[{string.Join(", ", candidates)}]; versions=[{string.Join(", ", versions)}]";
        }

        // The library lists the group's highest priority provider first, so it is the group's main provider.
        [Test]
        public void GroupedProvider_ShowsItsGroup()
        {
            Assert.AreEqual(
                "name=Other Group; search=Grouped; synonyms=other,main; help=https://example.com/main; label=Other Label; " +
                "groups=[GroupTest.Other=GroupOther, GroupTest.Main=GroupMain]; versions=[GroupMain=<unversioned>]",
                Snapshot(NodeFor("GroupTest.Main")));
        }

        [Test]
        public void MainGroupProvider_ShowsItsGroup()
        {
            Assert.AreEqual(
                "name=Other Group; search=Other Group; synonyms=other,main; help=https://docs.unity3d.com/Packages/com.unity.shadergraph@<version>/manual/Other-Group-Node.html; label=Other Label; " +
                "groups=[GroupTest.Other=GroupOther, GroupTest.Main=GroupMain]; versions=[GroupOther=<unversioned>]",
                Snapshot(NodeFor("GroupTest.Other")));
        }

        [TestCase("1", "VersionOne")]
        [TestCase("2", "VersionTwo")]
        public void VersionedProvider_ListsEveryVersion(string version, string name)
        {
            Assert.AreEqual(
                $"name={name}; search=float {name}(in float); synonyms={name}; help=https://docs.unity3d.com/Packages/com.unity.shadergraph@<version>/manual/{name}-Node.html; label=; " +
                "groups=[]; versions=[VersionTwo=2, VersionOne=1]",
                Snapshot(NodeFor("GroupTest.Versioned", version)));
        }
    }
}
