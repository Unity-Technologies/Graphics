using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    [Category("Graph")]
    [Category("Command")]
    class GraphCommandTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void DeleteElementsCommandDeletesAndUnselects([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Minus", Vector2.zero);
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Fake1", Vector2.zero);
            GraphModel.CreateEdge(node0.Input0, node1.Output0);
            GraphTool.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0));

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    var n0 = GetNode(0) as Type0FakeNodeModel;
                    var n1 = GetNode(1) as Type0FakeNodeModel;
                    Assert.NotNull(n0);
                    Assert.NotNull(n1);
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.Output0));
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));
                    return new DeleteElementsCommand(node0, node1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));
                });
        }

        [Test]
        public void MoveElementsCommandForNodesWorks([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node3", Vector2.zero);
            var newPosition0 = new Vector2(50, -75);
            var newPosition1 = new Vector2(60, 25);
            var newPosition2 = new Vector2(-30, 15);
            var deltaAll = new Vector2(100, 100);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(1).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsCommand(newPosition0, GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsCommand(newPosition1, GetNode(1));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsCommand(newPosition2, GetNode(2));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(newPosition2));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(newPosition2));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsCommand(deltaAll, GetNode(0), GetNode(1), GetNode(2), GetNode(3));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0 + deltaAll));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1 + deltaAll));
                    Assert.That(GetNode(2).Position, Is.EqualTo(newPosition2 + deltaAll));
                    Assert.That(GetNode(3).Position, Is.EqualTo(deltaAll));
                });
        }

        [Test]
        public void MoveElementsCommandForStickyNodesWorks([Values] TestingMode mode)
        {
            var origStickyPosition = new Rect(Vector2.zero, new Vector2(100, 100));
            var newStickyPosition = new Rect(Vector2.right * 100, new Vector2(100, 100));
            var stickyNote = (StickyNoteModel)GraphModel.CreateStickyNote(origStickyPosition);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(origStickyPosition));
                    return new MoveElementsCommand(newStickyPosition.position - origStickyPosition.position, stickyNote);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).PositionAndSize.position, Is.EqualTo(newStickyPosition.position));
                });
        }

        [Test]
        public void MoveElementsCommandForMultipleTypesWorks([Values] TestingMode mode)
        {
            var deltaMove = new Vector2(50, -75);
            var itemSize = new Vector2(100, 100);

            var origNodePosition = Vector2.zero;
            var newNodePosition = deltaMove;
            var node = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);

            var origStickyPosition = new Rect(Vector2.one * -100, itemSize);
            var newStickyPosition = new Rect(origStickyPosition.position + deltaMove, itemSize);
            var stickyNote = (StickyNoteModel)GraphModel.CreateStickyNote(origStickyPosition);

            var origPlacematPosition = new Rect(Vector2.one * 200, itemSize);
            var newPlacematPosition = new Rect(origPlacematPosition.position + deltaMove, itemSize);
            var placemat = (PlacematModel)GraphModel.CreatePlacemat(origPlacematPosition);
            placemat.Title = "Blah";

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0).Position, Is.EqualTo(origNodePosition));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(origStickyPosition));
                    Assert.That(GetPlacemat(0).PositionAndSize, Is.EqualTo(origPlacematPosition));
                    return new MoveElementsCommand(deltaMove, new IMovable[] { node, placemat, stickyNote });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newNodePosition));
                    Assert.That(GetStickyNote(0).PositionAndSize.position, Is.EqualTo(newStickyPosition.position));
                    Assert.That(GetPlacemat(0).PositionAndSize.position, Is.EqualTo(newPlacematPosition.position));
                });
        }
    }
}
