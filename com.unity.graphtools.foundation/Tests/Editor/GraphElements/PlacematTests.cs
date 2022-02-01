using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class PlacematTests : GraphViewTester
    {
        public enum TestType
        {
            Default,
            Collapsed
        }

        public enum ElementType
        {
            Node,
            StickyNote
        }

        static readonly Vector2 k_DefaultPlacematPos = new Vector2(150, 300);
        static readonly Vector2 k_DefaultPlacematSize = new Vector2(250, 250);
        static readonly Vector2 k_SecondPlacematSize = new Vector2(150, 150);
        static readonly Rect k_DefaultPlacematRect = new Rect(k_DefaultPlacematPos, k_DefaultPlacematSize);
        static readonly Vector2 k_SelectionOffset = new Vector2(35, 35);
        static readonly Vector2 k_DefaultNodeSize = new Vector2(80, 50);

        static readonly Vector2 k_NoNode = Vector2.negativeInfinity;
        static readonly Vector2 k_NoSecondPlacemat = Vector2.negativeInfinity;

        IInputOutputPortsNodeModel AddNode(Vector2 pos)
        {
            return CreateNode("", pos);
        }

        IStickyNoteModel AddSticky(Vector2 pos)
        {
            return CreateSticky("FOO", "bar", new Rect(pos, k_DefaultNodeSize));
        }

        IInputOutputPortsNodeModel AddNode(Vector2 pos, PortDirection direction, PortOrientation orientation = PortOrientation.Horizontal)
        {
            if (direction == PortDirection.Input)
                return CreateNode("", pos, 1, 0, orientation: orientation);
            return CreateNode("", pos, 0, 1, orientation: orientation);
        }

        IEnumerator PlacematTestMove(Vector2 startElementPos, Vector2 startSecondPmPos, ElementType elementType, TestType testType, EventModifiers modifier)
        {
            IPlacematModel pmModel = CreatePlacemat(GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematRect));

            IGraphElementModel elementModel = null;
            if (!float.IsInfinity(startElementPos.magnitude))
            {
                switch (elementType)
                {
                    case ElementType.Node:
                        elementModel = AddNode(GraphViewStaticBridge.RoundToPixelGrid(startElementPos));
                        break;
                    case ElementType.StickyNote:
                        elementModel = AddSticky(GraphViewStaticBridge.RoundToPixelGrid(startElementPos));
                        break;
                }
            }

            IPlacematModel pm2Model = null;
            if (!float.IsInfinity(startSecondPmPos.magnitude))
            {
                pm2Model = CreatePlacemat(new Rect(GraphViewStaticBridge.RoundToPixelGrid(startSecondPmPos), k_SecondPlacematSize));
                pm2Model.Color = Color.red;

                Assert.Greater(pm2Model.GetZOrder(), pmModel.GetZOrder());
            }

            MarkGraphViewStateDirty();
            yield return null;

            var element = elementModel.GetUI<GraphElement>(graphView);
            var pm = pmModel.GetUI<Placemat>(graphView);
            var pm2 = pm2Model.GetUI<Placemat>(graphView);

            string Elem() => elementType == ElementType.Node ? "Node" : "Sticky note";

            bool testCollapsed = testType == TestType.Collapsed;
            if (testCollapsed)
            {
                if (element != null)
                    Assert.IsTrue(element.visible, $"{Elem()} should be visible prior to main placemat collapsing.");

                if (pm2 != null)
                    Assert.IsTrue(pm2.visible, "Overlapping placemat should be visible prior to main placemat collapsing.");

                pm.CollapsePlacemat(true);

                MarkGraphViewStateDirty();
                yield return null;

                element = elementModel.GetUI<GraphElement>(graphView);
                pm = pmModel.GetUI<Placemat>(graphView);
                pm2 = pm2Model.GetUI<Placemat>(graphView);

                if (element != null)
                    Assert.IsFalse(element.visible, $"{Elem()} should not be visible after main placemat collapsing.");

                if (pm2 != null)
                    Assert.IsFalse(pm2.visible, "Overlapping placemat should not be visible after main placemat collapsing.");
            }

            Vector2 moveDelta = GraphViewStaticBridge.RoundToPixelGrid(new Vector2(20, 20));

            // Move!
            {
                var worldPmPosition = graphView.ContentViewContainer.LocalToWorld(pm.layout.position);
                var start = worldPmPosition + k_SelectionOffset;
                var end = start + moveDelta;
                helpers.DragTo(start, end, eventModifiers: modifier);
                yield return null;
            }

            yield return null;

            element = elementModel.GetUI<GraphElement>(graphView);
            pm = pmModel.GetUI<Placemat>(graphView);
            pm2 = pm2Model.GetUI<Placemat>(graphView);

            // Main placemat will always move.
            // The node and second placemat will not move if and only if Shift is pressed (so we move only the main
            // placemat) and the main placemat is not collapsed.
            Vector2 expectedPlacematPos = GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematRect.position + moveDelta);
            Vector2 expectedNodePos = GraphViewStaticBridge.RoundToPixelGrid(startElementPos);
            Vector2 expectedSecondPlacematPos = GraphViewStaticBridge.RoundToPixelGrid(startSecondPmPos);
            string errorMessage = "have moved following manipulation.";
            if (testCollapsed || modifier != EventModifiers.Shift)
            {
                expectedNodePos += moveDelta;
                expectedSecondPlacematPos += moveDelta;
                errorMessage = "not have moved following manipulation because ";
                if (testCollapsed)
                    errorMessage += "main placemat is collapsed.";
                else
                    errorMessage += "main placemat was moved in 'slid under' mode.";
            }

            Assert.AreEqual(expectedPlacematPos, pm.layout.position, "Main placemat should have moved following manipulation.");
            if (element != null)
                Assert.AreEqual(expectedNodePos, element.layout.position, $"{Elem()} should " + errorMessage);

            if (pm2 != null)
                Assert.AreEqual(expectedSecondPlacematPos, pm2.layout.position, "Overlapping placemat should " + errorMessage);

            if (testCollapsed)
            {
                pm.CollapsePlacemat(false);
                yield return null;

                element = elementModel.GetUI<GraphElement>(graphView);
                pm2 = pm2Model.GetUI<Placemat>(graphView);

                if (element != null)
                    Assert.IsTrue(element.visible, $"{Elem()} should be visible after main placemat uncollapsing.");

                if (pm2 != null)
                    Assert.IsTrue(pm2.visible, "Overlapping placemat should be visible after main placemat uncollapsing.");
            }
        }

        IEnumerator PlacematTestCollapseEdges(Vector2 node1Pos, Vector2 node2Pos, PortOrientation orientation)
        {
            var pmModel = CreatePlacemat(k_DefaultPlacematRect);

            var node1Model = AddNode(node1Pos, PortDirection.Output, orientation);
            var node2Model = AddNode(node2Pos, PortDirection.Input, orientation);

            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(node1Model.GetOutputPorts().First(), node2Model.GetInputPorts().First());
            while (actions.MoveNext())
                yield return null;

            var node1 = node1Model.GetUI<Node>(graphView);
            var node2 = node2Model.GetUI<Node>(graphView);
            var pm = pmModel.GetUI<Placemat>(graphView);

            var edgeModel = node1Model.GetOutputPorts().First().GetConnectedEdges().First();
            var edge = edgeModel.GetUI<Edge>(graphView);

            bool node1Overlaps = pm.worldBound.Overlaps(node1.worldBound);
            bool node2Overlaps = pm.worldBound.Overlaps(node2.worldBound);

            Assert.IsTrue((node1Overlaps || node2Overlaps) && !(node1Overlaps && node2Overlaps),
                "One and only one node should be over the placemat");

            var overridenPort = node1Overlaps ? node1Model.GetOutputPorts().First() : node2Model.GetInputPorts().First();

            Assert.IsTrue(node1.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible prior to placemat collapsing.");

            var isPortOverridden = pm.GetPortCenterOverride(overridenPort, out var portOverridePos);
            Assert.IsFalse(isPortOverridden, "Port of visible node should not be overridden.");

            pm.CollapsePlacemat(true);
            yield return null;

            node1 = node1Model.GetUI<Node>(graphView);
            node2 = node2Model.GetUI<Node>(graphView);
            pm = pmModel.GetUI<Placemat>(graphView);
            edge = edgeModel.GetUI<Edge>(graphView);

            if (node1Overlaps)
            {
                Assert.IsFalse(node1.visible, "Node over placemat should not be visible after placemat collapse.");
                Assert.IsTrue(node2.visible, "Node not over placemat should still be visible after placemat collapse.");
            }
            else
            {
                Assert.IsTrue(node1.visible, "Node not over placemat should still be visible after placemat collapse.");
                Assert.IsFalse(node2.visible, "Node over placemat should not be visible after placemat collapse.");
            }

            Assert.IsTrue(edge.visible, "Edge crossing collapsed / uncollapsed boundary should still be visible.");

            isPortOverridden = pm.GetPortCenterOverride(overridenPort, out portOverridePos);
            Assert.IsTrue(isPortOverridden, "Port of collapsed node should be overridden.");
            if (node1Overlaps)
            {
                var edgePos = graphView.ContentViewContainer.LocalToWorld(edge.From);
                Assert.AreEqual(portOverridePos, edgePos, "Overriden port position is not what it was expected.");
            }
            else
            {
                var edgePos = graphView.ContentViewContainer.LocalToWorld(edge.To);
                Assert.AreEqual(portOverridePos, edgePos, "Overriden port position is not what it was expected.");
            }

            pm.CollapsePlacemat(false);
            yield return null;

            node1 = node1Model.GetUI<Node>(graphView);
            node2 = node2Model.GetUI<Node>(graphView);
            pm = pmModel.GetUI<Placemat>(graphView);
            edge = edgeModel.GetUI<Edge>(graphView);

            Assert.IsTrue(node1.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible after to placemat uncollapsing.");

            isPortOverridden = pm.GetPortCenterOverride(overridenPort, out portOverridePos);
            Assert.IsFalse(isPortOverridden, "Port of visible node should not be overridden.");
        }

        IEnumerator TestStackedPlacematsMoveAndCollapse(Vector2 mouseStart, params Rect[] positions)
        {
            Placemat pm;

            var pmModels = positions.Select(p => CreatePlacemat(GraphViewStaticBridge.RoundToPixelGrid(p))).ToList();
            MarkGraphViewStateDirty();
            yield return null;

            var start = mouseStart;
            var delta = Vector2.up * 50;
            var end = start + delta;
            helpers.DragTo(start, end);
            yield return null;

            // Test Move
            for (int i = 0; i < positions.Length; i++)
            {
                pm = pmModels[i].GetUI<Placemat>(graphView);
                AssertVector2AreEqualWithinDelta(GraphViewStaticBridge.RoundToPixelGrid(positions[i].position + delta),
                    pm.layout.position, 0.0001f, $"Placemat with zOrder {i + 1} did not move properly");
            }

            // Test Collapse
            for (int i = 0; i < positions.Length; i++)
            {
                pm = pmModels[i].GetUI<Placemat>(graphView);
                Assert.True(pm.visible, $"Placemat with zOrder {i + 1} should be visible before collapse");
            }

            pm = pmModels[0].GetUI<Placemat>(graphView);
            pm.CollapsePlacemat(true);
            yield return null;

            pm = pmModels[0].GetUI<Placemat>(graphView);
            Assert.True(pm.visible, "Placemat with zOrder 1 should be visible after collapse");

            for (int i = 1; i < positions.Length; i++)
            {
                pm = pmModels[i].GetUI<Placemat>(graphView);
                Assert.False(pm.visible, $"Placemat with zOrder {i + 1} should not be visible after collapse");
            }
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToPort = false;

            graphView.AddTestStylesheet("Tests.uss");
        }

        [Test]
        public void PlacematsZOrderSetInAdditionOrder()
        {
            var pm1Model = CreatePlacemat(k_DefaultPlacematRect);
            var pm2Model = CreatePlacemat(k_DefaultPlacematRect);
            var pm3Model = CreatePlacemat(k_DefaultPlacematRect);
            var pm4Model = CreatePlacemat(k_DefaultPlacematRect);

            Assert.AreEqual(pm1Model.GetZOrder() + 1, pm2Model.GetZOrder(), "Placemat has unexpected z order at creation.");
            Assert.AreEqual(pm2Model.GetZOrder() + 1, pm3Model.GetZOrder(), "Placemat has unexpected z order at creation.");
            Assert.AreEqual(pm3Model.GetZOrder() + 1, pm4Model.GetZOrder(), "Placemat has unexpected z order at creation.");
        }

        [UnityTest]
        public IEnumerator PlacematsCanBeZCycledUpAndDown()
        {
            var pm1Model = CreatePlacemat(k_DefaultPlacematRect);
            var pm2Model = CreatePlacemat(k_DefaultPlacematRect);
            var pm3Model = CreatePlacemat(k_DefaultPlacematRect);
            var pm4Model = CreatePlacemat(k_DefaultPlacematRect);
            graphView.RebuildUI();
            yield return null;

            var pm1 = pm1Model.GetUI<Placemat>(graphView);
            var pm2 = pm2Model.GetUI<Placemat>(graphView);
            var pm3 = pm3Model.GetUI<Placemat>(graphView);
            var pm4 = pm4Model.GetUI<Placemat>(graphView);

            var orders = new[] { 0, 1, 2, 3 };

            Assert.AreEqual(orders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.PlacematModel.GetZOrder()), "Unexpected placemat z orders at creation.");
            //                              ^^^

            //pm1.CyclePlacemat(PlacematCommandsExtension.CycleDirection.Up);
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveForward, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(orders, new[] { pm2, pm1, pm3, pm4 }.Select(p => p.PlacematModel.GetZOrder()), "Unexpected placemat z orders after cycle up.");
            //                                   ^^^

            //pm1.CyclePlacemat(PlacematCommandsExtension.CycleDirection.Up);
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveForward, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(orders, new[] { pm2, pm3, pm1, pm4 }.Select(p => p.PlacematModel.GetZOrder()), "Unexpected placemat z orders after cycle up.");
            //                                        ^^^

            //pm1.CyclePlacemat(PlacematCommandsExtension.CycleDirection.Up);
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveForward, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(orders, new[] { pm2, pm3, pm4, pm1 }.Select(p => p.PlacematModel.GetZOrder()), "Unexpected placemat z orders after cycle up.");
            //                                             ^^^

            // Once at the top, it stays at the top
            //pm1.CyclePlacemat(PlacematCommandsExtension.CycleDirection.Up);
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveForward, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(orders, new[] { pm2, pm3, pm4, pm1 }.Select(p => p.PlacematModel.GetZOrder()), "Cycling up topmost placemat should be idempotent.");
            //                                             ^^^

            // Go back down
            //pm1.CyclePlacemat(PlacematCommandsExtension.CycleDirection.Down);
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveBackward, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(orders, new[] { pm2, pm3, pm1, pm4 }.Select(p => p.PlacematModel.GetZOrder()), "Unexpected placemat z orders after cycle down.");
            //                                        ^^^

            //pm1.CyclePlacemat(PlacematCommandsExtension.CycleDirection.Down);
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveBackward, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(orders, new[] { pm2, pm1, pm3, pm4 }.Select(p => p.PlacematModel.GetZOrder()), "Unexpected placemat z orders after cycle down.");
            //                                   ^^^

            //pm1.CyclePlacemat(PlacematCommandsExtension.CycleDirection.Down);
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveBackward, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(orders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.PlacematModel.GetZOrder()), "Unexpected placemat z orders after cycle down.");
            //                              ^^^

            // Once at the bottom, it stays at the bottom
            //pm1.CyclePlacemat(PlacematCommandsExtension.CycleDirection.Down);
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveBackward, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(orders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.PlacematModel.GetZOrder()), "Cycling down bottommost placemat should be idempotent.");
            //                              ^^^
        }

        [UnityTest]
        public IEnumerator PlacematsCanBeBroughtToFrontAndBack()
        {
            void CheckOrder(IReadOnlyList<Placemat> pms)
            {
                Assert.AreEqual(GraphModel.PlacematModels.Count, pms.Count);
                for (var i = 0; i < pms.Count - 1; i++)
                {
                    Assert.AreEqual(GraphModel.PlacematModels[i], pms[i].PlacematModel, $"Wrong order for {i}th placemat.");
                }
            }

            var pm1Model = CreatePlacemat(k_DefaultPlacematRect, "pm 1");
            var pm2Model = CreatePlacemat(k_DefaultPlacematRect, "pm 2");
            var pm3Model = CreatePlacemat(k_DefaultPlacematRect, "pm 3");
            var pm4Model = CreatePlacemat(k_DefaultPlacematRect, "pm 4");
            graphView.RebuildUI();
            yield return null;

            var pm1 = pm1Model.GetUI<Placemat>(graphView);
            var pm2 = pm2Model.GetUI<Placemat>(graphView);
            var pm3 = pm3Model.GetUI<Placemat>(graphView);
            var pm4 = pm4Model.GetUI<Placemat>(graphView);

            CheckOrder(new[] { pm1, pm2, pm3, pm4 });

            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveTop, new[] {pm1Model}));
            yield return null;
            CheckOrder(new[] { pm2, pm3, pm4, pm1 });

            // BringPlacematToFront called twice is idempotent.
            var currentZOrders = new[] { pm1, pm2, pm3, pm4 }.Select(p => p.PlacematModel.GetZOrder());
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveTop, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(currentZOrders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.PlacematModel.GetZOrder()), "Bringing to front topmost placemat should be idempotent.");

            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveBottom, new[] {pm1Model}));
            yield return null;
            graphView.RebuildUI();
            CheckOrder(new[] { pm1, pm2, pm3, pm4 });

            // SendPlacematToBack called twice is idempotent.
            currentZOrders = new[] { pm1, pm2, pm3, pm4 }.Select(p => p.PlacematModel.GetZOrder());
            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveBottom, new[] {pm1Model}));
            yield return null;
            Assert.AreEqual(currentZOrders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.PlacematModel.GetZOrder()), "Sending to back bottommost placemat should be idempotent.");
        }

        [UnityTest]
        public IEnumerator PlacematsCanGrowToFitNodesOnTop()
        {
            var pmModel = CreatePlacemat(GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematRect));
            var node1Model = CreateNode("", GraphViewStaticBridge.RoundToPixelGrid(pmModel.PositionAndSize.position - Vector2.one * 10));
            var node2Model = CreateNode("", GraphViewStaticBridge.RoundToPixelGrid(pmModel.PositionAndSize.position + pmModel.PositionAndSize.size - Vector2.one * 10));

            MarkGraphViewStateDirty();
            yield return null;
            var pm = pmModel.GetUI<Placemat>(graphView);

            var newRect = pm.ComputeGrowToFitElementsRect();
            if (newRect != Rect.zero)
                graphView.Dispatch(new ChangeElementLayoutCommand(pmModel, newRect));
            yield return null;

            pm = pmModel.GetUI<Placemat>(graphView);
            var node1 = node1Model.GetUI<Node>(graphView);
            var node2 = node2Model.GetUI<Node>(graphView);

            var placematBounds = new Vector2(GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds),
                GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds));

            AssertVector2AreEqualWithinDelta(node1.layout.position -
                new Vector2(GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds),
                    GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds + Placemat.boundTop)),
                pm.layout.position,
                0.0001f,
                "Incorrect placemat top left position after growing it to fit nodes over it.");
            // Have a 1px tolerance to account for math errors at various pixel per points values.
            AssertVector2AreEqualWithinDelta(node2.layout.max + placematBounds,
                pm.layout.max,
                1f / GraphViewStaticBridge.PixelPerPoint + 0.0001f,
                "Incorrect placemat bottom right position after growing it to fit nodes over it.");
        }

        [UnityTest]
        public IEnumerator PlacematsCanGrowToFitAnyNodes()
        {
            var pmModel = CreatePlacemat(GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematRect));
            var node1Model = CreateNode("", GraphViewStaticBridge.RoundToPixelGrid(pmModel.PositionAndSize.position + pmModel.PositionAndSize.size + Vector2.one * 10));
            var node2Model = CreateNode("", GraphViewStaticBridge.RoundToPixelGrid(pmModel.PositionAndSize.position + pmModel.PositionAndSize.size + Vector2.one * 60));

            MarkGraphViewStateDirty();
            yield return null;

            var pm = pmModel.GetUI<Placemat>(graphView);
            var node1 = node1Model.GetUI<Node>(graphView);
            var node2 = node2Model.GetUI<Node>(graphView);

            var newRect = pm.ComputeGrowToFitElementsRect(new List<GraphElement> { node1, node2 });
            if (newRect != Rect.zero)
                graphView.Dispatch(new ChangeElementLayoutCommand(pmModel, newRect));
            yield return null;

            pm = pmModel.GetUI<Placemat>(graphView);
            node2 = node2Model.GetUI<Node>(graphView);

            var placematBounds = new Vector2(GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds),
                GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds));

            // Since we're not snugging, the position of the placemat will remain unchanged.
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematRect.position), pm.layout.position,
                "Incorrect placemat top left position after growing it to fit nodes not over it.");
            // Have a 1px tolerance to account for math errors at various pixel per points values.

            AssertVector2AreEqualWithinDelta(node2.layout.max + placematBounds,
                pm.layout.max,
                1f / GraphViewStaticBridge.PixelPerPoint + 0.0001f,
                "Incorrect placemat bottom right position after growing it to fit nodes not over it.");
        }

        [UnityTest]
        public IEnumerator PlacematsCanShrinkToSnugNodesOnTop()
        {
            var largeRectSize = new Rect(GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematPos),
                k_DefaultPlacematSize * 5);
            var pmModel = CreatePlacemat(largeRectSize);

            var baseNodePos = k_DefaultPlacematPos + largeRectSize.size / 2;
            var node1Model = CreateNode("", GraphViewStaticBridge.RoundToPixelGrid(baseNodePos));
            var node2Model = CreateNode("", GraphViewStaticBridge.RoundToPixelGrid(baseNodePos + k_DefaultNodeSize * 2));

            MarkGraphViewStateDirty();
            yield return null;

            var pm = pmModel.GetUI<Placemat>(graphView);

            var newRect = pm.ComputeShrinkToFitElementsRect();
            if (newRect != Rect.zero)
                graphView.Dispatch(new ChangeElementLayoutCommand(pmModel, newRect));
            yield return null;

            pm = pmModel.GetUI<Placemat>(graphView);
            var node1 = node1Model.GetUI<Node>(graphView);
            var node2 = node2Model.GetUI<Node>(graphView);

            var placematBounds = new Vector2(GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds),
                GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds));

            AssertVector2AreEqualWithinDelta(node1.layout.position -
                new Vector2(GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds),
                    GraphViewStaticBridge.RoundToPixelGrid(Placemat.bounds + Placemat.boundTop)),
                pm.layout.position,
                0.0001f,
                "Incorrect placemat top left position after growing it to fit nodes over it.");
            // Have a 1px tolerance to account for math errors at various pixel per points values.
            AssertVector2AreEqualWithinDelta(node2.layout.max + placematBounds,
                pm.layout.max,
                1f / GraphViewStaticBridge.PixelPerPoint + 0.0001f,
                "Incorrect placemat bottom right position after growing it to fit nodes over it.");
        }

        static EventModifiers[] s_ModifiersNoneShift = { EventModifiers.None, EventModifiers.Shift };
        static TestType[] s_TestTypes = { TestType.Default, TestType.Collapsed };
        static ElementType[] s_ElementTypes = { ElementType.Node, ElementType.StickyNote };

        // Config 1
        // +----------------------+
        // |       Placemat       |
        // |                      |
        // |    +------+          |
        // |    | Node |          |
        // |    |      |          |
        // |    +------+          |
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveWithSingleNodeFullyOver(
            [ValueSource(nameof(s_ModifiersNoneShift))] EventModifiers modifiers,
            [ValueSource(nameof(s_TestTypes))] TestType testType,
            [ValueSource(nameof(s_ElementTypes))] ElementType elementType)
        {
            var pos = k_DefaultPlacematPos + Vector2.one * 50;
            var actions = PlacematTestMove(pos, k_NoSecondPlacemat, elementType, testType, modifiers);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        // Config 2
        // +----------------------+
        // |       Placemat       |
        // |                      |
        // |                      |
        // |                      |
        // |                      |
        // |                   +------+
        // |                   | Node |
        // +-------------------|      |
        //                     +------+
        [UnityTest]
        public IEnumerator PlacematMoveWithSingleNodePartiallyOver(
            [ValueSource(nameof(s_ModifiersNoneShift))] EventModifiers modifiers,
            [ValueSource(nameof(s_TestTypes))] TestType testTypemodifiers,
            [ValueSource(nameof(s_ElementTypes))] ElementType elementType)
        {
            var pos = k_DefaultPlacematPos + k_DefaultPlacematSize - Vector2.one * 5;
            var actions = PlacematTestMove(pos, k_NoSecondPlacemat, elementType, testTypemodifiers, modifiers);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        // Config 3
        // +----------------------+
        // |       Placemat       |
        // |                      |
        // |                      |  +------+
        // |                      |  | Node |
        // |                      |  |      |
        // |                      |  +------+
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveUnderExternalNodeWithoutEffect()
        {
            var pmModel = CreatePlacemat(GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematRect));

            Vector2 startNodePos = k_DefaultPlacematPos + new Vector2(k_DefaultPlacematSize.x + k_DefaultNodeSize.x, k_DefaultNodeSize.y / 2);
            var nodeModel = AddNode(GraphViewStaticBridge.RoundToPixelGrid(startNodePos));

            MarkGraphViewStateDirty();
            yield return null;

            const int steps = 10;
            Vector2 moveDelta = GraphViewStaticBridge.RoundToPixelGrid(new Vector2(2 * k_DefaultPlacematSize.x / steps, 0));

            // Move!
            {
                var worldPmPosition = graphView.ContentViewContainer.LocalToWorld(k_DefaultPlacematRect.position);
                var start = worldPmPosition + k_SelectionOffset;
                var end = start + moveDelta;
                helpers.MouseDownEvent(start);
                yield return null;

                for (int i = 0; i < steps; i++)
                {
                    // Make sure we get under the node
                    helpers.MouseDragEvent(start, end);
                    yield return null;

                    start = end;
                    end += moveDelta;
                }

                helpers.MouseUpEvent(end);
                yield return null;
            }

            // The placemat will have moved, but not the node.
            Vector2 expectedPlacematPos = GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematRect.position + moveDelta * steps);
            Vector2 expectedNodePos = GraphViewStaticBridge.RoundToPixelGrid(startNodePos);
            var pm = pmModel.GetUI<Placemat>(graphView);
            var node = nodeModel.GetUI<Node>(graphView);
            Assert.AreEqual(expectedPlacematPos, pm?.layout.position, "Placemat should have moved following manipulation.");
            Assert.AreEqual(expectedNodePos, node?.layout.position, "Node should not have moved when placemat was moved under it.");
            yield return null;
        }

        // Config 4
        // +----------------------+
        // |       Placemat1      |
        // |                      |
        // |   +-------------+    |
        // |   |  Placemat2  |    |
        // |   |             |    |
        // |   +-------------+    |
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSinglePlacematFullyOver(
            [ValueSource(nameof(s_ModifiersNoneShift))] EventModifiers modifiers)
        {
            var pos = k_DefaultPlacematPos + Vector2.one * 50;
            var actions = PlacematTestMove(k_NoNode, pos, ElementType.Node, TestType.Default, modifiers);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        // Config 5
        // +----------------------+
        // |       Placemat1      |
        // |                      |
        // |   +-------------+    |
        // |   |  Placemat2  |    |
        // |   |             |    |
        // |   |  +------+   |    |
        // |   |  | Node |   |    |
        // |   |  +------+   |    |
        // |   |             |    |
        // |   +-------------+    |
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSingleNodeFullyOverPlacematFullyOver(
            [ValueSource(nameof(s_ModifiersNoneShift))] EventModifiers modifiers,
            [ValueSource(nameof(s_ElementTypes))] ElementType elementType)
        {
            var pm2Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var nodePos = pm2Pos + Vector2.one * 50;
            var actions = PlacematTestMove(nodePos, pm2Pos, elementType, TestType.Default, modifiers);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        // Config 6
        // +----------------------+
        // |       Placemat1      |
        // |                      |
        // |   +-------------+    |
        // |   |  Placemat2  |    |
        // |   |             |    |
        // |   |        +------+  |
        // |   |        | Node |  |
        // |   |        +------+  |
        // |   |             |    |
        // |   +-------------+    |
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSingleNodePartiallyOverPlacematFullyOver(
            [ValueSource(nameof(s_ModifiersNoneShift))] EventModifiers modifiers,
            [ValueSource(nameof(s_ElementTypes))] ElementType elementType)
        {
            var pm2Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var nodePos = pm2Pos + new Vector2(k_SecondPlacematSize.x - k_DefaultNodeSize.x / 2, 50);
            var actions = PlacematTestMove(nodePos, pm2Pos, elementType, TestType.Default, modifiers);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        // Config 7
        // +------------------+
        // |    Placemat1     |
        // |                  |
        // |                +-------------+
        // |                |  Placemat2  |
        // |                |             |
        // |                |    +------+ |
        // |                |    | Node | |
        // |                |    +------+ |
        // |                |             |
        // |                +-------------+
        // |                  |
        // +------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSingleNodeFullyOverPlacematPartiallyOver(
            [ValueSource(nameof(s_ModifiersNoneShift))] EventModifiers modifiers,
            [ValueSource(nameof(s_ElementTypes))] ElementType elementType)
        {
            var pm2Pos = k_DefaultPlacematPos + new Vector2(k_DefaultPlacematSize.x - 25, 50);
            var nodePos = pm2Pos + Vector2.one * 50;
            var actions = PlacematTestMove(nodePos, pm2Pos, elementType, TestType.Default, modifiers);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        // Config 8
        // +------------------+
        // |    Placemat1     |
        // |                  |
        // |                +-------------+
        // |                |  Placemat2  |
        // |                |             |
        // |                |    +------+ |
        // |                +----| Node |-+
        // |                  |  +------+
        // |                  |
        // +------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSingleNodePartiallyOverPlacematPartiallyOver(
            [ValueSource(nameof(s_ModifiersNoneShift))] EventModifiers modifiers,
            [ValueSource(nameof(s_ElementTypes))] ElementType elementType)
        {
            var pm2Pos = k_DefaultPlacematPos + new Vector2(k_DefaultPlacematSize.x - 25, 50);
            var nodePos = pm2Pos + new Vector2(50, k_SecondPlacematSize.y - 25);
            var actions = PlacematTestMove(nodePos, pm2Pos, elementType, TestType.Default, modifiers);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        // Config 9
        // +------------------+
        // |    Placemat1     |
        // |                  |
        // |                  |-----------+
        // |                  | Placemat2 |
        // |                  |           |
        // |                  |  +------+ |
        // |                  |  | Node | |
        // |                  |  +------+ |
        // |                  |           |
        // |                  |-----------+
        // |                  |
        // +------------------+
        [UnityTest]
        public IEnumerator PlacematDoesNotMoveSingleNodeFullyOverPlacematUnder()
        {
            var pm2Pos = GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematPos + new Vector2(k_DefaultPlacematSize.x - 25, 50));
            var nodePos = GraphViewStaticBridge.RoundToPixelGrid(pm2Pos + Vector2.one * 50);

            var pmModel = CreatePlacemat(GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematRect));
            var pm2Model = CreatePlacemat(new Rect(pm2Pos, k_SecondPlacematSize));
            pm2Model.Color = Color.red;
            var nodeModel = AddNode(nodePos);

            MarkGraphViewStateDirty();
            yield return null;

            var pm = pmModel.GetUI<Placemat>(graphView);
            var pm2 = pm2Model.GetUI<Placemat>(graphView);
            var node = nodeModel.GetUI<Node>(graphView);

            graphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveBottom, new[] {pm2Model}));
            yield return null;

            Vector2 moveDelta = new Vector2(20, 20);

            // Move!
            {
                var worldPmPosition = graphView.ContentViewContainer.LocalToWorld(pm.layout.position);
                var start = worldPmPosition + k_SelectionOffset;
                var end = start + moveDelta;
                helpers.DragTo(start, end);
                yield return null;
            }

            Vector2 expectedPlacematPos = GraphViewStaticBridge.RoundToPixelGrid(k_DefaultPlacematRect.position + moveDelta);

            // Node and second placemat should not have moved since they are below main placemat
            Vector2 expectedNodePos = nodePos;
            Vector2 expectedSecondPlacematPos = pm2Pos;

            Assert.AreEqual(expectedPlacematPos, pm.layout.position, "Placemat should have moved following manipulation.");
            Assert.AreEqual(expectedNodePos, node.layout.position, "Node should not have moved.");
            Assert.AreEqual(expectedSecondPlacematPos, pm2.layout.position, "Placemat should have moved because it was under the placemat being manipulated.");
            yield return null;

            pmModel.Collapsed = true;

            // Items under a collapsed placemat will not be hidden.
            Assert.IsTrue(node.visible, "Node should be visible since not over collapsed placemat.");
            Assert.IsTrue(pm2.visible, "Placemat should be visible since it was under collapsed placemat.");

            yield return null;
        }

        // Config 10 (edges)
        // +-------------+
        // | v Placemat  |
        // |             |
        // |  +-------+  |  +-------+       +-------------+   +-------+
        // |  | Node1 o-----o Node2 |  >>>  | > Placemat -----o Node2 |
        // |  +-------+  |  +-------+       +-------------+   +-------+
        // |             |
        // +-------------+
        [UnityTest]
        public IEnumerator SingleConnectedToTheEastOverCollapsedPlacematHidesNodeAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var node2Pos = k_DefaultPlacematPos + k_DefaultPlacematSize / 2 + Vector2.right * k_DefaultPlacematSize.x;

            var actions = PlacematTestCollapseEdges(node1Pos, node2Pos, PortOrientation.Horizontal);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 11 (edges)
        //             +-------------+
        //             | v Placemat  |
        //             |             |
        //  +-------+  |   +-------+ |       +-------+    +-------------+
        //  | Node1 o----- o Node2 | |  >>>  | Node1 o------ > Placemat |
        //  +-------+  |   +-------+ |       +-------+    +-------------+
        //             |             |
        //             +-------------+
        [UnityTest]
        public IEnumerator SingleConnectedToTheWestOverCollapsedPlacematHidesNodeAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos - Vector2.right * 250;
            var node2Pos = k_DefaultPlacematPos + Vector2.one * 50;

            var actions = PlacematTestCollapseEdges(node1Pos, node2Pos, PortOrientation.Horizontal);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 12 (edges)
        // +-------------+
        // | v Placemat  |
        // |             |
        // |  +-------+  |        +-------------+
        // |  | Node1 |  |  >>>   | > Placemat  |
        // |  +---o---+  |        +------|------+
        // |      |      |               |
        // +------|------+               |
        //        |                      |
        //    +---o---+              +---o---+
        //    | Node2 |              | Node2 |
        //    +-------+              +-------+
        [UnityTest]
        public IEnumerator SingleConnectedToTheSouthOverCollapsedPlacematHidesNodeAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var node2Pos = k_DefaultPlacematPos + k_DefaultPlacematSize / 2 + Vector2.up * 150;

            var actions = PlacematTestCollapseEdges(node1Pos, node2Pos, PortOrientation.Vertical);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 13 (edges)
        //    +-------+              +-------+
        //    | Node1 |              | Node1 |
        //    +---o---+              +---o---+
        //        |                      |
        // +------|------+               |
        // | v Pla|emat  |               |
        // |      |      |               |
        // |  +---o---+  |        +------|------+
        // |  | Node2 |  |  >>>   | > Placemat  |
        // |  +-------+  |        +-------------+
        // |             |
        // +-------------+
        [UnityTest]
        public IEnumerator SingleConnectedToTheNorthOverCollapsedPlacematHidesNodeAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos + k_DefaultPlacematSize / 2 - Vector2.up * 400;
            var node2Pos = k_DefaultPlacematPos + Vector2.one * 50;

            var actions = PlacematTestCollapseEdges(node1Pos, node2Pos, PortOrientation.Vertical);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 14 (edges)
        // +------------------------+
        // |        Placemat        |
        // |                        |       +-------------+
        // | +-------+    +-------+ |  >>>  | > Placemat  |
        // | | Node1 o----o Node2 | |       +-------------+
        // | +-------+    +-------+ |
        // |                        |
        // +------------------------+
        [UnityTest]
        public IEnumerator TwoConnectedNodesOverCollapsedPlacematHideBothNodesAndEdge()
        {
            var pmModel = CreatePlacemat(k_DefaultPlacematRect);
            var node1Pos = k_DefaultPlacematRect.position + Vector2.one * 50;
            var node2Pos = k_DefaultPlacematRect.position + k_DefaultPlacematRect.size - Vector2.one * 100;

            var node1Model = AddNode(node1Pos, PortDirection.Output);
            var node2Model = AddNode(node2Pos, PortDirection.Input);

            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(node1Model.GetOutputPorts().First(), node2Model.GetInputPorts().First());
            while (actions.MoveNext())
            {
                yield return null;
            }

            var node1 = node1Model.GetUI<Node>(graphView);
            var node2 = node2Model.GetUI<Node>(graphView);
            var edge = node1Model.GetOutputPorts().First().GetConnectedEdges().First().GetUI<Edge>(graphView);

            Assert.IsTrue(node1.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible prior to placemat collapsing.");

            var pm = pmModel.GetUI<Placemat>(graphView);

            pm.CollapsePlacemat(true);
            yield return null;

            node1 = node1Model.GetUI<Node>(graphView);
            node2 = node2Model.GetUI<Node>(graphView);
            edge = node1Model.GetOutputPorts().First().GetConnectedEdges().First().GetUI<Edge>(graphView);

            Assert.IsFalse(node1.visible, "Node over placemat should not be visible after placemat collapse.");
            Assert.IsFalse(node2.visible, "Node over placemat should not be visible after placemat collapse.");
            Assert.IsFalse(edge.visible, "Edge connecting two nodes over placemat should not be visible after placemat collapse.");

            pm = pmModel.GetUI<Placemat>(graphView);
            pm.CollapsePlacemat(false);
            yield return null;

            node1 = node1Model.GetUI<Node>(graphView);
            node2 = node2Model.GetUI<Node>(graphView);
            edge = node1Model.GetOutputPorts().First().GetConnectedEdges().First().GetUI<Edge>(graphView);

            Assert.IsTrue(node1.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible after to placemat uncollapsing.");

            yield return null;
        }

        // If Placemat1 is collapsed, Node is hidden. Moving Placemat2 should not move the node.
        //
        //        +---------------+   +---------------+
        //        |   Placemat1   |   |   Placemat2   |
        //        |            +----------+           |
        //        |            |   Node   |           |
        //        |            |          |           |
        //        |            +----------+           |
        //        |               |   |               |
        //        +---------------+   +---------------+
        [UnityTest]
        public IEnumerator PlacematDoesNotMoveElementHiddenByOtherPlacemat()
        {
            var pmModel = CreatePlacemat(k_DefaultPlacematRect);
            pmModel.Color = Color.red;

            float xOffset = k_DefaultPlacematSize.x - k_DefaultNodeSize.x / 2;
            Vector2 nodePos = k_DefaultPlacematPos + Vector2.right * xOffset;
            var nodeModel = AddNode(nodePos);

            Vector2 pm2Pos = k_DefaultPlacematPos + Vector2.right * (k_DefaultPlacematSize.x + 10f);
            var pm2Model = CreatePlacemat(new Rect(pm2Pos, k_DefaultPlacematSize));
            pm2Model.Color = Color.green;

            MarkGraphViewStateDirty();
            yield return null;

            var pm = pmModel.GetUI<Placemat>(graphView);
            pm.CollapsePlacemat(true);
            yield return null;

            var node = nodeModel.GetUI<Node>(graphView);
            Assert.AreEqual(nodePos, node.layout.position);

            // Move pm2
            {
                var pm2 = pm2Model.GetUI<Placemat>(graphView);
                var worldPm2Position = graphView.ContentViewContainer.LocalToWorld(pm2.layout.position);
                var start = worldPm2Position + k_SelectionOffset;
                var end = start + (Vector2.right * 100);
                helpers.DragTo(start, end);
                yield return null;
            }

            node = nodeModel.GetUI<Node>(graphView);
            Assert.AreEqual(nodePos, node.layout.position);
        }

        //               +------------------+
        //               |    Placemat 1    |
        //               |                  |
        //   +------------------+   +------------------+
        //   |    Placemat 2    |   |    Placemat 3    |
        //   |                  |---|                  |
        //   |           +------------------+          |
        //   |           |    Placemat 4    |          |
        //   +-----------|                  |----------+
        //               |                  |
        //               +------------------+
        [UnityTest]
        public IEnumerator PlacematDiamondMovesProperly()
        {
            var pos = new[]
            {
                new Rect(300, 100, 200, 100),
                new Rect(175, 175, 200, 100),
                new Rect(425, 175, 200, 100),
                new Rect(300, 250, 200, 100)
            };
            var actions = TestStackedPlacematsMoveAndCollapse(pos[0].center, pos);

            while (actions.MoveNext())
                yield return null;
        }

        //                                   +------------------+
        //                                   |    Placemat 1    |
        //                                   |                  |
        //                       +------------------+   +------------------+
        //                       |    Placemat 2    |   |    Placemat 3    |
        //                       |                  |---|                  |
        //             +------------------+  +------------------+  +------------------+
        //             |    Placemat 4    |  |    Placemat 5    |  |    Placemat 6    |
        //             |                  |--|                  |--|                  |
        //   +------------------+  +------------------+  +------------------+  +------------------+
        //   |    Placemat 7    |  |    Placemat 8    |  |    Placemat 9    |  |    Placemat 10   |
        //   |                  |--|                  |  |                  |--|                  |
        //   |                  |  |                  |  |                  |  |                  |
        //   +------------------+  +------------------+  +------------------+  +------------------+
        [UnityTest]
        public IEnumerator PlacematPyramidMovesProperly()
        {
            var pos = new[]
            {
                new Rect(425, 50, 200, 100),

                new Rect(300, 125, 200, 100),
                new Rect(550, 125, 200, 100),

                new Rect(175, 200, 200, 100),
                new Rect(425, 200, 200, 100),
                new Rect(675, 200, 200, 100),

                new Rect(50, 275, 200, 100),
                new Rect(300, 275, 200, 100),
                new Rect(550, 275, 200, 100),
                new Rect(800, 275, 200, 100)
            };
            var actions = TestStackedPlacematsMoveAndCollapse(pos[0].center, pos);

            while (actions.MoveNext())
                yield return null;
        }

        //   +--------------------------------------------------------------------+
        //   |                             Placemat 1                             |
        //   |                                                                    |
        //   |  +------------------+  +------------------+  +------------------+  |
        //   +--|    Placemat 2    |--|    Placemat 3    |--|    Placemat 4    |--+
        //      |                  |  |                  |  |                  |
        //   +--------------------------------------------------------------------+
        //   |                             Placemat 5                             |
        //   |                                                                    |
        //   +--------------------------------------------------------------------+
        [UnityTest]
        public IEnumerator PlacematSandwichMovesProperly()
        {
            var pos = new[]
            {
                new Rect(50,  50, 825, 100),

                new Rect(75, 125, 200, 100),
                new Rect(325, 125, 200, 100),
                new Rect(575, 125, 200, 100),

                new Rect(50, 200, 825, 100)
            };
            var actions = TestStackedPlacematsMoveAndCollapse(pos[0].center, pos);

            while (actions.MoveNext())
                yield return null;
            yield return null;
        }

        // +-----------------------------+       +-----------------------------+
        // |           Placemat          |       |           Placemat          |
        // |                             |       |                             |
        // |  +-------------+            |       |  +-------------+            |
        // |  | v Placemat  |            |       |  | > Placemat  |            |
        // |  |             |            |  ==>  |  +-------------+            |  ==>  +-----------------------------+  ==>  +-----------------------------+
        // |  |   +------+  |            |       |                             |       |           Placemat          |       |           Placemat          |
        // |  |   | Node |  |            |       |                             |       |                             |       |                             |
        // |  |   +------+  |            |       |                             |       |  +-------------+            |       |  +-------------+            |
        // |  +-------------+            |       |                             |       |  | > Placemat  |            |       |  | v Placemat  |            |
        // +-----------------------------+       +-----------------------------+       |  +-------------+            |       |  |             |            |
        //                                                                             |                             |       |  |   +------+  |            |
        //                                                                             |                             |       |  |   | Node |  |            |
        //                                                                             |                             |       |  |   +------+  |            |
        //                                                                             |                             |       |  +-------------+            |
        //                                                                             +-----------------------------+       +-----------------------------+
        [UnityTest]
        public IEnumerator CollapsedPlacematMovedByPlacematMovesNodeCorrectly()
        {
            var pm2Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var nodePos = pm2Pos + Vector2.one * 50;

            CreatePlacemat(k_DefaultPlacematRect);
            var pm2Model = CreatePlacemat(new Rect(pm2Pos, k_SecondPlacematSize));
            pm2Model.Color = Color.red;

            var nodeModel = AddNode(nodePos);
            yield return null;

            MarkGraphViewStateDirty();
            yield return null;

            var pm2 = pm2Model.GetUI<Placemat>(graphView);
            var node = nodeModel.GetUI<Node>(graphView);

            Assert.True(node.visible, "Node should be visible prior to collapse");
            pm2.CollapsePlacemat(true);
            yield return null;

            node = nodeModel.GetUI<Node>(graphView);
            Assert.False(node.visible, "Node should not be visible after collapse");
            yield return null;

            var worldPmPosition = graphView.ContentViewContainer.LocalToWorld(k_DefaultPlacematRect.position);
            var start = worldPmPosition + k_SelectionOffset;
            var delta = Vector2.one * 10;
            var end = start + delta;
            helpers.DragTo(start, end);
            yield return null;

            pm2.CollapsePlacemat(false);
            yield return null;

            node = nodeModel.GetUI<Node>(graphView);
            Assert.True(node.visible, "Node should be visible after uncollapse");
            Assert.AreEqual(nodePos + delta, node.layout.position, "Node should have moved with the placemat hiding it");
        }

        // +-------------------+
        // |      Placemat     |
        // |                   |
        // |  +-------------+  |    +-------+                                +-------+
        // |  | > Placemat ---------o  Node |  ==>  +-------------------+  --o  Node |
        // |  +-------------+  |    +-------+       |      Placemat     | /  +-------+
        // |                   |                    |                   |/
        // +-------------------+                    |  +-------------+  /
        //                                          |  | > Placemat ---/|
        //                                          |  +-------------+  |
        //                                          |                   |
        //                                          +-------------------+
        [UnityTest]
        public IEnumerator CollapsedPlacematWithEdgesMovedByPlacematMovesEdgesCorrectly()
        {
            var pm2Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var node1Pos = pm2Pos + Vector2.one * 50;
            var node2Pos = node1Pos + Vector2.right * 250;

            CreatePlacemat(k_DefaultPlacematRect);
            var pm2Model = CreatePlacemat(new Rect(pm2Pos, k_SecondPlacematSize));
            pm2Model.Color = Color.red;

            var node1Model = AddNode(node1Pos, PortDirection.Output);
            var node2Model = AddNode(node2Pos, PortDirection.Input);

            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(node1Model.GetOutputPorts().First(), node2Model.GetInputPorts().First());
            while (actions.MoveNext())
            {
                yield return null;
            }

            var node1 = node1Model.GetUI<Node>(graphView);
            var node2 = node2Model.GetUI<Node>(graphView);
            var edge = node1Model.GetOutputPorts().First().GetConnectedEdges().First().GetUI<Edge>(graphView);
            var pm2 = pm2Model.GetUI<Placemat>(graphView);

            Assert.True(node1.visible, "Node should be visible prior to collapse");
            Assert.True(node2.visible, "Node should be visible prior to collapse");
            Assert.True(edge.visible, "Edge should be visible prior to collapse");
            pm2.CollapsePlacemat(true);
            yield return null;

            node1 = node1Model.GetUI<Node>(graphView);
            node2 = node2Model.GetUI<Node>(graphView);
            edge = node1Model.GetOutputPorts().First().GetConnectedEdges().First().GetUI<Edge>(graphView);
            pm2 = pm2Model.GetUI<Placemat>(graphView);

            Assert.False(node1.visible, "Node should not be visible after collapse");
            Assert.True(node2.visible, "Node should still be visible after collapse");
            Assert.True(edge.visible, "Edge should still be visible after collapse");

            var isPortOverridden = pm2.GetPortCenterOverride(node1Model.GetOutputPorts().First(), out var portOverridePos);
            Assert.IsTrue(isPortOverridden, "Port of hidden node should be overridden.");
            var edgeFromPos = graphView.ContentViewContainer.LocalToWorld(edge.From);
            Assert.AreEqual(portOverridePos, edgeFromPos, "Overriden port position is not what it was expected.");

            var worldPmPosition = graphView.ContentViewContainer.LocalToWorld(k_DefaultPlacematRect.position);
            var start = worldPmPosition + k_SelectionOffset;
            var delta = Vector2.one * 10;
            var end = start + delta;
            helpers.DragTo(start, end);
            yield return null;

            pm2 = pm2Model.GetUI<Placemat>(graphView);
            edge = node1Model.GetOutputPorts().First().GetConnectedEdges().First().GetUI<Edge>(graphView);

            isPortOverridden = pm2.GetPortCenterOverride(node1Model.GetOutputPorts().First(), out portOverridePos);
            Assert.IsTrue(isPortOverridden, "Port of hidden node should still be overridden.");
            edgeFromPos = graphView.ContentViewContainer.LocalToWorld(edge.From);
            Assert.AreEqual(portOverridePos, edgeFromPos, "Overriden port position is not what it was expected after move.");
        }

        [UnityTest]
        public IEnumerator PlacematSetPositionDoesNotChangeSizeWhenCollapsed()
        {
            var pmModel = CreatePlacemat(k_DefaultPlacematRect);
            pmModel.PositionAndSize = k_DefaultPlacematRect;

            MarkGraphViewStateDirty();
            yield return null;

            var pm = pmModel.GetUI<Placemat>(graphView);

            var newRect = new Rect(-42, 4242, 242, 242);
            pm.SetPosition(newRect);
            yield return null;

            Assert.AreEqual(newRect, pm.layout);

            using (var graphUpdater = graphView.GraphViewState.UpdateScope)
            {
                pmModel.PositionAndSize = newRect;
                pmModel.Collapsed = true;
                graphUpdater.MarkChanged(pmModel);
            }
            yield return null;

            pm = pmModel.GetUI<Placemat>(graphView);
            var currentSize = pm.layout.size;
            pm.SetPosition(k_DefaultPlacematRect);
            yield return null;

            Assert.AreEqual(currentSize, pm.layout.size);
        }

        [UnityTest]
        public IEnumerator SettingCollapsedElementsWorks()
        {
            var pmModel = CreatePlacemat(k_DefaultPlacematRect);

            // ReSharper disable once Unity.InefficientMultiplicationOrder
            Vector2 nodePos = k_DefaultPlacematPos + Vector2.down * 2 * Placemat.defaultCollapsedSize;
            var nodeModel = AddNode(nodePos);

            MarkGraphViewStateDirty();
            yield return null;

            var node = nodeModel.GetUI<Node>(graphView);
            Assert.IsFalse(node.style.visibility == Visibility.Hidden);

            using (var graphUpdater = graphView.GraphViewState.UpdateScope)
            {
                pmModel.Collapsed = true;
                pmModel.HiddenElements = new[] { nodeModel };
                graphUpdater.MarkChanged(pmModel);
            }
            yield return null;

            node = nodeModel.GetUI<Node>(graphView);

            Assert.IsTrue(node.style.visibility == Visibility.Hidden);
        }
    }
}
