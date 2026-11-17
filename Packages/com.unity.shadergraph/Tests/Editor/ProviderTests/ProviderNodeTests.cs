using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    [TestFixture]
    class ProviderNodeTests
    {
        sealed class ParameterMessageProvider : IProvider<IShaderFunction>
        {
            public ProviderInfo Info => new("ParameterMessage");
            public IShaderFunction Definition { get; } = new ShaderFunction("Func", null, new IShaderField[] { new ShaderField("a", true, false, new ShaderType("float")) }, new ShaderType("float"), "return a;");
            public IReadOnlyList<Message> Messages { get; } = new[] { new Message("Is ignored.", MessageSeverity.Warning, "a") };
            public IProvider Clone() => this;
        }

        static ProviderNode MissingNode()
        {
            var node = new ProviderNode();
            node.InitializeFromProvider(new ReflectedFunctionProvider(GUID.Generate(), "Missing"));
            return node;
        }

        static GraphData BuiltInGraph()
        {
            var graph = new GraphData { messageManager = new MessageManager() };
            graph.AddContexts();
            graph.InitializeOutputs(new Target[] { new BuiltInTarget() }, new[] { BlockFields.SurfaceDescription.BaseColor });
            return graph;
        }

        // Keeps the output it had when its source last loaded.
        static ProviderNode MissingNodeWithAnOutput(GraphData graph)
        {
            var node = MissingNode();
            node.AddSlot(new Vector3MaterialSlot(1, "Out", "Out", SlotType.Output, Vector3.zero));
            graph.AddNode(node);
            return node;
        }

        static bool ImportFails(GraphData graph)
        {
            new Generator(graph, null, GenerationMode.ForReals, "Test");
            return graph.messageManager.HasSeverity(id => graph.GetNodeFromId(id)?.wasUsedByGenerator ?? false, Rendering.ShaderCompilerMessageSeverity.Error);
        }

        [Test]
        public void ProviderNode_WhoseSourceIsMissing_IsAnOrphanLikeAnyProviderNode()
        {
            var graph = new GraphData { messageManager = new MessageManager() };
            var node = MissingNode();

            graph.AddNode(node);

            Assert.IsTrue(node.isOrphaned);
            StringAssert.Contains("'Missing'", node.messages.Single().text);
            Assert.AreEqual(MessageType.Error, node.messages.Single().severity);
            Assert.AreEqual("Setup: " + node.messages.Single().text, graph.messageManager.ErrorStrings(id => id == node.objectId).Single());
        }

        [Test]
        public void ProviderNode_WhoseSourceIsMissing_StaysInactive_InAGraphWhoseTargetAllowsIt()
        {
            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new Target[] { new BuiltInTarget() }, null);
            var node = MissingNode();

            graph.AddNode(node);

            Assert.IsTrue(node.isOrphaned);
            Assert.IsFalse(node.isActive);
        }

        [Test]
        public void ProviderNode_WhoseSourceIsMissing_FailsTheImport_WhenItFeedsTheOutput()
        {
            var graph = BuiltInGraph();
            var node = MissingNodeWithAnOutput(graph);
            var baseColor = graph.GetNodes<BlockNode>().Single(b => b.descriptor == BlockFields.SurfaceDescription.BaseColor);
            graph.Connect(node.GetSlotReference(1), baseColor.GetSlotReference(0));

            Assert.IsTrue(ImportFails(graph));
            Assert.IsTrue(node.wasUsedByGenerator);
        }

        [Test]
        public void ProviderNode_WhoseSourceIsMissing_DoesNotFailTheImport_WhenItFeedsNothing()
        {
            var graph = BuiltInGraph();
            var node = MissingNodeWithAnOutput(graph);

            Assert.IsFalse(ImportFails(graph));
            Assert.IsFalse(node.wasUsedByGenerator);
        }

        [Test]
        public void ProviderNodes_WhoseSourcesAreMissing_InALongChain_DoNotFailTheImport_WhenTheyFeedNothing()
        {
            var graph = BuiltInGraph();
            var previous = MissingNodeWithAnOutput(graph);
            for (int i = 0; i < 40; ++i)
            {
                var node = MissingNodeWithAnOutput(graph);
                node.AddSlot(new Vector3MaterialSlot(0, "In", "In", SlotType.Input, Vector3.zero));
                graph.Connect(previous.GetSlotReference(1), node.GetSlotReference(0));
                previous = node;
            }

            Assert.IsFalse(ImportFails(graph));
        }

        [Test]
        public void ProviderNode_ReportsAParametersMessage_WithTheParametersName()
        {
            var graph = new GraphData { messageManager = new MessageManager() };
            var node = new ProviderNode();
            node.InitializeFromProvider(new ParameterMessageProvider());

            graph.AddNode(node);

            CollectionAssert.Contains(graph.messageManager.ErrorStrings(id => id == node.objectId, Rendering.ShaderCompilerMessageSeverity.Warning), "Validation: 'a': Is ignored.");
        }
    }
}
