using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    [TestFixture]
    class VirtualFunctionStackViewTests : VirtualFunctionFixture
    {
        class HostWindow : EditorWindow
        {
        }

        GraphObject m_GraphObject;
        HostWindow m_Window;
        GraphEditorView m_View;

        GraphData graph => m_GraphObject.graph;

        [SetUp]
        public void SetUp() => m_GraphObject = VirtualFunctionTestInterface.InterfaceGraphObject();

        [TearDown]
        public void TearDown()
        {
            m_View?.Dispose();
            if (m_Window != null)
                m_Window.Close();
            Object.DestroyImmediate(m_GraphObject);
        }

        void Open()
        {
            graph.ClearChanges();
            CreateView();
        }

        void CreateView()
        {
            m_Window = EditorWindow.CreateWindow<HostWindow>();
            m_View = new GraphEditorView(m_Window, graph, new MessageManager(), "Test", true);
            m_Window.rootVisualElement.Add(m_View);
        }

        void HandleChanges(bool wasUndoRedoPerformed = false)
        {
            m_View.HandleGraphChanges(wasUndoRedoPerformed);
            graph.ClearChanges();
        }

        List<VirtualFunctionStackView> Stacks() => m_View.graphView.Query<VirtualFunctionStackView>().ToList();

        string[] StackContexts() => Stacks().Select(s => s.stackData.context).ToArray();

        VirtualFunctionStackView Stack(string context) => Stacks().Single(s => s.stackData.context == context);

        static string[] OutputsIn(VirtualFunctionStackView stack)
            => stack.Children().OfType<MaterialNodeView>().Select(v => v.node.name).ToArray();

        List<MaterialNodeView> NodeViews<T>() where T : AbstractMaterialNode
            => m_View.graphView.Query<MaterialNodeView>().Where(v => v.node is T).ToList();

        List<Toggle> EnsureBaseCallToggles() => m_View.inspectorView.Query<PropertyRow>().ToList()
            .Where(r => (r.label as Label)?.text == "Ensure Base Call").Select(r => r.Q<Toggle>()).ToList();

        [Test]
        public void OpeningTheGraph_PutsEachOutputNodeInItsContextStack()
        {
            Open();

            CollectionAssert.AreEquivalent(new[] { "Vertex", "Fragment", "" }, StackContexts());
            CollectionAssert.AreEqual(new[] { "OnVertex" }, OutputsIn(Stack("Vertex")));
            CollectionAssert.AreEqual(new[] { "OnSurface" }, OutputsIn(Stack("Fragment")));
            CollectionAssert.AreEqual(new[] { "GetTint" }, OutputsIn(Stack("")));
        }

        [Test]
        public void WhileTheModeIsUnavailable_OutputNodesAndStacksAreHidden_AndTheOtherNodesShown()
        {
            VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            GraphData.isInternalMode = () => false;

            Open();

            CollectionAssert.IsEmpty(Stacks());
            CollectionAssert.IsEmpty(NodeViews<VirtualFunctionOutputNode>());
            Assert.AreEqual(1, NodeViews<VirtualFunctionInputNode>().Count);
        }

        [Test]
        public void Stacks_AreChainedInTheirOrder()
        {
            Open();

            Assert.AreEqual(DisplayStyle.None, Stack("Vertex").inputPort.style.display.value);
            Assert.AreEqual(DisplayStyle.None, Stack("").outputPort.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, Stack("Fragment").inputPort.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, Stack("Fragment").outputPort.style.display.value);
            var chain = m_View.graphView.edges.ToList().Where(e => e.input?.node is VirtualFunctionStackView).ToList();
            Assert.AreEqual(2, chain.Count);
            Assert.IsTrue(chain.Any(e => e.output == Stack("Vertex").outputPort && e.input == Stack("Fragment").inputPort));
            Assert.IsTrue(chain.Any(e => e.output == Stack("Fragment").outputPort && e.input == Stack("").inputPort));
        }

        [Test]
        public void OpeningAGraph_AfterItsInterfaceGainedAFunction_ShowsEachOutputNodeOnce()
        {
            var json = MultiJson.Serialize(graph);
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kDefault + VirtualFunctionTestInterface.kLightFunction);
            var loaded = new GraphData();
            MultiJson.Deserialize(loaded, json);
            loaded.ValidateGraph();
            m_GraphObject.graph = loaded;

            // As the window does after loading, without clearing changes first.
            CreateView();
            HandleChanges();

            Assert.AreEqual(4, NodeViews<VirtualFunctionOutputNode>().Count);
            CollectionAssert.AreEquivalent(new[] { "OnSurface", "OnLight" }, OutputsIn(Stack("Fragment")));
        }

        [Test]
        public void GroupingOnlyOutputNodes_CreatesNoGroup()
        {
            Open();
            m_View.graphView.ClearSelection();
            foreach (var view in NodeViews<VirtualFunctionOutputNode>())
                m_View.graphView.AddToSelection(view);

            m_View.graphView.GroupSelection();

            CollectionAssert.IsEmpty(graph.groups);
        }

        [Test]
        public void OutputNodes_CantBeMoved_AndStacksCantBeDeleted()
        {
            Open();

            Assert.IsTrue(NodeViews<VirtualFunctionOutputNode>().All(v => (v.capabilities & Capabilities.Movable) == 0));
            Assert.IsTrue(Stacks().All(s => (s.capabilities & Capabilities.Deletable) == 0));
        }

        [Test]
        public void HotReload_MovesAnOutputNodeToItsNewStack_AndDropsTheEmptyOne()
        {
            Open();

            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.Hints(VirtualFunctionTestInterface.kSurface, "Lighting, 3")
                + "UNITY_VIRTUAL void OnSurface(in float3 positionWS, inout half3 albedo, out half alpha) { alpha = 1; }\n"
                + VirtualFunctionTestInterface.kVertexFunction + VirtualFunctionTestInterface.kTintFunction);
            VirtualFunctionTestInterface.Reload(graph);
            HandleChanges();

            CollectionAssert.AreEquivalent(new[] { "Vertex", "Lighting", "" }, StackContexts());
            CollectionAssert.AreEqual(new[] { "OnSurface" }, OutputsIn(Stack("Lighting")));
        }

        [Test]
        public void DeletingAnOrphanOutputNode_RemovesItAndItsStack()
        {
            Open();
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kSurfaceFunction + VirtualFunctionTestInterface.kTintFunction);
            VirtualFunctionTestInterface.Reload(graph);
            HandleChanges();
            var orphan = VirtualFunctionTestInterface.Output(graph, VirtualFunctionTestInterface.kVertex);

            graph.RemoveElements(new AbstractMaterialNode[] { orphan }, new IEdge[0], new GroupData[0], new StickyNoteData[0]);
            HandleChanges();

            Assert.IsFalse(NodeViews<VirtualFunctionOutputNode>().Any(v => v.node == orphan));
            CollectionAssert.AreEquivalent(new[] { "Fragment", "" }, StackContexts());
        }

        [Test]
        public void Undo_KeepsEachOutputNodeInItsStack()
        {
            Open();
            var saved = new GraphData();
            MultiJson.Deserialize(saved, MultiJson.Serialize(graph));

            graph.ReplaceWith(saved);
            HandleChanges(true);

            Assert.AreEqual(3, NodeViews<VirtualFunctionOutputNode>().Count);
            Assert.IsTrue(NodeViews<VirtualFunctionOutputNode>().All(v => v.GetFirstAncestorOfType<VirtualFunctionStackView>() != null));
            CollectionAssert.AreEquivalent(new[] { "Vertex", "Fragment", "" }, StackContexts());
        }

        [Test]
        [UnityPlatform(exclude = new RuntimePlatform[] {
            RuntimePlatform.LinuxEditor // Disabled for Instability https://unitycorp.atlassian.net/browse/UUM-162552
        })]
        public void InputAndBaseNodes_AreShownOutsideTheStacks()
        {
            Open();

            VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            VirtualFunctionTestInterface.AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface);
            HandleChanges();

            Assert.AreEqual(1, NodeViews<VirtualFunctionInputNode>().Count);
            Assert.AreEqual(1, NodeViews<VirtualFunctionBaseNode>().Count);
            Assert.IsNull(NodeViews<VirtualFunctionInputNode>().Single().GetFirstAncestorOfType<VirtualFunctionStackView>());
        }

        MaterialNodeView ViewOf(AbstractMaterialNode node) => m_View.graphView.Query<MaterialNodeView>().Where(v => v.node == node).First();

        static void Hover(VisualElement view, bool entering)
        {
            using EventBase evt = entering ? MouseEnterEvent.GetPooled() : MouseLeaveEvent.GetPooled();
            evt.target = view;
            view.SendEvent(evt);
        }

        [Test]
        public void HoveringAnOutputNode_HighlightsTheInputAndBaseNodesOfItsFunction_UntilTheMouseLeaves()
        {
            Open();
            var surfaceInput = VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            var surfaceBase = VirtualFunctionTestInterface.AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface);
            var vertexInput = VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kVertex);
            HandleChanges();
            var output = ViewOf(VirtualFunctionTestInterface.Output(graph, VirtualFunctionTestInterface.kSurface));

            Hover(output, true);

            Assert.IsTrue(ViewOf(surfaceInput).ClassListContains(LocalProviderHighlight.kHighlightedClass));
            Assert.IsTrue(ViewOf(surfaceBase).ClassListContains(LocalProviderHighlight.kHighlightedClass));
            Assert.IsFalse(ViewOf(vertexInput).ClassListContains(LocalProviderHighlight.kHighlightedClass));
            Assert.IsFalse(output.ClassListContains(LocalProviderHighlight.kHighlightedClass));

            Hover(output, false);

            Assert.IsFalse(ViewOf(surfaceInput).ClassListContains(LocalProviderHighlight.kHighlightedClass));
            Assert.IsFalse(ViewOf(surfaceBase).ClassListContains(LocalProviderHighlight.kHighlightedClass));
        }

        [Test]
        public void HoveringAnInputNode_HighlightsTheOtherNodesOfItsFunction()
        {
            Open();
            var surfaceInput = VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            var surfaceBase = VirtualFunctionTestInterface.AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface);
            HandleChanges();

            Hover(ViewOf(surfaceInput), true);

            Assert.IsTrue(ViewOf(VirtualFunctionTestInterface.Output(graph, VirtualFunctionTestInterface.kSurface)).ClassListContains(LocalProviderHighlight.kHighlightedClass));
            Assert.IsTrue(ViewOf(surfaceBase).ClassListContains(LocalProviderHighlight.kHighlightedClass));
            Assert.IsFalse(ViewOf(surfaceInput).ClassListContains(LocalProviderHighlight.kHighlightedClass));
        }

        [Test]
        public void InputFieldsOfAnOrphanThatRecovers_LoseTheInactiveStyling()
        {
            var baseCall = VirtualFunctionTestInterface.AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface);
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kVertexFunction + VirtualFunctionTestInterface.kTintFunction);
            VirtualFunctionTestInterface.Reload(graph);
            Open();
            var inputFields = ViewOf(baseCall).Query<PortInputView>().ToList();
            Assert.IsNotEmpty(inputFields);
            Assert.IsTrue(inputFields.All(v => v.ClassListContains("disabled")));

            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kDefault);
            VirtualFunctionTestInterface.Reload(graph);
            HandleChanges();

            Assert.IsTrue(baseCall.isActive);
            Assert.IsFalse(ViewOf(baseCall).Query<PortInputView>().ToList().Any(v => v.ClassListContains("disabled")));
        }

        [Test]
        public void GoToOutputNode_SelectsTheOutputNodeOfTheFunction()
        {
            Open();
            var input = VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kVertex);
            HandleChanges();

            VirtualFunctionNodeMenu.GoToOutputNode(m_View.graphView, input);

            var selected = m_View.graphView.selection.OfType<MaterialNodeView>().Single();
            Assert.AreSame(VirtualFunctionTestInterface.Output(graph, VirtualFunctionTestInterface.kVertex), selected.node);
        }

        [Test]
        public void EnsureBaseCall_ChangedForOneOfTwoSelectedNodes_ShowsInBothInspectors()
        {
            Open();
            VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            VirtualFunctionTestInterface.AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface);
            HandleChanges();
            m_View.graphView.ClearSelection();
            m_View.graphView.AddToSelection(NodeViews<VirtualFunctionInputNode>().Single());
            m_View.graphView.AddToSelection(NodeViews<VirtualFunctionBaseNode>().Single());
            m_View.inspectorView.Update();
            var toggle = EnsureBaseCallToggles()[0];

            using (var evt = ChangeEvent<bool>.GetPooled(true, false))
            {
                evt.target = toggle;
                toggle.SendEvent(evt);
            }

            Assert.AreEqual(2, EnsureBaseCallToggles().Count);
            Assert.IsTrue(EnsureBaseCallToggles().All(t => !t.value));
        }

        [Test]
        public void NewStacks_ArePlacedBelowThePlacedOnes()
        {
            var placed = new VirtualFunctionStackData("Vertex", new List<string>()) { position = new Vector2(100, 50) };
            var added = new VirtualFunctionStackData("Fragment", new List<string>());

            VirtualFunctionStackData.PlaceNew(new[] { placed, added }, new[] { new Vector2(200, 300), new Vector2(200, 100) });

            Assert.IsTrue(added.isPlaced);
            Assert.AreEqual(100, added.position.x);
            Assert.Greater(added.position.y, 350);
        }
    }
}
