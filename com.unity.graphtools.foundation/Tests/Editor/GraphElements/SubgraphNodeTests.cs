using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class SubgraphNodeTests : BaseFixture<NoUIBlackboardTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;

        IVariableDeclarationModel m_Variable0;
        IVariableDeclarationModel m_Variable1;
        IVariableDeclarationModel m_Variable2;
        IVariableDeclarationModel m_Variable3;

        IPortModel m_Port0;
        IPortModel m_Port1;
        IPortModel m_Port2;
        IPortModel m_Port3;

        ISubgraphNodeModel m_SubgraphNodeModel;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            SetupSubgraphNode();
        }

        static string GetPortTitle(IPortModel port) => (port as IHasTitle)?.Title ?? port.UniqueName;

        void SetupSubgraphNode()
        {
            m_Variable0 = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "input0", ModifierFlags.Read, true);
            m_Variable1 = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "input1", ModifierFlags.Read, true);
            m_Variable2 = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "input2", ModifierFlags.Read, true);
            m_Variable3 = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "input3", ModifierFlags.Read, true);

            Assert.That(GetSectionItem(0).Guid, Is.EqualTo(m_Variable0.Guid));
            Assert.That(GetSectionItem(1).Guid, Is.EqualTo(m_Variable1.Guid));
            Assert.That(GetSectionItem(2).Guid, Is.EqualTo(m_Variable2.Guid));
            Assert.That(GetSectionItem(3).Guid, Is.EqualTo(m_Variable3.Guid));

            GraphModel.CreateSubgraphNode(GraphModel, Vector2.zero);
            m_SubgraphNodeModel = GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.IsNotNull(m_SubgraphNodeModel);

            m_Port0 = m_SubgraphNodeModel.InputsByDisplayOrder[0];
            m_Port1 = m_SubgraphNodeModel.InputsByDisplayOrder[1];
            m_Port2 = m_SubgraphNodeModel.InputsByDisplayOrder[2];
            m_Port3 = m_SubgraphNodeModel.InputsByDisplayOrder[3];

            Assert.IsNotNull(m_Port0);
            Assert.IsNotNull(m_Port1);
            Assert.IsNotNull(m_Port2);
            Assert.IsNotNull(m_Port3);

            Assert.That(GetPortTitle(m_Port0), Is.EqualTo(m_Variable0.Title));
            Assert.That(GetPortTitle(m_Port1), Is.EqualTo(m_Variable1.Title));
            Assert.That(GetPortTitle(m_Port2), Is.EqualTo(m_Variable2.Title));
            Assert.That(GetPortTitle(m_Port3), Is.EqualTo(m_Variable3.Title));
        }

        [Test]
        [TestCase(TestingMode.Command, 0, 2, new[] { 1, 2, 0, 3 })]
        [TestCase(TestingMode.UndoRedo, 0, 2, new[] { 1, 2, 0, 3 })]
        [TestCase(TestingMode.Command, 0, 0, new[] { 0, 1, 2, 3 })]
        [TestCase(TestingMode.Command, 0, 0, new[] { 0, 1, 2, 3 })]
        [TestCase(TestingMode.Command, 3, 3, new[] { 0, 1, 2, 3 })]
        [TestCase(TestingMode.Command, 0, 3, new[] { 1, 2, 3, 0 })]
        [TestCase(TestingMode.Command, 3, 0, new[] { 0, 3, 1, 2 })]
        [TestCase(TestingMode.Command, 1, 1, new[] { 0, 1, 2, 3 })]
        [TestCase(TestingMode.Command, 1, 2, new[] { 0, 2, 1, 3 })]
        public void Test_ReorderVariableShouldReorderSubgraphNodePort(TestingMode mode, int indexToMove, int afterWhich, int[] expectedOrder)
        {
            var variables = new[] { m_Variable0, m_Variable1, m_Variable2, m_Variable3 };

            TestPrereqCommandPostreq(mode,
            () =>
                {
                    Assert.That(GetSectionItem(0).Guid, Is.EqualTo(m_Variable0.Guid));
                    Assert.That(GetSectionItem(1).Guid, Is.EqualTo(m_Variable1.Guid));
                    Assert.That(GetSectionItem(2).Guid, Is.EqualTo(m_Variable2.Guid));
                    Assert.That(GetSectionItem(3).Guid, Is.EqualTo(m_Variable3.Guid));
                    return new ReorderGroupItemsCommand(GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First()), variables[afterWhich], variables[indexToMove]);
                },
                () =>
                {
                    Assert.That(GetSectionItem(0).Guid, Is.EqualTo(variables[expectedOrder[0]].Guid));
                    Assert.That(GetSectionItem(1).Guid, Is.EqualTo(variables[expectedOrder[1]].Guid));
                    Assert.That(GetSectionItem(2).Guid, Is.EqualTo(variables[expectedOrder[2]].Guid));
                    Assert.That(GetSectionItem(3).Guid, Is.EqualTo(variables[expectedOrder[3]].Guid));

                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[0]), Is.EqualTo(variables[expectedOrder[0]].Title));
                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[1]), Is.EqualTo(variables[expectedOrder[1]].Title));
                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[2]), Is.EqualTo(variables[expectedOrder[2]].Title));
                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[3]), Is.EqualTo(variables[expectedOrder[3]].Title));
                });
        }

        [Test]
        public void Test_DeleteVariableShouldReorderSubgraphNodePort([Values] TestingMode mode)
        {
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetSectionItem(0).Guid, Is.EqualTo(m_Variable0.Guid));
                    Assert.That(GetSectionItem(1).Guid, Is.EqualTo(m_Variable1.Guid));
                    Assert.That(GetSectionItem(2).Guid, Is.EqualTo(m_Variable2.Guid));
                    Assert.That(GetSectionItem(3).Guid, Is.EqualTo(m_Variable3.Guid));
                    return new DeleteElementsCommand(m_Variable1);
                },
                () =>
                {
                    Assert.That(GetSectionItem(0).Guid, Is.EqualTo(m_Variable0.Guid));
                    Assert.That(GetSectionItem(1).Guid, Is.EqualTo(m_Variable2.Guid));
                    Assert.That(GetSectionItem(2).Guid, Is.EqualTo(m_Variable3.Guid));

                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[0]), Is.EqualTo(m_Variable0.Title));
                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[1]), Is.EqualTo(m_Variable2.Title));
                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[2]), Is.EqualTo(m_Variable3.Title));
                });
        }

        [Test]
        public void Test_CreateVariableShouldReorderSubgraphNodePort([Values] TestingMode mode)
        {
            var newVariableGuid = new SerializableGUID("bob");
            const string newVariableTitle = "test";

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetSectionItem(0).Guid, Is.EqualTo(m_Variable0.Guid));
                    Assert.That(GetSectionItem(1).Guid, Is.EqualTo(m_Variable1.Guid));
                    Assert.That(GetSectionItem(2).Guid, Is.EqualTo(m_Variable2.Guid));
                    Assert.That(GetSectionItem(3).Guid, Is.EqualTo(m_Variable3.Guid));
                    // Create variable at index 1
                    return new CreateGraphVariableDeclarationCommand(newVariableTitle, true, TypeHandle.Float, indexInGroup: 1, modifierFlags: ModifierFlags.Read, guid: newVariableGuid);
                },
                () =>
                {
                    Assert.That(GetSectionItem(0).Guid, Is.EqualTo(m_Variable0.Guid));
                    Assert.That(GetSectionItem(1).Guid, Is.EqualTo(newVariableGuid));
                    Assert.That(GetSectionItem(2).Guid, Is.EqualTo(m_Variable1.Guid));
                    Assert.That(GetSectionItem(3).Guid, Is.EqualTo(m_Variable2.Guid));
                    Assert.That(GetSectionItem(4).Guid, Is.EqualTo(m_Variable3.Guid));

                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[0]), Is.EqualTo(m_Variable0.Title));
                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[1]), Is.EqualTo(newVariableTitle));
                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[2]), Is.EqualTo(m_Variable1.Title));
                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[3]), Is.EqualTo(m_Variable2.Title));
                    Assert.That(GetPortTitle(m_SubgraphNodeModel.InputsByDisplayOrder[4]), Is.EqualTo(m_Variable3.Title));
                });
        }
    }

    class CreateSubgraphNodeFromSelectionTests : GraphViewTester
    {
        Type0FakeNodeModel FirstNodeModel { get; set; }
        Type0FakeNodeModel SecondNodeModel { get; set; }
        Type0FakeNodeModel ThirdNodeModel { get; set; }
        Type0FakeNodeModel FourthNodeModel { get; set; }
        IPlacematModel PlacematModel { get; set; }
        IStickyNoteModel StickyNoteModel { get; set; }

        IGraphAsset m_ReferenceGraphAsset;
        IGraphAsset m_CurrentGraphAsset;

        const string k_CurrentGraphName = "Current Graph";

        IEnumerator CreateElements()
        {
            // Configuration
            // +----+
            // | 1  o----
            // +----+   |    +----+    +----+
            //          +----o 3  o----o 4  |
            // +----+   |    +----+    +----+
            // | 2  o----
            // +----+
            // +--------+  +------+
            // |placemat|  |sticky|
            // +--------+  +------+

            FirstNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(0, 50));
            SecondNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(0, 250));
            ThirdNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", new Vector2(200, 150));
            FourthNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node4", new Vector2(400, 150));
            PlacematModel = CreatePlacemat(new Rect(new Vector2(0, 450), new Vector2(100, 100)), "Placemat");
            StickyNoteModel = CreateSticky("Sticky", "", new Rect(new Vector2(200, 450), new Vector2(100, 100)));

            MarkGraphViewStateDirty();
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(FirstNodeModel.ExeOutput0, ThirdNodeModel.ExeInput0);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(SecondNodeModel.ExeOutput0, ThirdNodeModel.ExeInput0);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(ThirdNodeModel.ExeOutput0, FourthNodeModel.ExeInput0);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        void CreateSubgraph(List<IGraphElementModel> sourceElements)
        {
            var template = new GraphTemplate<ClassStencil>("subgraph");
            GraphView.Dispatch(new CreateSubgraphCommand(typeof(ClassGraphAsset), sourceElements, template, GraphView, ""));
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // Register the command
            GraphView.RegisterCommandHandler<CreateSubgraphCommand>(CreateSubgraphCommand.DefaultCommandHandler);

            var template = new GraphTemplate<ClassStencil>(k_CurrentGraphName);
            m_CurrentGraphAsset = GraphAssetCreationHelpers<TestGraphAsset>.CreateGraphAsset(typeof(ClassStencil), k_CurrentGraphName, $"Assets/{k_CurrentGraphName}.asset", template);
            GraphView.Dispatch(new LoadGraphCommand(m_CurrentGraphAsset.GraphModel));
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            var assetPaths = AssetDatabase.FindAssets($"t:{typeof(TestGraphAsset)}").Select(AssetDatabase.GUIDToAssetPath);

            foreach (var assetPath in assetPaths)
                AssetDatabase.DeleteAsset(assetPath);
        }

        [UnityTest]
        public IEnumerator CreateSubgraphWorks()
        {
            var actions = CreateElements();
            while (actions.MoveNext())
            {
                yield return null;
            }

            var sourceElements = new List<IGraphElementModel> { ThirdNodeModel, PlacematModel, StickyNoteModel };

            CreateSubgraph(sourceElements);

            var subgraphNode = GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.IsNotNull(subgraphNode);

            var selection = GraphView.GetSelection();
            Assert.AreEqual(1, selection.Count);
            Assert.AreEqual(subgraphNode, selection.First());

            var subgraphGraphModel = subgraphNode.SubgraphModel;

            var sourceNodes = sourceElements.OfType<NodeModel>().ToList();
            var nodesInSubgraph = subgraphGraphModel.NodeModels.OfType<Type0FakeNodeModel>().ToList();
            Assert.AreEqual(sourceNodes.Count, nodesInSubgraph.Count);

            foreach (var nodeInSubgraph in nodesInSubgraph)
                Assert.AreEqual(1, sourceNodes.Count(sourceNode => sourceNode.Title == nodeInSubgraph.Title));

            var sourcePlacemats = sourceElements.OfType<PlacematModel>().ToList();
            var placematsInSubgraph = subgraphGraphModel.PlacematModels.ToList();
            Assert.AreEqual(sourcePlacemats.Count, placematsInSubgraph.Count);

            foreach (var placematInSubgraph in placematsInSubgraph)
                Assert.AreEqual(1, sourcePlacemats.Count(sourcePlacemat => sourcePlacemat.Title == ((PlacematModel)placematInSubgraph).Title));

            var sourceStickyNotes = sourceElements.OfType<StickyNoteModel>().ToList();
            var stickyNotesInSubgraph = subgraphGraphModel.StickyNoteModels.ToList();
            Assert.AreEqual(sourceStickyNotes.Count(), stickyNotesInSubgraph.Count);

            foreach (var stickyNoteInSubgraph in stickyNotesInSubgraph)
                Assert.AreEqual(1, sourceStickyNotes.Count(sourceStickyNote => sourceStickyNote.Title == ((StickyNoteModel)stickyNoteInSubgraph).Title));
        }

        [UnityTest]
        public IEnumerator ElementCountIsCoherent()
        {
            var actions = CreateElements();
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Current graph contains 9 elements: Node1, Node2, Node3, Node4, the placemat, the sticky note and 3 edges.
            var initialElementCount = GraphModel.NodeModels.Count + GraphModel.PlacematModels.Count + GraphModel.StickyNoteModels.Count + GraphModel.EdgeModels.Count;
            Assert.AreEqual(9, initialElementCount);

            var sourceElements = new List<IGraphElementModel> { FirstNodeModel, ThirdNodeModel, StickyNoteModel };

            CreateSubgraph(sourceElements);

            // Current graph now contains 6 elements: Node2, the new subgraph node, Node4, 2 edges and the placemat:
            // +----+     +-----+    +----+
            // | 2  o-----o sub o----o 4  |
            // +----      +-----+    +----+
            // +--------+
            // |placemat|
            // +--------+

            var elementCountAfterSubgraphCreation = GraphModel.NodeModels.Count + GraphModel.PlacematModels.Count + GraphModel.StickyNoteModels.Count + GraphModel.EdgeModels.Count;
            Assert.AreEqual(6, elementCountAfterSubgraphCreation);
        }

        [UnityTest]
        public IEnumerator SubgraphNodePortsAndSubgraphIOHaveSameTitles()
        {
            var actions = CreateElements();
            while (actions.MoveNext())
            {
                yield return null;
            }

            var sourceElements = new List<IGraphElementModel> { FirstNodeModel, ThirdNodeModel, PlacematModel };

            CreateSubgraph(sourceElements);

            var subgraphNodeModel = GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.IsNotNull(subgraphNodeModel);

            var subgraphIOCount = subgraphNodeModel.SubgraphModel.VariableDeclarations.Count(v => v.IsInputOrOutput());
            Assert.AreEqual(subgraphIOCount, subgraphNodeModel.Ports.Count());

            foreach (var port in subgraphNodeModel.Ports)
                Assert.IsTrue(subgraphNodeModel.SubgraphModel.VariableDeclarations.Any(v => v.Title == (port as IHasTitle)?.Title));
        }

        [UnityTest]
        public IEnumerator SubgraphNodeShouldKeepConnections()
        {
            var actions = CreateElements();
            while (actions.MoveNext())
            {
                yield return null;
            }

            var connectedEdges = ThirdNodeModel.GetConnectedEdges();
            foreach (var edge in connectedEdges)
            {
                if (edge.ToPort.NodeModel != ThirdNodeModel)
                    Assert.IsTrue(edge.ToPort.NodeModel.Equals(FourthNodeModel));
                else if (edge.FromPort.NodeModel != ThirdNodeModel)
                    Assert.IsTrue(edge.FromPort.NodeModel.Equals(FirstNodeModel) || edge.FromPort.NodeModel.Equals(SecondNodeModel));
            }

            var sourceElements = new List<IGraphElementModel> { ThirdNodeModel };

            CreateSubgraph(sourceElements);

            var subgraphNodeModel = GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.IsNotNull(subgraphNodeModel);

            var connectedEdgesToSubgraphNode = subgraphNodeModel.GetConnectedEdges();
            foreach (var edge in connectedEdgesToSubgraphNode)
            {
                if (edge.ToPort.NodeModel != subgraphNodeModel)
                    Assert.IsTrue(edge.ToPort.NodeModel.Equals(FourthNodeModel));
                else if (edge.FromPort.NodeModel != subgraphNodeModel)
                    Assert.IsTrue(edge.FromPort.NodeModel.Equals(FirstNodeModel) || edge.FromPort.NodeModel.Equals(SecondNodeModel));
            }
        }

        [UnityTest]
        public IEnumerator NewInputsShouldHaveDifferentPositions()
        {
            //               +------+
            //          +----o      |
            // +----+   |    |   2  |
            // | 1  o---+----o      |
            // +----+        +------+

            FirstNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(0, 50));
            SecondNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(200, 50));
            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(FirstNodeModel.Output0, SecondNodeModel.Input0);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(FirstNodeModel.Output0, SecondNodeModel.Input1);
            while (actions.MoveNext())
            {
                yield return null;
            }

            CreateSubgraph(new List<IGraphElementModel>{ SecondNodeModel });

            var subgraphNodeModel = GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.NotNull(subgraphNodeModel);

            var subgraph = subgraphNodeModel.SubgraphModel;
            Assert.NotNull(subgraph);

            var inputs = subgraph.NodeModels.OfType<IVariableNodeModel>().ToList();
            Assert.AreEqual(2, inputs.Count);

            var firstInputNode = inputs[0];
            Assert.NotNull(firstInputNode);
            var secondInputNode = inputs[1];
            Assert.NotNull(secondInputNode);

            Assert.AreNotEqual(firstInputNode.Position, secondInputNode.Position);
        }

        [UnityTest]
        public IEnumerator NewOutputsShouldHaveDifferentPositions()
        {
            //                +----+
            // +------+   +---o 2  |
            // |  1   o---+   +----+
            // |      o---+   +----+
            // +------+   +---o 3  |
            //                +----+

            FirstNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(0, 100));
            SecondNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(250, 50));
            var thirdNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", new Vector2(250, 250));
            Assert.NotNull(FirstNodeModel);
            Assert.NotNull(SecondNodeModel);
            Assert.NotNull(thirdNodeModel);
            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(FirstNodeModel.Output0, SecondNodeModel.Input0);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(FirstNodeModel.Output1, thirdNodeModel.Input0);
            while (actions.MoveNext())
            {
                yield return null;
            }

            CreateSubgraph(new List<IGraphElementModel>{ FirstNodeModel });

            var subgraphNodeModel = GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.NotNull(subgraphNodeModel);

            var subgraph = subgraphNodeModel.SubgraphModel;
            Assert.NotNull(subgraph);

            var outputs = subgraph.NodeModels.OfType<IVariableNodeModel>().ToList();
            Assert.AreEqual(2, outputs.Count);

            var firstOutputNode = outputs[0];
            Assert.NotNull(firstOutputNode);
            var secondOutputNode = outputs[1];
            Assert.NotNull(secondOutputNode);

            Assert.AreNotEqual(firstOutputNode.Position, secondOutputNode.Position);
        }
        [UnityTest]
        public IEnumerator ShouldCreateOnlyOneInput()
        {
            // +----+
            // | 1  o---+    +----+
            // +----+   +----o 3  |
            // +----+   |    +----+
            // | 2  o---+
            // +----+

            FirstNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(0, 50));
            SecondNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(0, 250));
            var thirdNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", new Vector2(250, 100));
            Assert.NotNull(FirstNodeModel);
            Assert.NotNull(SecondNodeModel);
            Assert.NotNull(thirdNodeModel);
            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(FirstNodeModel.ExeOutput0, thirdNodeModel.ExeInput0);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(SecondNodeModel.ExeOutput0, thirdNodeModel.ExeInput0);
            while (actions.MoveNext())
            {
                yield return null;
            }

            CreateSubgraph(new List<IGraphElementModel>{ thirdNodeModel });

            var subgraphNodeModel = GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.NotNull(subgraphNodeModel);

            var subgraph = subgraphNodeModel.SubgraphModel;
            Assert.NotNull(subgraph);

            var inputs = subgraph.NodeModels.OfType<IVariableNodeModel>().ToList();
            Assert.AreEqual(1, inputs.Count);
        }

        [UnityTest]
        public IEnumerator ShouldCreateOnlyOneOutput()
        {
            //                +----+
            // +------+   +---o 2  |
            // |  1   o---+   +----+
            // |      |   |   +----+
            // +------+   +---o 3  |
            //                +----+

            FirstNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(0, 100));
            SecondNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(250, 50));
            var thirdNodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", new Vector2(250, 250));
            Assert.NotNull(FirstNodeModel);
            Assert.NotNull(SecondNodeModel);
            Assert.NotNull(thirdNodeModel);
            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(FirstNodeModel.Output0, SecondNodeModel.Input0);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(FirstNodeModel.Output0, thirdNodeModel.Input0);
            while (actions.MoveNext())
            {
                yield return null;
            }

            CreateSubgraph(new List<IGraphElementModel>{ FirstNodeModel });

            var subgraphNodeModel = GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.NotNull(subgraphNodeModel);

            var subgraph = subgraphNodeModel.SubgraphModel;
            Assert.NotNull(subgraph);

            var outputs = subgraph.NodeModels.OfType<IVariableNodeModel>().ToList();
            Assert.AreEqual(1, outputs.Count);
        }
    }
}
