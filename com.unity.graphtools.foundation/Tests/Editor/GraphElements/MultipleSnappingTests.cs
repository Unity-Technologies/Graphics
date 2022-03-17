using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.TestTools;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class MultipleSnappingTests : GraphViewSnappingTester
    {
        const float k_Spacing = 200f;
        const float k_HalfSpacing = k_Spacing * 0.5f;
        const float k_QuarterSpacing = k_HalfSpacing * 0.5f;
        static readonly Vector2 k_ReferenceNodePos = new Vector2(k_Spacing + 200f, k_Spacing + 50f);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            GraphViewSettings.UserSettings.EnableSnapToPort = true;
            GraphViewSettings.UserSettings.EnableSnapToBorders = true;
            GraphViewSettings.UserSettings.EnableSnapToGrid = true;
            GraphViewSettings.UserSettings.EnableSnapToSpacing = true;

            m_SnappedNode = null;
            m_ReferenceNode1 = null;
            m_ReferenceNode2 = null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToGridAndPort()
        {
            // Config
            //           |          |
            //   --------+-------+----+------ +
            //           | Node1 o----o Node2 |
            //           +-------+  | +-------+
            //           |          |
            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToSpacing = false;

            var actions = SetUpUIElements(new Vector2(k_Spacing, k_Spacing + k_QuarterSpacing), k_ReferenceNodePos, Vector2.zero, false, true);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing));
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(
                SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Left),
                k_SnapDistance);

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

            // Snapped to Port
            Assert.AreEqual(inputPortUI.GetGlobalCenter().y, outputPortUI.GetGlobalCenter().y);

            // Snapped to Grid
            var borderWidth = SnapToGridStrategy.GetBorderWidth(m_SnappedNode);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.x - borderWidth.Left));
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y), m_SnappedNode.layout.y, 0.0001);

            // Should not be dragged normally
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.x - borderWidth.Left));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToBorderAndPort()
        {
            // Config
            // +-----==+
            // | Node3 |
            // +-------+
            //          +-------+    +-------+
            //          | Node1 o----o Node2 |
            //          +-------+    +-------+
            //
            GraphViewSettings.UserSettings.EnableSnapToGrid = false;
            GraphViewSettings.UserSettings.EnableSnapToSpacing = false;

            var actions = SetUpUIElements(new Vector2(k_Spacing, 2f * k_Spacing + k_QuarterSpacing), k_ReferenceNodePos, new Vector2(k_Spacing, 0), false, true);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing));
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(k_SnapDistance, -(k_Spacing + k_SnapDistance));
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

            // Snapped to Port
            Assert.AreEqual(inputPortUI.GetGlobalCenter().y, outputPortUI.GetGlobalCenter().y);

            // Left and right borders of snapped node are snapped to left and right border of second reference node
            Assert.AreEqual(m_ReferenceNode2.layout.xMax, m_SnappedNode.layout.xMax);
            Assert.AreEqual(m_ReferenceNode2.layout.xMin, m_SnappedNode.layout.xMin);

            // Should not be dragged normally
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToBorderAndGrid()
        {
            // Config
            //   |            |
            // --+------------+--------
            //   |            |
            //   +-------+    +-------+
            //   | Node1 o----o Node2 |
            //   +-------+    +-------+
            //   |            |
            // --+------------+-------|
            //   |            |
            //   |            |

            GraphViewSettings.UserSettings.EnableSnapToPort = false;
            GraphViewSettings.UserSettings.EnableSnapToSpacing = false;

            var actions = SetUpUIElements(new Vector2(k_Spacing + k_QuarterSpacing, 2f * k_Spacing + k_QuarterSpacing), k_ReferenceNodePos, Vector2.zero, false, true);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing));
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(k_SnapDistance - k_QuarterSpacing, -(k_Spacing + k_SnapDistance));
            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Snapped to first grid line (positioned at k_Spacing)
            var borderWidth = SnapToGridStrategy.GetBorderWidth(m_SnappedNode);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(k_Spacing),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.x - borderWidth.Left));

            // Snapped to borders
            Assert.AreEqual(m_SnappedNode.layout.y, m_ReferenceNode1.layout.y);

            // Should not be dragged normally
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.y - borderWidth.Top));
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.x - borderWidth.Left));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToSpacingAndPort()
        {
            // Config
            //
            //   +-------+     +-------+     +-------+
            //   | Node2 |     | Node2 o-----o Node1 |
            //   +-------+     +-------+     +-------+
            //

            GraphViewSettings.UserSettings.EnableSnapToGrid = false;
            GraphViewSettings.UserSettings.EnableSnapToBorders = false;

            var actions = SetUpUIElements(k_ReferenceNodePos + new Vector2(k_HalfSpacing + k_QuarterSpacing, k_QuarterSpacing),
                k_ReferenceNodePos,
                new Vector2(k_ReferenceNodePos.x - k_HalfSpacing - k_QuarterSpacing, k_ReferenceNodePos.y), false, true);

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing));
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(k_SnapDistance, k_SnapDistance - k_QuarterSpacing);
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

            // Snapped to Port
            Assert.AreEqual(inputPortUI.GetGlobalCenter().y, outputPortUI.GetGlobalCenter().y);

            // The three nodes should be evenly spaced horizontally
            float distanceBetweenNode1AndNode2 = m_ReferenceNode1.layout.xMin - m_SnappedNode.layout.xMax;
            float distanceBetweenNode2AndNode3 = m_ReferenceNode2.layout.xMin - m_ReferenceNode1.layout.xMax;

            Assert.AreEqual(distanceBetweenNode1AndNode2, distanceBetweenNode2AndNode3);

            // Should not be dragged normally
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToSpacingAndGrid()
        {
            // Config
            //
            //---+-------+-----------------------------
            //   | Node1 |     +-------+     +-------+
            //   +-------+     | Node2 |     | Node3 |
            //                 +-------+     +-------+
            //

            GraphViewSettings.UserSettings.EnableSnapToPort = false;
            GraphViewSettings.UserSettings.EnableSnapToBorders = false;

            var actions = SetUpUIElements(new Vector2(k_HalfSpacing + k_QuarterSpacing, k_ReferenceNodePos.y + k_QuarterSpacing), k_ReferenceNodePos, k_ReferenceNodePos + new Vector2(k_Spacing + k_QuarterSpacing, 0));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing));
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(k_SnapDistance, -k_HalfSpacing + SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Top));
            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Snapped to top grid line
            var borderWidth = SnapToGridStrategy.GetBorderWidth(m_SnappedNode);

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(k_Spacing),
                m_SnappedNode.layout.yMin - GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Top));

            // The three nodes should be evenly spaced horizontally
            float distanceBetweenNode1AndNode2 = m_ReferenceNode1.layout.xMin - m_SnappedNode.layout.xMax;
            float distanceBetweenNode2AndNode3 = m_ReferenceNode2.layout.xMin - m_ReferenceNode1.layout.xMax;

            Assert.AreEqual(distanceBetweenNode1AndNode2, distanceBetweenNode2AndNode3);

            // Should not be dragged normally
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y), m_SnappedNode.layout.y);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x), m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToSpacingAndBorder()
        {
            // Config
            //
            //   +-------+     +-------+     +-------+
            //   | Node1 |     | Node2 |     | Node3 |
            //   +-------+     +-------+     +-------+
            //

            GraphViewSettings.UserSettings.EnableSnapToPort = false;
            GraphViewSettings.UserSettings.EnableSnapToGrid = false;

            var actions = SetUpUIElements(new Vector2(k_Spacing + k_QuarterSpacing - k_HalfSpacing, m_ReferenceNode1Pos.y + k_QuarterSpacing), k_ReferenceNodePos, m_ReferenceNode1Pos + new Vector2(k_Spacing + k_QuarterSpacing, 0));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing));
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(k_SnapDistance, k_SnapDistance - k_QuarterSpacing);
            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Snapped to top and bottom Border
            Assert.AreEqual(m_ReferenceNode1.layout.yMin, m_SnappedNode.layout.yMin);
            Assert.AreEqual(m_ReferenceNode1.layout.yMax, m_SnappedNode.layout.yMax);

            // The three nodes should be evenly spaced horizontally
            float distanceBetweenNode1AndNode2 = m_ReferenceNode1.layout.xMin - m_SnappedNode.layout.xMax;
            float distanceBetweenNode2AndNode3 = m_ReferenceNode2.layout.xMin - m_ReferenceNode1.layout.xMax;

            Assert.AreEqual(distanceBetweenNode1AndNode2, distanceBetweenNode2AndNode3);

            // Should not be dragged normally
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToBorderAndGridAndSpacingAndPort()
        {
            // Config
            //   |              |             |
            // --+--------------+-------------+
            //   |              |             |
            //   +-------+      +-------+     +-------+
            //   | Node3 |      | Node2 o-----o Node1 |
            //   +-------+      +-------+     +-------+
            //   |              |             |
            // --+--------------+-------------+
            //   |              |             |
            //   |              |             |

            var actions = SetUpUIElements(new Vector2(3f * k_Spacing, k_ReferenceNodePos.y),
                k_ReferenceNodePos,
                new Vector2(k_ReferenceNodePos.x - k_Spacing, k_ReferenceNodePos.y), false, true);


            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing), new Vector2(k_HalfSpacing, k_HalfSpacing));
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Left), k_SnapDistance);
            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Snapped to first grid line (positioned at k_Spacing)
            var borderWidth = SnapToGridStrategy.GetBorderWidth(m_SnappedNode);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(3f * k_Spacing),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.x - borderWidth.Left));

            // Snapped to borders
            Assert.AreEqual(m_SnappedNode.layout.y, m_ReferenceNode1.layout.y);

            // Get the UI ports
            var outputPortUI = m_OutputPort.GetView<Port>(GraphView);
            var inputPortUI = m_InputPort.GetView<Port>(GraphView);
            Assert.IsNotNull(outputPortUI);
            Assert.IsNotNull(inputPortUI);

            // Snapped to Port
            Assert.AreEqual(inputPortUI.GetGlobalCenter().y, outputPortUI.GetGlobalCenter().y);

            // The three nodes should be evenly spaced horizontally up to one pixel difference
            float distanceBetweenNode1AndNode2 = m_SnappedNode.layout.xMin - GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Right) - m_ReferenceNode1.layout.xMax;
            float distanceBetweenNode2AndNode3 = m_ReferenceNode1.layout.xMin - m_ReferenceNode2.layout.xMax;

            var pixelSize = 1f / GraphViewStaticBridge.PixelPerPoint;
            Assert.AreEqual(distanceBetweenNode1AndNode2, distanceBetweenNode2AndNode3, pixelSize + 0.0001f);

            // Should not be dragged normally
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }
    }
}
