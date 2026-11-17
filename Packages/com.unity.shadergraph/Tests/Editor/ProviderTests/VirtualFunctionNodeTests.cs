using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    [TestFixture]
    class VirtualFunctionNodeTests : VirtualFunctionFixture
    {
        static GraphData InterfaceGraph() => VirtualFunctionTestInterface.InterfaceGraph();
        static VirtualFunctionOutputNode Output(GraphData graph, string key) => VirtualFunctionTestInterface.Output(graph, key);
        static IProvider<IVirtualFunction> Function(GraphData graph, string key) => VirtualFunctionTestInterface.Function(graph, key);
        static T AddNode<T>(GraphData graph, string key) where T : AbstractVirtualFunctionNode, new() => VirtualFunctionTestInterface.AddNode<T>(graph, key);

        static MaterialSlot Slot(AbstractMaterialNode node, string name, bool input)
            => node.GetSlots<MaterialSlot>().Single(s => s.shaderOutputName == name && s.isInputSlot == input);

        static string[] SlotNames(AbstractMaterialNode node, bool input)
            => node.GetSlots<MaterialSlot>().Where(s => s.isInputSlot == input).Select(s => s.shaderOutputName).ToArray();

        static IEdge ConnectConstant(GraphData graph, MaterialSlot target)
        {
            var constant = new Vector1Node();
            graph.AddNode(constant);
            return graph.Connect(constant.GetSlotReference(Vector1Node.OutputSlotId), target.slotReference);
        }

        static string[] Errors(GraphData graph, AbstractMaterialNode node) => VirtualFunctionTestInterface.Errors(graph, node);

        [Test]
        public void SelectingAnInterface_AddsOneOutputNodePerVirtualFunction()
        {
            var graph = InterfaceGraph();

            CollectionAssert.AreEquivalent(
                new[] { VirtualFunctionTestInterface.kSurface, VirtualFunctionTestInterface.kVertex, VirtualFunctionTestInterface.kTint },
                graph.GetNodes<VirtualFunctionOutputNode>().Select(o => o.providerKey).ToArray());
        }

        [Test]
        public void OutputNodes_AreStackedByContext_OrderedContextsFirst()
        {
            var graph = InterfaceGraph();

            CollectionAssert.AreEqual(new[] { "Vertex", "Fragment", "" }, VirtualFunctionTestInterface.StackContexts(graph));
            Assert.AreEqual("Fragment", VirtualFunctionTestInterface.StackOf(graph, VirtualFunctionTestInterface.kSurface));
            Assert.AreEqual("", VirtualFunctionTestInterface.StackOf(graph, VirtualFunctionTestInterface.kTint));
        }

        [Test]
        public void OutputNode_TakesTheOutputsAndTheReturnValue()
        {
            var graph = InterfaceGraph();
            var surface = Output(graph, VirtualFunctionTestInterface.kSurface);
            var vertex = Output(graph, VirtualFunctionTestInterface.kVertex);

            Assert.AreEqual("OnSurface", surface.name);
            CollectionAssert.AreEqual(new[] { "albedo", "alpha" }, SlotNames(surface, true));
            CollectionAssert.IsEmpty(SlotNames(surface, false));
            Assert.AreEqual("inout", Slot(surface, "albedo", true).CustomBinding);
            Assert.IsNull(Slot(surface, "alpha", true).CustomBinding);
            Assert.IsNotNull(vertex.FindInputSlot<MaterialSlot>(ProviderSlotUtils.kReturnSlotId));
        }

        [Test]
        public void InputNode_ShowsTheArgumentsAsOutputs()
        {
            var graph = InterfaceGraph();
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);

            Assert.AreEqual("OnSurface Input", input.name);
            CollectionAssert.AreEqual(new[] { "positionWS", "albedo" }, SlotNames(input, false));
            CollectionAssert.IsEmpty(SlotNames(input, true));
        }

        [Test]
        public void BaseNode_ShowsACall_WhoseUnconnectedInputsPassTheIncomingArguments()
        {
            var graph = InterfaceGraph();
            var surface = AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface);
            var vertex = AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kVertex);

            Assert.AreEqual("OnSurface Base", surface.name);
            CollectionAssert.AreEqual(new[] { "positionWS", "albedo" }, SlotNames(surface, true));
            CollectionAssert.AreEqual(new[] { "albedo", "alpha" }, SlotNames(surface, false));
            Assert.AreEqual("in", Slot(surface, "positionWS", true).CustomBinding);
            Assert.AreEqual("inout", Slot(surface, "albedo", true).CustomBinding);
            Assert.IsNotNull(vertex.FindOutputSlot<MaterialSlot>(ProviderSlotUtils.kReturnSlotId));
        }

        [Test]
        public void Nodes_ReferToTheInterfacesProvider_RatherThanCopyingIt()
        {
            var graph = InterfaceGraph();
            var function = Function(graph, VirtualFunctionTestInterface.kSurface);

            Assert.AreSame(function, Output(graph, VirtualFunctionTestInterface.kSurface).Provider);
            Assert.AreSame(function, AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface).Provider);
            Assert.AreSame(function, AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface).Provider);
        }

        static string ReadOutput(AbstractMaterialNode node, string name)
        {
            var slot = Slot(node, name, false);
            return node.GetOutputForSlot(slot.slotReference, slot.concreteValueType, GenerationMode.ForReals);
        }

        static string SurfaceWithPositionDefault(string value) => VirtualFunctionTestInterface.Hints(VirtualFunctionTestInterface.kSurface, "Fragment, 2")
            + "/// <paramhints name=\"positionWS\">\n/// <sg:Default>" + value + "</sg:Default>\n/// </paramhints>\n"
            + "UNITY_VIRTUAL void OnSurface(in float3 positionWS, inout half3 albedo, out half alpha) { alpha = 1; }\n";

        [Test]
        public void VirtualFunctionNodes_HaveNoPreview_AndTheirOutputsReadAsTheirDefaultValues()
        {
            var graph = InterfaceGraph();
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);

            Assert.IsFalse(input.hasPreview);
            Assert.AreEqual(Slot(input, "positionWS", false).GetDefaultValue(GenerationMode.ForReals), ReadOutput(input, "positionWS"));
        }

        [Test]
        public void HotReload_ThatChangesAnArgumentsDefault_ChangesWhatItsOutputReads()
        {
            VirtualFunctionTestInterface.Write(SurfaceWithPositionDefault("1, 2, 3"));
            var graph = InterfaceGraph();
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            var before = ReadOutput(input, "positionWS");

            VirtualFunctionTestInterface.Write(SurfaceWithPositionDefault("4, 5, 6"));
            VirtualFunctionTestInterface.Reload(graph);

            Assert.AreNotEqual(before, ReadOutput(input, "positionWS"));
            StringAssert.Contains("4", ReadOutput(input, "positionWS"));
        }

        [Test]
        public void Sync_IsIdempotent()
        {
            var graph = InterfaceGraph();
            var output = Output(graph, VirtualFunctionTestInterface.kSurface);
            var slotIds = output.GetSlots<MaterialSlot>().Select(s => s.id).ToArray();
            graph.ClearChanges();

            graph.SyncVirtualFunctionStacks();
            graph.ValidateGraph();

            Assert.IsFalse(graph.shaderInterfaceChanged);
            Assert.AreEqual(3, graph.GetNodes<VirtualFunctionOutputNode>().Count());
            Assert.AreSame(output, Output(graph, VirtualFunctionTestInterface.kSurface));
            CollectionAssert.AreEqual(slotIds, output.GetSlots<MaterialSlot>().Select(s => s.id).ToArray());
        }

        [Test]
        public void OutputNodes_CannotBeCopiedOrCut_AndAreDeletableOnlyAsOrphans()
        {
            var graph = InterfaceGraph();
            var output = Output(graph, VirtualFunctionTestInterface.kVertex);

            Assert.IsFalse(output.canCopyNode);
            Assert.IsFalse(output.canCutNode);
            Assert.IsFalse(output.canDeleteNode);

            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kSurfaceFunction + VirtualFunctionTestInterface.kTintFunction);
            VirtualFunctionTestInterface.Reload(graph);

            Assert.IsTrue(output.isOrphaned);
            Assert.IsTrue(output.canDeleteNode);
        }

        [Test]
        public void HotReload_UpdatesNodesInPlace_AndKeepsTheirSlotIdsAndEdges()
        {
            var graph = InterfaceGraph();
            var output = Output(graph, VirtualFunctionTestInterface.kSurface);
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            var alphaId = Slot(output, "alpha", true).id;
            ConnectConstant(graph, Slot(output, "alpha", true));

            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.Hints(VirtualFunctionTestInterface.kSurface, "Fragment, 2")
                + "UNITY_VIRTUAL void OnSurface(in float3 positionWS, out half3 albedo, out half alpha, in half3 emission) { albedo = 0; alpha = 1; }\n"
                + VirtualFunctionTestInterface.kVertexFunction + VirtualFunctionTestInterface.kTintFunction);
            VirtualFunctionTestInterface.Reload(graph);

            Assert.AreSame(output, Output(graph, VirtualFunctionTestInterface.kSurface));
            Assert.AreEqual(alphaId, Slot(output, "alpha", true).id);
            Assert.AreEqual(1, graph.GetEdges(output.GetSlotReference(alphaId)).Count());
            Assert.IsNull(Slot(output, "albedo", true).CustomBinding, "An out parameter takes a value, not the incoming one.");
            CollectionAssert.AreEqual(new[] { "positionWS", "emission" }, SlotNames(input, false));
        }

        [Test]
        public void HotReload_RefreshesNodesAgainstTheReloadedInterface_BeforeValidation()
        {
            var graph = InterfaceGraph();
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            var vertex = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kVertex);
            int changes = 0;
            vertex.RegisterCallback((_, _) => ++changes);

            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.Hints(VirtualFunctionTestInterface.kSurface, "Fragment, 2")
                + "UNITY_VIRTUAL void OnSurface(in float3 positionWS, inout half3 albedo, out half alpha, in half3 emission) { alpha = 1; }\n"
                + VirtualFunctionTestInterface.kVertexFunction + VirtualFunctionTestInterface.kTintFunction);
            bool reloaded = graph.ReloadAssetDependencies(new HashSet<string> { graph.shaderInterface.AssetID.ToString() });

            Assert.IsTrue(reloaded);
            CollectionAssert.AreEqual(new[] { "positionWS", "albedo", "emission" }, SlotNames(input, false));
            Assert.Greater(changes, 0, "Every node of the interface refreshes, even when its function didn't change.");
        }

        [Test]
        public void HotReload_ThatAddsAFunction_WhileANodeDropsAConnectedSlot_AddsItsOutputNode()
        {
            var graph = InterfaceGraph();
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            var output = Output(graph, VirtualFunctionTestInterface.kSurface);
            graph.Connect(Slot(input, "positionWS", false).slotReference, Slot(output, "albedo", true).slotReference);

            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.Hints(VirtualFunctionTestInterface.kSurface, "Fragment, 2")
                + "UNITY_VIRTUAL void OnSurface(inout half3 albedo, out half alpha) { alpha = 1; }\n"
                + VirtualFunctionTestInterface.kVertexFunction + VirtualFunctionTestInterface.kTintFunction + VirtualFunctionTestInterface.kLightFunction);

            Assert.DoesNotThrow(() => VirtualFunctionTestInterface.Reload(graph));
            Assert.IsFalse(Output(graph, VirtualFunctionTestInterface.kLight).isOrphaned);
            CollectionAssert.AreEqual(new[] { "albedo" }, SlotNames(input, false));
            CollectionAssert.IsEmpty(graph.GetEdges(input));
        }

        [Test]
        public void HotReload_AFunctionTheInterfaceDropped_LeavesItsNodesAsOrphans_WithTheirEdges()
        {
            var graph = InterfaceGraph();
            var output = Output(graph, VirtualFunctionTestInterface.kVertex);
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kVertex);
            ConnectConstant(graph, output.FindInputSlot<MaterialSlot>(ProviderSlotUtils.kReturnSlotId));
            graph.messageManager = new MessageManager();

            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kSurfaceFunction + VirtualFunctionTestInterface.kTintFunction);
            VirtualFunctionTestInterface.Reload(graph);

            Assert.IsTrue(graph.ContainsNode(output) && graph.ContainsNode(input));
            Assert.IsTrue(output.isOrphaned && input.isOrphaned);
            Assert.IsFalse(output.isActive);
            Assert.AreEqual(1, graph.GetEdges(output.GetSlotReference(ProviderSlotUtils.kReturnSlotId)).Count());
            CollectionAssert.AreEqual(new[] { "Fragment", "", "Vertex" }, VirtualFunctionTestInterface.StackContexts(graph));
            StringAssert.Contains(VirtualFunctionTestInterface.kVertex, Errors(graph, input).Single());
        }

        [Test]
        public void UnloadableInterface_OrphansEveryNode_UntilItLoadsAgain()
        {
            var graph = InterfaceGraph();
            var output = Output(graph, VirtualFunctionTestInterface.kSurface);
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            ConnectConstant(graph, Slot(output, "alpha", true));
            graph.messageManager = new MessageManager();

            VirtualFunctionTestInterface.WritePlainShader();
            VirtualFunctionTestInterface.Reload(graph);

            Assert.IsTrue(output.isOrphaned && input.isOrphaned);
            CollectionAssert.AreEqual(new[] { "Vertex", "Fragment", "" }, VirtualFunctionTestInterface.StackContexts(graph));
            Assert.AreEqual(1, graph.GetEdges(Slot(output, "alpha", true).slotReference).Count());
            Assert.IsNotEmpty(Errors(graph, output));

            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kDefault);
            VirtualFunctionTestInterface.Reload(graph);

            Assert.IsFalse(output.isOrphaned || input.isOrphaned);
            Assert.IsTrue(output.isActive);
            Assert.AreEqual(3, graph.GetNodes<VirtualFunctionOutputNode>().Count());
        }

        [Test]
        public void Reassigning_OrphansTheNodesTheNewInterfaceDoesntDeclare_AndCarriesTheirState()
        {
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kSurfaceFunction + VirtualFunctionTestInterface.kLightFunction,
                VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName);
            var graph = InterfaceGraph();
            var surface = Output(graph, VirtualFunctionTestInterface.kSurface);
            var surfaceInput = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            var vertexInput = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kVertex);
            ((VirtualFunctionProvider)Function(graph, VirtualFunctionTestInterface.kSurface)).EnsureBaseCall = false;

            graph.SetShaderInterface(VirtualFunctionTestInterface.Interface(VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName));

            CollectionAssert.AreEquivalent(
                new[] { VirtualFunctionTestInterface.kSurface, VirtualFunctionTestInterface.kLight, VirtualFunctionTestInterface.kVertex, VirtualFunctionTestInterface.kTint },
                graph.GetNodes<VirtualFunctionOutputNode>().Select(o => o.providerKey).ToArray());
            Assert.IsFalse(surface.isOrphaned || surfaceInput.isOrphaned);
            Assert.IsTrue(vertexInput.isOrphaned && Output(graph, VirtualFunctionTestInterface.kVertex).isOrphaned && Output(graph, VirtualFunctionTestInterface.kTint).isOrphaned);
            Assert.IsFalse(((VirtualFunctionProvider)Function(graph, VirtualFunctionTestInterface.kSurface)).EnsureBaseCall);
            CollectionAssert.AreEqual(new[] { "Fragment", "Vertex", "" }, VirtualFunctionTestInterface.StackContexts(graph));
        }

        [Test]
        public void ReassigningBack_RecoversTheOrphans_WithTheirConnections()
        {
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kSurfaceFunction + VirtualFunctionTestInterface.kLightFunction,
                VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName);
            var graph = InterfaceGraph();
            var vertex = Output(graph, VirtualFunctionTestInterface.kVertex);
            var vertexInput = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kVertex);
            ConnectConstant(graph, vertex.FindInputSlot<MaterialSlot>(ProviderSlotUtils.kReturnSlotId));

            graph.SetShaderInterface(VirtualFunctionTestInterface.Interface(VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName));
            graph.SetShaderInterface(VirtualFunctionTestInterface.Interface());

            Assert.AreSame(vertex, Output(graph, VirtualFunctionTestInterface.kVertex));
            Assert.IsFalse(vertex.isOrphaned || vertexInput.isOrphaned);
            Assert.AreEqual(1, graph.GetEdges(vertex.GetSlotReference(ProviderSlotUtils.kReturnSlotId)).Count());
            Assert.IsTrue(Output(graph, VirtualFunctionTestInterface.kLight).isOrphaned);
            CollectionAssert.AreEqual(new[] { "Vertex", "Fragment", "" }, VirtualFunctionTestInterface.StackContexts(graph));
        }

        [Test]
        public void Reassigning_KeepsThePositionOfAStackBothInterfacesUse()
        {
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kLightFunction,
                VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName);
            var graph = InterfaceGraph();
            graph.virtualFunctionStacks.Single(s => s.context == "Fragment").position = new Vector2(300, 100);

            graph.SetShaderInterface(VirtualFunctionTestInterface.Interface(VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName));

            var fragment = graph.virtualFunctionStacks.Single(s => s.context == "Fragment");
            CollectionAssert.AreEqual(new[] { VirtualFunctionTestInterface.kLight, VirtualFunctionTestInterface.kSurface }, fragment.providerKeys);
            Assert.AreEqual(new Vector2(300, 100), fragment.position);
        }

        [Test]
        public void LeavingTheMode_RemovesTheOutputNodes_AndOrphansTheOtherNodes()
        {
            var graph = InterfaceGraph();
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);

            graph.SwitchShaderInterfaceMode(false);

            CollectionAssert.IsEmpty(graph.GetNodes<VirtualFunctionOutputNode>());
            Assert.IsTrue(input.isOrphaned);
        }

        [Test]
        public void LeavingTheMode_InAGraphWithATarget_KeepsTheOrphansInactive()
        {
            var graph = VirtualFunctionTestInterface.InterfaceGraphWithTarget();
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);

            graph.SwitchShaderInterfaceMode(false);

            Assert.IsTrue(input.isOrphaned);
            Assert.IsFalse(input.isActive, "Target validation must not reactivate an orphan.");
        }

        [Test]
        public void ReplacingTheGraph_AsUndoDoes_DoesNotDuplicateOutputNodes()
        {
            var withContexts = new GraphData();
            withContexts.AddContexts();
            var graph = VirtualFunctionTestInterface.InterfaceGraph(withContexts);
            var saved = new GraphData();
            MultiJson.Deserialize(saved, MultiJson.Serialize(graph));

            graph.ReplaceWith(saved);

            Assert.AreEqual(3, graph.GetNodes<VirtualFunctionOutputNode>().Count());
            CollectionAssert.AreEqual(new[] { "Vertex", "Fragment", "" }, VirtualFunctionTestInterface.StackContexts(graph));
        }

        [Test]
        public void StacksAndNodes_SurviveSerialization()
        {
            var graph = InterfaceGraph();
            AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kVertex);
            AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface);
            graph.virtualFunctionStacks[0].position = new Vector2(40, 80);

            var loaded = new GraphData();
            MultiJson.Deserialize(loaded, MultiJson.Serialize(graph));
            loaded.ValidateGraph();

            Assert.AreEqual(3, loaded.GetNodes<VirtualFunctionOutputNode>().Count());
            Assert.IsTrue(loaded.GetNodes<AbstractVirtualFunctionNode>().All(n => !n.isOrphaned));
            CollectionAssert.AreEqual(new[] { "Vertex", "Fragment", "" }, VirtualFunctionTestInterface.StackContexts(loaded));
            Assert.AreEqual(new Vector2(40, 80), loaded.virtualFunctionStacks[0].position);
            Assert.AreSame(Function(loaded, VirtualFunctionTestInterface.kVertex), loaded.GetNodes<VirtualFunctionInputNode>().Single().Provider);
        }

        [Test]
        public void Loading_AfterTheInterfaceGainedAFunction_AddsItsOutputNode()
        {
            var json = MultiJson.Serialize(InterfaceGraph());
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kDefault + VirtualFunctionTestInterface.kLightFunction);

            var loaded = new GraphData();
            MultiJson.Deserialize(loaded, json);

            Assert.AreEqual(4, loaded.GetNodes<VirtualFunctionOutputNode>().Count());
            Assert.AreEqual("Fragment", VirtualFunctionTestInterface.StackOf(loaded, VirtualFunctionTestInterface.kLight));
            CollectionAssert.IsEmpty(loaded.addedNodes, "The view builds every loaded node itself.");
        }

        [Test]
        public void DeletingAnOrphanOutputNode_RemovesItFromItsStack()
        {
            var graph = InterfaceGraph();
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kSurfaceFunction + VirtualFunctionTestInterface.kTintFunction);
            VirtualFunctionTestInterface.Reload(graph);
            var orphan = Output(graph, VirtualFunctionTestInterface.kVertex);
            graph.ClearChanges();

            graph.RemoveElements(new AbstractMaterialNode[] { orphan }, new IEdge[0], new GroupData[0], new StickyNoteData[0]);

            CollectionAssert.AreEqual(new[] { "Fragment", "" }, VirtualFunctionTestInterface.StackContexts(graph));
            Assert.IsTrue(graph.shaderInterfaceChanged);
        }

        [Test]
        public void NodeAddedToAGraphWhoseInterfaceDoesntDeclareIt_IsAnOrphan()
        {
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kLightFunction, VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName);
            var source = InterfaceGraph();
            var other = new GraphData { isShaderInterfaceMode = true };
            other.SetShaderInterface(VirtualFunctionTestInterface.Interface(VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName));
            other.messageManager = new MessageManager();

            var node = new VirtualFunctionInputNode();
            node.InitializeFromLocalProvider(Function(source, VirtualFunctionTestInterface.kSurface));
            other.AddNode(node);

            Assert.IsNull(node.Provider);
            Assert.IsTrue(node.isOrphaned);
            Assert.IsFalse(node.isActive);
            StringAssert.Contains(VirtualFunctionTestInterface.kSurface, Errors(other, node).Single());
        }

        [Test]
        public void BaseNode_OfAPureVirtualFunction_ReportsThatThereIsNoBase()
        {
            var graph = InterfaceGraph();
            graph.messageManager = new MessageManager();

            var node = AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kTint);

            StringAssert.Contains("pure virtual", Errors(graph, node).Single());
            Assert.AreEqual(Errors(graph, node).Single(), "Validation: " + node.messages.Single().text, "The inspector shows what validation reports.");
        }

        [Test]
        public void MissingProvider_IsReportedAsASetupError()
        {
            var graph = InterfaceGraph();
            graph.messageManager = new MessageManager();
            var node = new VirtualFunctionInputNode();
            node.InitializeFromLocalProvider(Function(graph, VirtualFunctionTestInterface.kSurface));
            var other = new GraphData { isShaderInterfaceMode = true, messageManager = graph.messageManager };

            other.AddNode(node);

            var message = node.messages.Single();
            Assert.AreEqual(MessageType.Error, message.severity);
            StringAssert.Contains("No shader interface is selected", message.text);
            Assert.AreEqual("Setup: " + message.text, Errors(other, node).Single());
        }

        [Test]
        public void AddedNode_IsPlacedWhereItWasDropped()
        {
            var graph = InterfaceGraph();
            var node = new VirtualFunctionInputNode();

            graph.AddLocalProviderNode(node, Function(graph, VirtualFunctionTestInterface.kSurface), new Vector2(-300, 40));

            Assert.IsTrue(graph.ContainsNode(node));
            Assert.AreEqual(new Vector2(-300, 40), node.drawState.position.position);
            Assert.AreEqual(VirtualFunctionTestInterface.kSurface, node.providerKey);
        }

        [Test]
        public void VirtualFunction_OffersAnInputNode_AndABaseNodeOnlyWhenItHasABase()
        {
            var graph = InterfaceGraph();

            CollectionAssert.AreEqual(new[] { "Input", "Base" },
                AbstractVirtualFunctionNode.NodesFor(Function(graph, VirtualFunctionTestInterface.kSurface).Definition).Select(n => n.shortName).ToArray());
            CollectionAssert.AreEqual(new[] { "Input" },
                AbstractVirtualFunctionNode.NodesFor(Function(graph, VirtualFunctionTestInterface.kTint).Definition).Select(n => n.shortName).ToArray());
        }

        [Test]
        public void EnsureBaseCall_ChangesThroughTheGraph_AndRefreshesEveryNodeOfThatFunction()
        {
            var graph = InterfaceGraph();
            var output = Output(graph, VirtualFunctionTestInterface.kSurface);
            var input = AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            var vertex = Output(graph, VirtualFunctionTestInterface.kVertex);
            int outputChanges = 0, inputChanges = 0, vertexChanges = 0;
            output.RegisterCallback((_, _) => ++outputChanges);
            input.RegisterCallback((_, _) => ++inputChanges);
            vertex.RegisterCallback((_, _) => ++vertexChanges);

            graph.SetEnsureBaseCall(VirtualFunctionTestInterface.kSurface, false);

            Assert.IsFalse(output.Provider.Definition.EnsureBaseCall);
            Assert.Greater(outputChanges, 0);
            Assert.Greater(inputChanges, 0);
            Assert.AreEqual(0, vertexChanges);
        }
    }
}
