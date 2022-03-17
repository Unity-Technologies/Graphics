using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class SnapToPortTests : GraphViewSnappingTester
    {
        static readonly Vector2 k_NodeSize = new Vector2(200, 200);
        static readonly Vector2 k_ReferenceNodePos = new Vector2(SelectionDragger.panAreaWidth, SelectionDragger.panAreaWidth);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_SnappedNode = null;
            m_ReferenceNode1 = null;
            m_InputPort = null;
            m_OutputPort = null;

            GraphViewSettings.UserSettings.EnableSnapToPort = true;
            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToSpacing = false;
            GraphViewSettings.UserSettings.EnableSnapToGrid = false;
        }

        [UnityTest]
        public IEnumerator HorizontalPortWithinSnappingDistanceShouldSnap()
        {
            // Config (both ports are connected horizontally)
            //   +-------+   +-------+
            //   | Node1 o---o Node2 |
            //   +-------+   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 400, k_ReferenceNodePos.y), k_ReferenceNodePos, Vector2.zero, false, true);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(-100, k_SnapDistance);
            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Get the UI ports
            var outputPortUI = m_OutputPort.GetView<Port>(GraphView);
            var inputPortUI = m_InputPort.GetView<Port>(GraphView);
            Assert.IsNotNull(outputPortUI);
            Assert.IsNotNull(inputPortUI);

            // The node should snap to the reference node's position in Y, but the X should be dragged normally
            Assert.AreEqual(outputPortUI.GetGlobalCenter().y, inputPortUI.GetGlobalCenter().y);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator HorizontalPortNotWithinSnappingDistanceShouldNotSnap()
        {
            // Config (both ports are connected horizontally)
            //   +-------+   +-------+
            //   | Node1 o---o Node2 |
            //   +-------+   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 400, k_ReferenceNodePos.y), k_ReferenceNodePos, Vector2.zero, false, true);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(-100, k_SnapDistance + 1);
            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Get the UI ports
            var outputPortUI = m_OutputPort.GetView<Port>(GraphView);
            var inputPortUI = m_InputPort.GetView<Port>(GraphView);
            Assert.IsNotNull(outputPortUI);
            Assert.IsNotNull(inputPortUI);

            // The port should not snap to the reference node's port in Y: the Y and X should be dragged normally
            Assert.AreNotEqual(outputPortUI.GetGlobalCenter().y, inputPortUI.GetGlobalCenter().y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator VerticalPortWithinSnappingDistanceShouldSnap()
        {
            // Config (both ports are connected vertically)
            //   +-------+
            //   | Node1 o
            //   +-------+
            //   +-------+
            //   o Node2 |
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y + k_NodeSize.y), k_ReferenceNodePos, Vector2.zero, true, true);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var outputPortUI = m_OutputPort.GetView<Port>(GraphView);
            var inputPortUI = m_InputPort.GetView<Port>(GraphView);
            Assert.IsNotNull(outputPortUI);
            Assert.IsNotNull(inputPortUI);
            m_OutputPort.Orientation = PortOrientation.Vertical;
            m_InputPort.Orientation = PortOrientation.Vertical;

            float outputPortInputPortDistance = Math.Abs(outputPortUI.GetGlobalCenter().x - inputPortUI.GetGlobalCenter().x);
            float offSetX = k_SnapDistance - outputPortInputPortDistance;

            Vector2 moveOffset = new Vector2(-offSetX, 10);
            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The node should snap to the reference node's position in X: the Y should be dragged normally
            Assert.AreEqual(outputPortUI.GetGlobalCenter().x, inputPortUI.GetGlobalCenter().x);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator VerticalPortNotWithinSnappingDistanceShouldNotSnap()
        {
            // Config (both ports are connected vertically)
            //   +-------+
            //   | Node1 o
            //   +-------+
            //   +-------+
            //   o Node2 |
            //   +-------+
            //

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y + k_NodeSize.y), k_ReferenceNodePos, Vector2.zero, true, true);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var outputPortUI = m_OutputPort.GetView<Port>(GraphView);
            var inputPortUI = m_InputPort.GetView<Port>(GraphView);
            Assert.IsNotNull(outputPortUI);
            Assert.IsNotNull(inputPortUI);
            m_OutputPort.Orientation = PortOrientation.Vertical;
            m_InputPort.Orientation = PortOrientation.Vertical;

            float outputPortInputPortDistance = Math.Abs(outputPortUI.GetGlobalCenter().x - inputPortUI.GetGlobalCenter().x);
            float offSetX = outputPortInputPortDistance - (k_SnapDistance + 1);

            Vector2 moveOffset = new Vector2(offSetX, 10);
            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The node should not snap to the reference node's position in X: the X and Y should be dragged normally
            Assert.AreNotEqual(outputPortUI.GetGlobalCenter().x, inputPortUI.GetGlobalCenter().x);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NodeShouldSnapToNearestConnectedPort()
        {
            // Config (ports are connected horizontally)
            //   +-------+
            //   | Node1 o +-------+
            //   +-------+ o Node2 o +-------+
            //             +-------+ o Node3 |
            //                       +-------+

            referenceNode1Model = CreateNode("Node1", GraphViewStaticBridge.RoundToPixelGrid(k_ReferenceNodePos), 0, 1);

            m_SnappingNodePos = GraphViewStaticBridge.RoundToPixelGrid(new Vector2(k_ReferenceNodePos.x + k_NodeSize.x, k_ReferenceNodePos.y + k_NodeSize.y * 0.5f));
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos, 1, 1);

            // Third node
            Vector2 secondReferenceNodePos = GraphViewStaticBridge.RoundToPixelGrid(new Vector2(m_SnappingNodePos.x + k_NodeSize.x, m_SnappingNodePos.y + k_NodeSize.y * 0.5f));
            IInputOutputPortsNodeModel secondReferenceNodeModel = CreateNode("Node3", secondReferenceNodePos, 1);

            var node1OutputPort = referenceNode1Model.OutputsByDisplayOrder[0];
            var node2InputPort = snappingNodeModel.InputsByDisplayOrder[0];
            var node2OutputPort = snappingNodeModel.OutputsByDisplayOrder[0];
            var node3InputPort = secondReferenceNodeModel.InputsByDisplayOrder[0];
            Assert.IsNotNull(node1OutputPort);
            Assert.IsNotNull(node2InputPort);
            Assert.IsNotNull(node2OutputPort);
            Assert.IsNotNull(node3InputPort);

            MarkGraphViewStateDirty();
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(node1OutputPort, node2InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }
            actions = ConnectPorts(node2OutputPort, node3InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // We move the snapping Node2 toward reference Node1 within the snapping range
            float offSetY = k_SnapDistance - k_NodeSize.y * 0.5f;
            Vector2 moveOffset = GraphViewStaticBridge.RoundToPixelGrid(new Vector2(0, offSetY));
            Vector2 end = start + moveOffset;

            // Move the snapping node.
            Helpers.MouseDownEvent(start);
            yield return null;

            Helpers.MouseDragEvent(start, end);
            yield return null;

            Helpers.MouseUpEvent(end);
            yield return null;

            // Get the UI ports
            var node1OutputPortUI = node1OutputPort.GetView<Port>(GraphView);
            var node2InputPortUI = node2InputPort.GetView<Port>(GraphView);
            var node2OutputPortUI = node2OutputPort.GetView<Port>(GraphView);
            var node3InputPortUI = node3InputPort.GetView<Port>(GraphView);
            Assert.IsNotNull(node1OutputPortUI);
            Assert.IsNotNull(node2InputPortUI);
            Assert.IsNotNull(node2OutputPortUI);
            Assert.IsNotNull(node3InputPortUI);

            // The snapping Node2 should snap to Node1's port
            Assert.AreEqual(node1OutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);
            // The snapping Node2 should not snap to Node3's port
            Assert.AreNotEqual(node3InputPortUI.GetGlobalCenter().y, node2OutputPortUI.GetGlobalCenter().y);

            worldNodePos = GraphView.ContentViewContainer.LocalToWorld(snappingNodeModel.Position);
            start = worldNodePos + m_SelectionOffset;

            // We move the snapping Node2 toward Node3 within the snapping range
            offSetY = k_NodeSize.y + k_SnapDistance - 1;
            moveOffset = GraphViewStaticBridge.RoundToPixelGrid(new Vector2(0, offSetY));

            // Move the snapping node.
            Helpers.MouseDownEvent(start);
            yield return null;

            end = start + moveOffset;
            Helpers.MouseDragEvent(start, end);
            yield return null;

            Helpers.MouseUpEvent(end);
            yield return null;

            // The snapping Node2's port should snap to Node3's port
            Assert.AreEqual(node3InputPortUI.GetGlobalCenter().y, node2OutputPortUI.GetGlobalCenter().y);
            // The snapping Node2's port should not snap to Node1's port
            Assert.AreNotEqual(node1OutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NodeUnderMouseShouldSnapWhenMultipleSelectedNodes()
        {
            // Config (ports are connected horizontally)
            //   +-------+
            //   | Node1 o +-------+
            //   +-------+ o Node2 o +-------+
            //             +-------+ o Node3 |
            //                       +-------+

            referenceNode1Model = CreateNode("Node1", k_ReferenceNodePos, 0, 1);

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + k_NodeSize.x, k_ReferenceNodePos.y + k_NodeSize.y * 0.5f);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos, 1, 1);

            // Third node
            Vector2 secondSelectedNodePos = new Vector2(m_SnappingNodePos.x + k_NodeSize.x, m_SnappingNodePos.y + k_NodeSize.y * 0.5f);
            IInputOutputPortsNodeModel secondSelectedNodeModel = CreateNode("Node3", secondSelectedNodePos, 1);

            var node1OutputPort = referenceNode1Model.OutputsByDisplayOrder[0];
            var node2InputPort = snappingNodeModel.InputsByDisplayOrder[0];
            var node2OutputPort = snappingNodeModel.OutputsByDisplayOrder[0];
            var node3InputPort = secondSelectedNodeModel.InputsByDisplayOrder[0];
            Assert.IsNotNull(node1OutputPort);
            Assert.IsNotNull(node2InputPort);
            Assert.IsNotNull(node2OutputPort);
            Assert.IsNotNull(node3InputPort);

            MarkGraphViewStateDirty();
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(node1OutputPort, node2InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }
            actions = ConnectPorts(node2OutputPort, node3InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 worldPosNode2 = GraphView.ContentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 worldPosNode3 = GraphView.ContentViewContainer.LocalToWorld(secondSelectedNodePos);

            Vector2 selectionPosNode2 = worldPosNode2 + m_SelectionOffset;
            Vector2 selectionPosNode3 = worldPosNode3 + m_SelectionOffset;

            // Select Node3 by clicking on it and pressing Ctrl
            Helpers.MouseDownEvent(selectionPosNode3, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            Helpers.MouseUpEvent(selectionPosNode3, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            // Move mouse to Node2
            Helpers.MouseMoveEvent(selectionPosNode3, selectionPosNode2, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            // Select Node2 by clicking on it and pressing Ctrl
            Helpers.MouseDownEvent(selectionPosNode2, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            // Move Node2 toward reference Node1 within the snapping range
            float topToTopDistance = k_NodeSize.y * 0.5f;
            float offSetY = k_SnapDistance - topToTopDistance;
            Vector2 moveOffset = new Vector2(0, offSetY);
            Vector2 end = selectionPosNode2 + moveOffset;
            Helpers.MouseDragEvent(selectionPosNode2, end, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            Helpers.MouseUpEvent(end, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            // Get the UI ports
            var node1OutputPortUI = node1OutputPort.GetView<Port>(GraphView);
            var node2InputPortUI = node2InputPort.GetView<Port>(GraphView);
            var node2OutputPortUI = node2OutputPort.GetView<Port>(GraphView);
            var node3InputPortUI = node3InputPort.GetView<Port>(GraphView);
            var node3 = secondSelectedNodeModel.GetView<Node>(GraphView);
            Assert.IsNotNull(node1OutputPortUI);
            Assert.IsNotNull(node2InputPortUI);
            Assert.IsNotNull(node2OutputPortUI);
            Assert.IsNotNull(node3InputPortUI);
            Assert.IsNotNull(node3);

            // The snapping Node2 should snap to Node1's port
            Assert.AreEqual(node1OutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);
            // Node3 should not snap to Node1's port
            Assert.AreNotEqual(node1OutputPortUI.GetGlobalCenter().y, node3InputPortUI.GetGlobalCenter().y);
            // Node 3 should have moved by the move offset in x and the same y offset as Node2
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(secondSelectedNodePos.x + moveOffset.x), node3.layout.x);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(secondSelectedNodePos.y - topToTopDistance), node3.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShouldSnapToClosestPortWhenMultipleConnectedPorts()
        {
            // Config (both node1 ports are connected horizontally to node2's port)
            //   +-------+   +-------+
            //   | Node1 o   o Node2 |
            //   |       o   +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + k_NodeSize.x, k_ReferenceNodePos.y);

            referenceNode1Model = CreateNode("Node1", k_ReferenceNodePos, 0, 0, 0, 2);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos, 0, 0, 1);

            var node1FirstOutputPort = referenceNode1Model.OutputsByDisplayOrder[0];
            var node1SecondOutputPort = referenceNode1Model.OutputsByDisplayOrder[1];
            var node2InputPort = snappingNodeModel.InputsByDisplayOrder[0] as PortModel;

            Debug.Log("Capacity = " + node2InputPort.Capacity);

            Assert.IsNotNull(node1FirstOutputPort);
            Assert.IsNotNull(node1SecondOutputPort);
            Assert.IsNotNull(node2InputPort);

            MarkGraphViewStateDirty();
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(node1FirstOutputPort, node2InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(node1SecondOutputPort, node2InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // We move the snapping node closer to the first output port within the snapping range
            float offSetY = -k_SnapDistance;
            Vector2 moveOffset = new Vector2(0, offSetY);

            // Move the snapping node.
            Helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            Helpers.MouseDragEvent(start, end);
            yield return null;

            Helpers.MouseUpEvent(end);
            yield return null;

            // Get the UI ports
            var node1FirstOutputPortUI = node1FirstOutputPort.GetView<Port>(GraphView);
            var node1SecondOutputPortUI = node1SecondOutputPort.GetView<Port>(GraphView);
            var node2InputPortUI = node2InputPort.GetView<Port>(GraphView);
            Assert.IsNotNull(node1FirstOutputPortUI);
            Assert.IsNotNull(node1SecondOutputPortUI);
            Assert.IsNotNull(node2InputPortUI);

            // The snapping Node2's port should snap to Node1's first output port
            Assert.AreEqual(node1FirstOutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);
            Assert.AreNotEqual(node1SecondOutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);

            // We move the snapping node closer to the second output port within the snapping range
            offSetY = node1SecondOutputPortUI.GetGlobalCenter().y - node2InputPortUI.GetGlobalCenter().y - k_SnapDistance;
            moveOffset = new Vector2(0, offSetY);

            // Move the snapping node.
            Helpers.MouseDownEvent(start);
            yield return null;

            end = start + moveOffset;
            Helpers.MouseDragEvent(start, end);
            yield return null;

            Helpers.MouseUpEvent(end);
            yield return null;

            // The snapping Node2's port should snap to Node1's second output port
            Assert.AreEqual(node1SecondOutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);
            Assert.AreNotEqual(node1FirstOutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);

            yield return null;
        }
    }
}
