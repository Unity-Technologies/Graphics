using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    [TestFixture]
    class ShaderInterfaceModeTests
    {
        static readonly GUID k_InterfaceAsset = new("0123456789abcdef0123456789abcdef");

        System.Func<bool> m_PreviousInternalMode;

        [SetUp]
        public void EnableInternalMode()
        {
            m_PreviousInternalMode = GraphData.isInternalMode;
            GraphData.isInternalMode = () => true;
        }

        [TearDown]
        public void RestoreInternalMode()
        {
            GraphData.isInternalMode = m_PreviousInternalMode;
        }

        static GraphData InterfaceGraph()
        {
            var graph = new GraphData { isShaderInterfaceMode = true };
            var definition = new ShaderInterface("Lit", default, null, null, null, null);
            graph.SetShaderInterface(new ShaderInterfaceProvider(k_InterfaceAsset, definition));
            return graph;
        }

        [Test]
        public void NewGraph_IsNotInShaderInterfaceMode()
        {
            var graph = new GraphData();

            Assert.IsFalse(graph.isShaderInterfaceMode);
            Assert.IsNull(graph.shaderInterface);
        }

        [Test]
        public void ModeAndInterface_SurviveSerialization()
        {
            var json = MultiJson.Serialize(InterfaceGraph());

            var loaded = new GraphData();
            MultiJson.Deserialize(loaded, json);

            Assert.IsTrue(loaded.isShaderInterfaceMode);
            Assert.AreEqual("Lit", loaded.shaderInterface.ProviderKey);
            Assert.AreEqual(k_InterfaceAsset, loaded.shaderInterface.AssetID);
        }

        [Test]
        public void SelectedInterface_IsADependencyOfTheGraph()
        {
            var path = Path.Combine(Path.GetTempPath(), "ShaderInterfaceModeTests.shadergraph");
            File.WriteAllText(path, MultiJson.Serialize(InterfaceGraph()));
            try
            {
                var dependencies = new AssetCollection();
                Assert.IsTrue(MinimalGraphData.GatherMinimalDependenciesFromFile(path, dependencies));

                Assert.IsTrue(dependencies.assets.TryGetValue(k_InterfaceAsset, out var flags));
                Assert.IsTrue(flags.HasFlag(AssetCollection.Flags.SourceDependency));
            }
            finally
            {
                File.Delete(path);
            }
        }

        static GraphData TargetGraph()
        {
            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new Target[] { new BuiltInTarget() }, null);
            return graph;
        }

        [Test]
        public void SwitchingTheModeOnAndOff_KeepsTargetsAndBlocks()
        {
            var graph = TargetGraph();
            var targets = graph.activeTargets.ToList();
            var blocks = graph.GetNodes<BlockNode>().ToList();

            graph.isShaderInterfaceMode = true;
            graph.ValidateGraph();
            graph.isShaderInterfaceMode = false;

            CollectionAssert.AreEqual(targets, graph.activeTargets.ToList());
            CollectionAssert.AreEqual(blocks, graph.GetNodes<BlockNode>().ToList());
        }

        [TestCase(false, ExpectedResult = false)]
        [TestCase(true, ExpectedResult = true)]
        public bool GraphWithTargets_ImportsAsTheErrorShader_OnlyInTheMode(bool interfaceMode)
        {
            var graph = TargetGraph();
            graph.isShaderInterfaceMode = interfaceMode;

            const string path = "Assets/ShaderInterfaceModeTests.shadergraph";
            File.WriteAllText(path, MultiJson.Serialize(graph));
            try
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                return AssetDatabase.LoadAssetAtPath<Shader>(path).name == "Hidden/GraphErrorShader2";
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        // Undo restores a graph by copying a deserialized one into the live one.
        [Test]
        public void ReplacingAGraph_CopiesTheModeAndTheInterface()
        {
            var source = InterfaceGraph();
            source.AddContexts();
            var live = new GraphData();
            live.AddContexts();

            live.ReplaceWith(source);

            Assert.IsTrue(live.isShaderInterfaceMode);
            Assert.AreEqual(k_InterfaceAsset, live.shaderInterface.AssetID);
        }

        [Test]
        public void SwitchingTheMode_FlipsTheFlag()
        {
            var graph = new GraphData();

            graph.SwitchShaderInterfaceMode(true);
            Assert.IsTrue(graph.isShaderInterfaceMode);

            graph.SwitchShaderInterfaceMode(false);
            Assert.IsFalse(graph.isShaderInterfaceMode);
        }

        static string PickerText(VisualElement settings)
            => settings.Q<Button>().Q<TextElement>(className: BasePopupField<string, string>.textUssClassName).text;

        [Test]
        public void Settings_WithoutAnInterface_OfferTheChoice()
        {
            var settings = new ShaderInterfaceSettings(new GraphData { isShaderInterfaceMode = true });

            Assert.AreEqual("None", PickerText(settings));
            Assert.IsNotNull(settings.Q<HelpBoxRow>());
        }

        [Test]
        public void Settings_WithAMissingInterface_Warn()
        {
            // Loaded from disk, the interface has to be found again; this one's source doesn't exist.
            var loaded = new GraphData();
            MultiJson.Deserialize(loaded, MultiJson.Serialize(InterfaceGraph()));
            var settings = new ShaderInterfaceSettings(loaded);

            Assert.AreEqual("Lit", PickerText(settings));
            Assert.IsNotNull(settings.Q<HelpBoxRow>());
            Assert.AreEqual(1, settings.Query<Button>().ToList().Count);
        }

        [Test]
        public void GraphNotUsingTheMode_SerializesWithoutInterfaceState()
        {
            StringAssert.DoesNotContain("ShaderInterface", MultiJson.Serialize(new GraphData()));
        }

        [Test]
        public void SwitchingTheModeOff_RemovesTheInterfaceState()
        {
            var graph = InterfaceGraph();

            graph.isShaderInterfaceMode = false;

            StringAssert.DoesNotContain("ShaderInterface", MultiJson.Serialize(graph));
        }

        [Test]
        public void TheMode_OnlyTakesEffectInInternalMode()
        {
            var graph = InterfaceGraph();

            GraphData.isInternalMode = () => false;

            Assert.IsFalse(graph.isShaderInterfaceMode);
        }

        [Test]
        public void ChangingTheInterfaceAsset_ReloadsTheGraphsInterface()
        {
            var graph = InterfaceGraph();
            Assume.That(graph.shaderInterface.IsValid);

            Assert.IsFalse(graph.ReloadDataExtensions(new System.Collections.Generic.HashSet<string> { "ffffffffffffffffffffffffffffffff" }));
            Assert.IsTrue(graph.shaderInterface.IsValid);

            Assert.IsTrue(graph.ReloadDataExtensions(new System.Collections.Generic.HashSet<string> { k_InterfaceAsset.ToString() }));
            Assert.IsFalse(graph.shaderInterface.IsValid, "The test interface has no source asset, so reloading it finds nothing.");
        }
    }
}
