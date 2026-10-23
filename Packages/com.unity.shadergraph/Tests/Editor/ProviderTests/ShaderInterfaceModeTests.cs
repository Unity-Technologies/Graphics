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
        const string kInterfacePath = "Assets/ShaderInterfaceModeTests.shader";
        const string kInterfaceName = "Hidden/Tests/ShaderInterfaceModeTests";

        const string kInterfaceSource = @"
Shader ""Hidden/Tests/ShaderInterfaceModeTests""
{
    SubShader
    {
        Pass
        {
            Name ""Forward""
            Public
            {
                UNITY_VIRTUAL half3 GetColor() { return 1; }
            }
            Private
            {
                #pragma vertex Vert
                #pragma fragment Frag
                float4 Vert(float4 positionOS : POSITION0) : SV_POSITION0 { return positionOS; }
                float4 Frag() : SV_TARGET0 { return float4(GetColor(), 1); }
            }
        }
    }
}";

        static GUID InterfaceAsset => AssetDatabase.GUIDFromAssetPath(kInterfacePath);

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
            AssetDatabase.DeleteAsset(kInterfacePath);
        }

        static GraphData InterfaceGraph()
        {
            var graph = new GraphData { isShaderInterfaceMode = true };
            File.WriteAllText(kInterfacePath, kInterfaceSource);
            AssetDatabase.ImportAsset(kInterfacePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            IProvider<IShaderInterface> provider = new ShaderInterfaceProvider(InterfaceAsset, kInterfaceName);
            Assume.That(provider.IsValid);
            graph.SetShaderInterface(provider);
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
            Assert.AreEqual(kInterfaceName, loaded.shaderInterface.Info.ProviderKey);
            Assert.AreEqual(InterfaceAsset, loaded.shaderInterface.AssetID);
        }

        [Test]
        public void LocalProviderState_SurvivesSerialization()
        {
            var graph = InterfaceGraph();
            var function = (VirtualFunctionProvider)((ShaderInterfaceProvider)graph.shaderInterface).VirtualFunctionProviders.Single(p => p.Definition.Name == "GetColor");
            function.EnsureBaseCall = false;

            var loaded = new GraphData();
            MultiJson.Deserialize(loaded, MultiJson.Serialize(graph));

            var restored = ((ShaderInterfaceProvider)loaded.shaderInterface).VirtualFunctionProviders.Single(p => p.Definition.Name == "GetColor");
            Assert.AreNotSame(function, restored);
            Assert.IsFalse(restored.Definition.EnsureBaseCall);
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

                Assert.IsTrue(dependencies.assets.TryGetValue(InterfaceAsset, out var flags));
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
            Assert.AreEqual(InterfaceAsset, live.shaderInterface.AssetID);
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
            var json = MultiJson.Serialize(InterfaceGraph());
            AssetDatabase.DeleteAsset(kInterfacePath);
            MultiJson.Deserialize(loaded, json);
            var settings = new ShaderInterfaceSettings(loaded);

            Assert.AreEqual(kInterfaceName, PickerText(settings), "Without its source, the interface is shown by its saved name.");
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

            var assetId = graph.shaderInterface.AssetID;
            AssetDatabase.DeleteAsset(kInterfacePath);
            Assert.IsTrue(graph.ReloadDataExtensions(new System.Collections.Generic.HashSet<string> { assetId.ToString() }));
            Assert.IsFalse(graph.shaderInterface.IsValid, "The interface was removed, so reloading it finds nothing.");
        }
    }
}
