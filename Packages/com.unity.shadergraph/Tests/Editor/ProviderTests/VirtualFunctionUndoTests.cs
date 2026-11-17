using System.Linq;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    // Undo and redo the way the window does: Unity restores the graph object, then the graph is replaced from it.
    [TestFixture]
    class VirtualFunctionUndoTests : VirtualFunctionFixture
    {
        GraphObject m_GraphObject;

        GraphData graph => m_GraphObject.graph;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
            m_GraphObject = VirtualFunctionTestInterface.InterfaceGraphObject();
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            Object.DestroyImmediate(m_GraphObject);
        }

        void Record(string actionName)
        {
            Undo.IncrementCurrentGroup();
            m_GraphObject.RegisterCompleteObjectUndo(actionName);
        }

        void PerformUndo()
        {
            Undo.PerformUndo();
            if (m_GraphObject.wasUndoRedoPerformed)
                m_GraphObject.HandleUndoRedo();
        }

        void PerformRedo()
        {
            Undo.PerformRedo();
            if (m_GraphObject.wasUndoRedoPerformed)
                m_GraphObject.HandleUndoRedo();
        }

        string[] OutputKeys() => graph.GetNodes<VirtualFunctionOutputNode>().Select(o => o.providerKey).OrderBy(k => k).ToArray();

        IProvider<IVirtualFunction> Function(string key) => VirtualFunctionTestInterface.Function(graph, key);

        static MaterialSlot Slot(AbstractMaterialNode node, string name) => node.GetSlots<MaterialSlot>().Single(s => s.shaderOutputName == name);

        int InputToOutputEdges() => graph.edges.Count(e => e.outputSlot.node is VirtualFunctionInputNode && e.inputSlot.node is VirtualFunctionOutputNode);

        void AssertEveryVirtualFunctionNodeResolves()
        {
            foreach (var node in graph.GetNodes<AbstractVirtualFunctionNode>())
                Assert.AreSame(Function(node.providerKey), node.Provider, node.name);
        }

        [Test]
        public void AddingANode_UndoesAndRedoes()
        {
            Record("Add Input");
            VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);

            PerformUndo();
            CollectionAssert.IsEmpty(graph.GetNodes<VirtualFunctionInputNode>());

            PerformRedo();
            Assert.AreEqual(1, graph.GetNodes<VirtualFunctionInputNode>().Count());
            AssertEveryVirtualFunctionNodeResolves();
        }

        [Test]
        public void EnsureBaseCall_UndoesAndRedoes_AndNodesFollowTheRestoredProvider()
        {
            Record("Don't Ensure Base Call");
            graph.SetEnsureBaseCall(VirtualFunctionTestInterface.kSurface, false);

            PerformUndo();
            Assert.IsTrue(Function(VirtualFunctionTestInterface.kSurface).Definition.EnsureBaseCall);
            AssertEveryVirtualFunctionNodeResolves();

            PerformRedo();
            Assert.IsFalse(Function(VirtualFunctionTestInterface.kSurface).Definition.EnsureBaseCall);
            AssertEveryVirtualFunctionNodeResolves();
        }

        [Test]
        public void PickingAnotherInterface_UndoesAndRedoes()
        {
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kSurfaceFunction + VirtualFunctionTestInterface.kLightFunction,
                VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName);
            var vertexInput = VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kVertex);
            var surface = VirtualFunctionTestInterface.Output(graph, VirtualFunctionTestInterface.kSurface);
            graph.Connect(Slot(vertexInput, "positionOS").slotReference, Slot(surface, "albedo").slotReference);
            graph.virtualFunctionStacks.Single(s => s.context == "Vertex").position = new Vector2(10, 20);

            Record("Change Shader Interface");
            graph.SetShaderInterface(VirtualFunctionTestInterface.Interface(VirtualFunctionTestInterface.kOtherPath, VirtualFunctionTestInterface.kOtherName));

            PerformUndo();
            Assert.AreEqual(VirtualFunctionTestInterface.kName, graph.shaderInterface.Info.ProviderKey);
            CollectionAssert.AreEqual(new[] { VirtualFunctionTestInterface.kSurface, VirtualFunctionTestInterface.kTint, VirtualFunctionTestInterface.kVertex }, OutputKeys());
            Assert.AreEqual(1, graph.GetNodes<VirtualFunctionInputNode>().Count());
            CollectionAssert.AreEqual(new[] { "Vertex", "Fragment", "" }, VirtualFunctionTestInterface.StackContexts(graph));
            Assert.AreEqual(new Vector2(10, 20), graph.virtualFunctionStacks.Single(s => s.context == "Vertex").position);
            Assert.AreEqual(1, InputToOutputEdges());
            AssertEveryVirtualFunctionNodeResolves();

            PerformRedo();
            CollectionAssert.AreEqual(
                new[] { VirtualFunctionTestInterface.kLight, VirtualFunctionTestInterface.kSurface, VirtualFunctionTestInterface.kTint, VirtualFunctionTestInterface.kVertex },
                OutputKeys());
            Assert.IsTrue(graph.GetNodes<VirtualFunctionInputNode>().Single().isOrphaned);
            Assert.AreEqual(1, InputToOutputEdges());
            CollectionAssert.AreEqual(new[] { "Fragment", "Vertex", "" }, VirtualFunctionTestInterface.StackContexts(graph));
        }

        [Test]
        public void LeavingTheMode_UndoesAndRedoes()
        {
            VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);

            Undo.IncrementCurrentGroup();
            graph.SwitchShaderInterfaceMode(false);

            PerformUndo();
            Assert.IsTrue(graph.isShaderInterfaceMode);
            Assert.AreEqual(3, OutputKeys().Length);
            Assert.IsFalse(graph.GetNodes<VirtualFunctionInputNode>().Single().isOrphaned);

            PerformRedo();
            Assert.IsFalse(graph.isShaderInterfaceMode);
            CollectionAssert.IsEmpty(graph.GetNodes<VirtualFunctionOutputNode>());
        }

        [Test]
        public void DeletingAnOrphanOutputNode_Undoes_BackIntoItsStack()
        {
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kSurfaceFunction + VirtualFunctionTestInterface.kTintFunction);
            VirtualFunctionTestInterface.Reload(graph);
            var orphan = VirtualFunctionTestInterface.Output(graph, VirtualFunctionTestInterface.kVertex);

            Record("Remove Elements");
            graph.RemoveElements(new AbstractMaterialNode[] { orphan }, new IEdge[0], new GroupData[0], new StickyNoteData[0]);
            Assert.AreEqual(2, OutputKeys().Length);

            PerformUndo();
            Assert.IsTrue(VirtualFunctionTestInterface.Output(graph, VirtualFunctionTestInterface.kVertex).isOrphaned);
            Assert.AreEqual("Vertex", VirtualFunctionTestInterface.StackOf(graph, VirtualFunctionTestInterface.kVertex));
        }

        [Test]
        public void MovingAStack_Undoes()
        {
            graph.virtualFunctionStacks[0].position = new Vector2(10, 20);

            Record("Move Elements");
            graph.virtualFunctionStacks[0].position = new Vector2(300, 400);

            PerformUndo();
            Assert.AreEqual(new Vector2(10, 20), graph.virtualFunctionStacks[0].position);
        }

        [Test]
        public void UndoAfterAReloadAddedAFunction_KeepsOneOutputNodePerFunction()
        {
            Record("Add Input");
            VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kDefault + VirtualFunctionTestInterface.kLightFunction);
            VirtualFunctionTestInterface.Reload(graph);
            Assert.AreEqual(4, OutputKeys().Length);

            PerformUndo();
            CollectionAssert.AreEqual(new[] { VirtualFunctionTestInterface.kLight, VirtualFunctionTestInterface.kSurface, VirtualFunctionTestInterface.kTint, VirtualFunctionTestInterface.kVertex }, OutputKeys());

            PerformRedo();
            Assert.AreEqual(4, OutputKeys().Length);
            AssertEveryVirtualFunctionNodeResolves();
        }

        [Test]
        public void RepeatedUndoAndRedo_DoesNotAccumulateNodes()
        {
            Record("Add Base");
            VirtualFunctionTestInterface.AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface);

            for (int i = 0; i < 3; ++i)
            {
                PerformUndo();
                PerformRedo();
            }

            Assert.AreEqual(3, OutputKeys().Length);
            Assert.AreEqual(1, graph.GetNodes<VirtualFunctionBaseNode>().Count());
            Assert.AreEqual(3, graph.virtualFunctionStacks.Sum(s => s.providerKeys.Count));
        }
    }
}
