using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class PlacematTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        // If Placemat1 is collapsed, Node is hidden. Moving Placemat2
        // should not move the node.
        //
        //        +----------------+   +-----------------+
        //        |  Placemat1     |   |   Placemat2     |
        //        |                |   |                 |
        //        |            +----------+              |
        //        |            | Node     |              |
        //        |            |          |              |
        //        |            +----------+              |
        //        |                |   |                 |
        //        |                |   |                 |
        //        +----------------+   +-----------------+
        //
        [UnityTest]
        public IEnumerator CollapsingAPlacematHidesItsContentToOtherPlacemats([Values] TestingMode mode)
        {
            GraphView.AddToClassList("CollapsingAPlacematHidesItsContentToOtherPlacemats");
            GraphView.AddTestStylesheet("Tests.uss");

            GraphModel.CreatePlacemat(new Rect(0, 0, 200, 200), new SerializableGUID(0, 1));
            GraphModel.CreatePlacemat(new Rect(205, 0, 200, 200), new SerializableGUID(0, 2));
            var nodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(190, 100), new SerializableGUID(0, 3));

            MarkGraphViewStateDirty();
            yield return null;

            var nodeUI = nodeModel.GetUI<Node>(GraphView);
            Assert.IsNotNull(nodeUI);
            nodeUI.style.width = 200;
            nodeUI.style.height = 200;
            yield return null;

            IEnumerable<IGraphElementModel> modelsToMove = null;
            var elementsToMove = new HashSet<GraphElement>();

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 1), out PlacematModel placemat0);
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 2), out PlacematModel placemat1);
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 3), out var node);

                    Assert.IsNotNull(placemat0);
                    Assert.IsNotNull(placemat1);
                    Assert.IsNotNull(node);

                    Assert.IsFalse(placemat0.Collapsed, "Placemat0 is collapsed");
                    Assert.IsFalse(placemat1.Collapsed, "Placemat1 is collapsed");

                    var placematElement = GetGraphElements().
                        OfType<Placemat>().FirstOrDefault(e => e.Model.Guid == placemat0.Guid);
                    elementsToMove.Clear();
                    placematElement?.GetElementsToMove(false, elementsToMove);
                    modelsToMove = elementsToMove.Select(e => e.Model);
                    Assert.IsTrue(modelsToMove.Contains(node), "Placemat0 models-to-move does not contain node");

                    placematElement = GetGraphElements().
                        OfType<Placemat>().FirstOrDefault(e => e.Model.Guid == placemat1.Guid);
                    elementsToMove.Clear();
                    placematElement?.GetElementsToMove(false, elementsToMove);
                    modelsToMove = elementsToMove.Select(e => e.Model);
                    Assert.IsTrue(modelsToMove.Contains(node), "Placemat1 models-to-move does not contain node");
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            MarkGraphViewStateDirty();
                            return TestPhase.WaitForNextFrame;
                        case 1:
                            {
                                GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 1), out PlacematModel placemat0);
                                GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 3), out var node);
                                GraphView.Dispatch(new CollapsePlacematCommand(placemat0, true, new[] { node }));
                                return TestPhase.WaitForNextFrame;
                            }
                        case 2:
                            MarkGraphViewStateDirty();
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 1), out PlacematModel placemat0);
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 2), out PlacematModel placemat1);
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 3), out var node);

                    Assert.IsNotNull(placemat0);
                    Assert.IsNotNull(placemat1);
                    Assert.IsNotNull(node);

                    Assert.IsTrue(placemat0.Collapsed, "Placemat0 is not collapsed");
                    Assert.IsFalse(placemat1.Collapsed, "Placemat1 is collapsed");

                    Assert.IsTrue(placemat0.HiddenElementsGuid.Contains(node.Guid.ToString()), "Placemat0 is not hiding node.");

                    var placematElement = GetGraphElements().
                        OfType<Placemat>().FirstOrDefault(e => e.Model.Guid == placemat1.Guid);

                    elementsToMove.Clear();
                    placematElement?.GetElementsToMove(false, elementsToMove);
                    modelsToMove = elementsToMove.Select(e => e.Model);
                    Assert.IsFalse(modelsToMove.Contains(node), "Placemat1 models-to-move contains node");
                });
        }

        // Moving a node under a collapsed placemat and rebuilding the UI should not hide the node
        // (the placemat should not think it should hide the node because it is in its uncollapsed area).
        //
        //        +----------------+
        //        |  Placemat      |
        //        +----------------+
        //        .                .
        //        .      <<-->>    .
        //        .   +----------+ .
        //        .   | Node     | .
        //        .   |          | .
        //        .   +----------+ .
        //        .                .
        //        . . . . .  . . . .
        //
        [UnityTest]
        public IEnumerator MovingANodeUnderACollapsedPlacematShouldNotHideIt([Values] TestingMode mode)
        {
            {
                // Create a placemat and collapse it.
                var placemat = GraphModel.CreatePlacemat(new Rect(0, 0, 200, 500), new SerializableGUID(0, 1)) as PlacematModel;
                MarkGraphViewStateDirty();
                yield return null;

                GraphView.Dispatch(new CollapsePlacematCommand(placemat, true, new IGraphElementModel[] {}));
                yield return null;

                // Add a node under it.
                GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(10, 100), new SerializableGUID(0, 2));
                MarkGraphViewStateDirty();
                yield return null;
            }

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 1), out PlacematModel placemat);
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 2), out var node);

                    Assert.IsNotNull(placemat);
                    Assert.IsNotNull(node);

                    Assert.IsTrue(placemat.Collapsed, "Placemat is not collapsed");
                    Assert.IsFalse(placemat.HiddenElementsGuid.Contains(node.Guid.ToString()), "Placemat is hiding node.");
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            MarkGraphViewStateDirty();
                            return TestPhase.WaitForNextFrame;
                        case 1:
                            {
                                GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 2), out Type0FakeNodeModel node);
                                GraphView.Dispatch(new MoveElementsCommand(new Vector2(10, 0), node));
                                return TestPhase.WaitForNextFrame;
                            }
                        case 2:
                            MarkGraphViewStateDirty();
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 1), out PlacematModel placemat);
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 2), out var node);

                    Assert.IsNotNull(placemat);
                    Assert.IsNotNull(node);

                    Assert.IsTrue(placemat.Collapsed, "Placemat is not collapsed");
                    Assert.IsFalse(placemat.HiddenElementsGuid.Contains(node.Guid.ToString()), "Placemat is hiding node.");
                }
            );
        }

        // If a node is under a collapsed placemat and the placemat is uncollapsed, the node will
        // fall under the placemat power. Undoing the uncollapse should liberate the node, not hide it.
        //
        //        +----------------+
        //        |  Placemat      |
        //        +----------------+
        //        .                .
        //        .                .
        //        .   +----------+ .
        //        .   | Node     | .
        //        .   |          | .
        //        .   +----------+ .
        //        .                .
        //        . . . . .  . . . .
        //
        [UnityTest]
        public IEnumerator UndoUncollapseShouldLiberateNodeUnderPlacemat([Values] TestingMode mode)
        {
            {
                // Create a placemat and collapse it.
                var placemat = GraphModel.CreatePlacemat(new Rect(0, 0, 200, 500), new SerializableGUID(0, 1)) as PlacematModel;
                MarkGraphViewStateDirty();
                yield return null;

                GraphView.Dispatch(new CollapsePlacematCommand(placemat, true, new IGraphElementModel[] {}));
                yield return null;

                // Add a node under it.
                GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(10, 100), new SerializableGUID(0, 2));
                MarkGraphViewStateDirty();
                yield return null;
            }

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 1), out PlacematModel placemat);
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 2), out var node);

                    Assert.IsNotNull(placemat);
                    Assert.IsNotNull(node);

                    Assert.IsTrue(placemat.Collapsed, "Placemat is not collapsed");
                    Assert.IsFalse(placemat.HiddenElementsGuid.Contains(node.Guid.ToString()), "Placemat is hiding node.");
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            MarkGraphViewStateDirty();
                            return TestPhase.WaitForNextFrame;
                        case 1:
                            {
                                GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 1), out PlacematModel placemat);
                                GraphView.Dispatch(new CollapsePlacematCommand(placemat, false, new IGraphElementModel[] {}));
                                return TestPhase.WaitForNextFrame;
                            }
                        case 2:
                            MarkGraphViewStateDirty();
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 1), out PlacematModel placemat);
                    GraphModel.TryGetModelFromGuid(new SerializableGUID(0, 2), out var node);

                    Assert.IsNotNull(placemat);
                    Assert.IsNotNull(node);

                    Assert.IsFalse(placemat.Collapsed, "Placemat is collapsed");
                    Assert.IsTrue(placemat.HiddenElementsGuid == null || placemat.HiddenElementsGuid.Count == 0, "Placemat is hiding something.");
                }
            );
        }

        [Test(Description = "Test for VSB-865: Sticky Note doesn't collapse with placemats")]
        public void ElementIsHidableByPlacement()
        {
            var placemat = GraphModel.CreatePlacemat(new Rect(0, 0, 200, 500));
            var node = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(10, 100));
            var sticky = (StickyNoteModel)GraphModel.CreateStickyNote(new Rect(100, 100, 50, 50));

            placemat.HiddenElements = new[] { node };
            Assert.AreEqual(1, placemat.HiddenElements.Count(), "Node was not properly added to the list of hidden elements");
            Assert.AreEqual(node, placemat.HiddenElements.First(), "Placemat does not contain the expected node in its hidden elements");

            placemat.HiddenElements = new[] { sticky };
            Assert.AreEqual(1, placemat.HiddenElements.Count(), "Sticky note was not properly added to the list of hidden elements");
            Assert.AreEqual(sticky, placemat.HiddenElements.First(), "Placemat does not contain the expected sticky note in its hidden elements");
        }
    }
}
