using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class SnapToBordersTests : GraphViewSnappingTester
    {
        static readonly Vector2 k_ReferenceNodePos = new Vector2(SelectionDragger.panAreaWidth, SelectionDragger.panAreaWidth);
        static readonly Vector2 k_SnappedNodeSize = new Vector2(100, 100);
        static readonly Vector2 k_ReferenceNodeSizeHorizontal = new Vector2(200, 100);
        static readonly Vector2 k_ReferenceNodeSizeVertical = new Vector2(100, 200);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_SnappedNode = null;
            m_ReferenceNode1 = null;
            m_SnappingNodePos = Vector2.zero;

            GraphViewSettings.UserSettings.EnableSnapToBorders = true;
            GraphViewSettings.UserSettings.EnableSnapToPort = false;
            GraphViewSettings.UserSettings.EnableSnapToSpacing = false;
            GraphViewSettings.UserSettings.EnableSnapToGrid = false;
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250), k_ReferenceNodePos + new Vector2(0, 200));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float topToTopDistance = m_SnappedNode.layout.yMin - m_ReferenceNode1.layout.yMin;
            float offSetY = k_SnapDistance - topToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's top border should snap to the reference node's top border, but the X should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.yMin, m_SnappedNode.layout.yMin);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float topToBottomDistance = m_ReferenceNode1.layout.yMax - m_SnappedNode.layout.yMin;
            float offSetY = topToBottomDistance - k_SnapDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's top border should snap to the reference node's bottom border in Y, but the X should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.yMax, m_SnappedNode.layout.yMin);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterYBorderShouldSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250), k_ReferenceNodePos + new Vector2(0, 200));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float centerToTopDistance = m_SnappedNode.layout.center.y - m_ReferenceNode1.layout.yMin;
            float offSetY = k_SnapDistance - centerToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's center should snap to the reference node's top border in Y, but the X should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.yMin, m_SnappedNode.layout.center.y);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterYBorderShouldSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float centerToBottomDistance = m_ReferenceNode1.layout.yMax - m_SnappedNode.layout.center.y;
            float offSetY = centerToBottomDistance - k_SnapDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's center should snap to the reference node's bottom border in Y, but the X should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.yMax, m_SnappedNode.layout.center.y);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250), k_ReferenceNodePos + new Vector2(0, 200));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float bottomToTopDistance = m_SnappedNode.layout.yMax - m_ReferenceNode1.layout.yMin;
            float offSetY = k_SnapDistance - bottomToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's bottom should snap to the reference node's top border in Y, but the X should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.yMin, m_SnappedNode.layout.yMax);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float bottomToBottomDistance = m_ReferenceNode1.layout.yMax - m_SnappedNode.layout.yMax;
            float offSetY = k_SnapDistance + bottomToBottomDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's center should snap to the reference node's top border in Y, but the X should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.yMax, m_SnappedNode.layout.yMax);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldNotSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250), k_ReferenceNodePos + new Vector2(0, 200));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float topToTopDistance = m_SnappedNode.layout.yMin - m_ReferenceNode1.layout.yMin;
            float offSetY = (k_SnapDistance + 1) - topToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's top border should not snap to the reference node's top border, but the X and Y should be dragged normally
            Assert.AreNotEqual(m_ReferenceNode1.layout.yMin, m_SnappedNode.layout.yMin);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y), m_SnappedNode.layout.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x), m_SnappedNode.layout.x);
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldNotSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float topToBottomDistance = m_SnappedNode.layout.yMin - m_ReferenceNode1.layout.yMin;
            float offSetY = (k_SnapDistance + 1) - topToBottomDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's top border should not snap to the reference node's top border: the X and Y should be dragged normally
            Assert.AreNotEqual(m_ReferenceNode1.layout.yMin, m_SnappedNode.layout.yMin);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y), m_SnappedNode.layout.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x), m_SnappedNode.layout.x);
        }

        [UnityTest]
        public IEnumerator ElementCenterYBorderShouldNotSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250), k_ReferenceNodePos + new Vector2(0, 200));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float centerToTopDistance = m_SnappedNode.layout.center.y - m_ReferenceNode1.layout.yMin;
            float offSetY = (k_SnapDistance + 1) - centerToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's center should not snap to the reference node's top border in Y: the X and Y should be dragged normally
            Assert.AreNotEqual(m_ReferenceNode1.layout.yMin, m_SnappedNode.layout.center.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y), m_SnappedNode.layout.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x), m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterYBorderShouldNotSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float centerToBottomDistance = m_ReferenceNode1.layout.yMax - m_SnappedNode.layout.center.y;
            float offSetY = centerToBottomDistance - (k_SnapDistance + 1);
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's center should not snap to the reference node's bottom border in Y: the X and Y should be dragged normally
            Assert.AreNotEqual(m_ReferenceNode1.layout.yMax, m_SnappedNode.layout.center.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y), m_SnappedNode.layout.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x), m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldNotSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250), k_ReferenceNodePos + new Vector2(0, 200));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float bottomToTopDistance = m_SnappedNode.layout.yMax - m_ReferenceNode1.layout.yMin;
            float offSetY = (k_SnapDistance + 1) - bottomToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's bottom should not snap to the reference node's top border in Y: the X and Y should be dragged normally
            Assert.AreNotEqual(m_ReferenceNode1.layout.yMin, m_SnappedNode.layout.yMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y), m_SnappedNode.layout.y, 0.0001f);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x), m_SnappedNode.layout.x, 0.0001f);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldNotSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float bottomToBottomDistance = m_ReferenceNode1.layout.yMax - m_SnappedNode.layout.yMax;
            float offSetY = (k_SnapDistance + 1) + bottomToBottomDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's center should not snap to the reference node's top border in Y: the X and Y should be dragged normally
            Assert.AreNotEqual(m_ReferenceNode1.layout.yMax, m_SnappedNode.layout.yMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y), m_SnappedNode.layout.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x), m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementLeftBorderShouldSnapToLeftBorder()
        {
            // Config
            //
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100), k_ReferenceNodePos + new Vector2(100, 0));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeHorizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float leftToLeftDistance = m_SnappedNode.layout.xMin - m_ReferenceNode1.layout.xMin;
            float offSetX = k_SnapDistance - leftToLeftDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's left border should snap to the reference node's left border in X, but Y should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.xMin, m_SnappedNode.layout.xMin);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementLeftBorderShouldSnapToRightBorder()
        {
            // Config
            //
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeHorizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float leftToRightDistance = Math.Abs(m_ReferenceNode1.layout.xMax - m_SnappedNode.layout.xMin);
            float offSetX = k_SnapDistance + leftToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's left border should snap to the reference node's right border in X, but Y should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.xMax, m_SnappedNode.layout.xMin);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterXBorderShouldSnapToLeftBorder()
        {
            // Config
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeHorizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float centerToLeftDistance = m_SnappedNode.layout.center.x - m_ReferenceNode1.layout.xMin;
            float offSetX = k_SnapDistance - centerToLeftDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's centerX border should snap to the reference node's left border in X, but Y should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.xMin, m_SnappedNode.layout.center.x);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterXBorderShouldSnapToRightBorder()
        {
            // Config
            //
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeHorizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float centerToRightDistance = m_ReferenceNode1.layout.xMax - m_SnappedNode.layout.center.x;
            float offSetX = k_SnapDistance + centerToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's centerX border should snap to the reference node's right border in X, but Y should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.xMax, m_SnappedNode.layout.center.x);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementRightBorderShouldSnapToLeftBorder()
        {
            // Config
            //
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100), k_ReferenceNodePos + new Vector2(100, 0));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeHorizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float rightToLeftDistance = m_SnappedNode.layout.xMax - m_ReferenceNode1.layout.xMin;
            float offSetX = k_SnapDistance - rightToLeftDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's right border should snap to the reference node's left border in X, but Y should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.xMin, m_SnappedNode.layout.xMax);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementRightBorderShouldSnapToRightBorder()
        {
            // Config
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100), k_ReferenceNodePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeHorizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float rightToRightDistance = m_ReferenceNode1.layout.xMax - m_SnappedNode.layout.xMax;
            float offSetX = k_SnapDistance + rightToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's right border should snap to the reference node's right border in X, but Y should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.xMax, m_SnappedNode.layout.xMax);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShouldSnapToMultipleElements()
        {
            // Config
            //           +-------+
            //  +-------+| Node2 |
            //  | Node1 |+-------+
            //  +-------+             O <-- should snap there
            //                        +-------+
            //                        | Node3 |
            //                        +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 200, k_ReferenceNodePos.y - 50), k_ReferenceNodePos, k_ReferenceNodePos + new Vector2(400, 200));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeVertical, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float offsetY = m_ReferenceNode1.layout.yMax - m_SnappedNode.layout.yMax + k_SnapDistance;
            float offSetX = m_ReferenceNode2.layout.xMin - m_SnappedNode.layout.xMax + k_SnapDistance;
            Vector2 moveOffset = new Vector2(offSetX, offsetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's bottom right corner should snap to the snapping point O
            Assert.AreEqual(m_ReferenceNode1.layout.yMax, m_SnappedNode.layout.yMax);
            Assert.AreEqual(m_ReferenceNode2.layout.xMin, m_SnappedNode.layout.xMax);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShouldNotSnapWhenShiftPressed()
        {
            // Config
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100), k_ReferenceNodePos, k_ReferenceNodePos + new Vector2(400, 200));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_SnappedNodeSize, k_ReferenceNodeSizeHorizontal, k_ReferenceNodeSizeVertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            float rightToRightDistance = m_ReferenceNode1.layout.xMax - m_SnappedNode.layout.xMax;
            float offSetX = k_SnapDistance + rightToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping node.
            Helpers.MouseDownEvent(start, MouseButton.LeftMouse, EventModifiers.Shift);
            yield return null;

            Vector2 end = start + moveOffset;
            Helpers.MouseDragEvent(start, end, MouseButton.LeftMouse, EventModifiers.Shift);
            yield return null;

            Helpers.MouseUpEvent(end, MouseButton.LeftMouse, EventModifiers.Shift);
            yield return null;

            // The snapping node's right border should not snap to the reference node's right border in X: X and Y should be dragged normally
            Assert.AreNotEqual(m_ReferenceNode1.layout.xMax, m_SnappedNode.layout.xMax);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, m_SnappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacematShouldSnap()
        {
            // Config
            //  +---------------+
            //  |     Node1     |
            //  +---------------+
            //     +---------+
            //     | Placemat|
            //     +---------+

            referenceNode1Model = CreateNode("Node1", k_ReferenceNodePos);

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);
            var placematModel = CreatePlacemat(new Rect(m_SnappingNodePos, new Vector2(200, 200)), "Placemat");

            MarkGraphViewStateDirty();
            yield return null;

            // Get the UI nodes
            var snappedPlacemat = placematModel.GetView<Placemat>(GraphView);
            m_ReferenceNode1 = referenceNode1Model.GetView<Node>(GraphView);
            Assert.IsNotNull(snappedPlacemat);
            Assert.IsNotNull(m_ReferenceNode1);

            // Changing the nodes' sizes to make it easier to test the snapping
            SetUINodeSize(ref m_ReferenceNode1, 100, 300);
            yield return null;

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            float rightToRightDistance = m_ReferenceNode1.layout.xMax - snappedPlacemat.layout.xMax;
            float offSetX = k_SnapDistance + rightToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping placemat
            Helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            Helpers.MouseDragEvent(start, end);
            yield return null;

            Helpers.MouseUpEvent(end);
            yield return null;

            // The snapping placemat's right border should snap to the reference node's right border in X: Y should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.xMax, snappedPlacemat.layout.xMax);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedPlacemat.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedPlacemat.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementUnderMouseShouldSnapWhenMultipleSelectedElements()
        {
            // Config
            //   +-------+
            //   | Node1 | +-------+
            //   +-------+ | Node2 | +----------+
            //             +-------+ | Placemat |
            //                       +----------+

            referenceNode1Model = CreateNode("Node1", k_ReferenceNodePos);
            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 200, k_ReferenceNodePos.y + 100);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            // Third element
            Vector2 secondSelectedElementPos = new Vector2(m_SnappingNodePos.x + 200, m_SnappingNodePos.y + 100);
            var placematModel = CreatePlacemat(new Rect(secondSelectedElementPos, new Vector2(200, 200)), "Placemat");

            MarkGraphViewStateDirty();
            yield return null;

            Vector2 worldPosNode2 = GraphView.ContentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 worldPosNode3 = GraphView.ContentViewContainer.LocalToWorld(secondSelectedElementPos);

            Vector2 selectionPosNode2 = worldPosNode2 + m_SelectionOffset;
            Vector2 selectionPosNode3 = worldPosNode3 + m_SelectionOffset;

            // Select placemat by clicking on it and pressing Ctrl
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

            Node node1 = referenceNode1Model.GetView<Node>(GraphView);
            Node node2 = snappingNodeModel.GetView<Node>(GraphView);
            Placemat placemat = placematModel.GetView<Placemat>(GraphView);
            Assert.IsNotNull(node1);
            Assert.IsNotNull(node2);
            Assert.IsNotNull(placemat);

            // Move Node2 toward reference Node1 within the snapping range
            float topToTopDistance = node2.layout.yMin - node1.layout.yMin;
            float offSetY = k_SnapDistance - topToTopDistance;
            Vector2 moveOffset = new Vector2(0, offSetY);
            Vector2 end = selectionPosNode2 + moveOffset;
            Helpers.MouseDragEvent(selectionPosNode2, end, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            Helpers.MouseUpEvent(end, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            // The snapping Node2 top border should snap to Node1's top border
            Assert.AreEqual(node1.layout.yMin, node2.layout.yMin);
            // placemat top border should not snap to Node1's top border
            Assert.AreNotEqual(node1.layout.yMin, placemat.layout.yMin);
            // placemat should have moved by the move offset in x and the same y offset as Node2
            Assert.AreEqual(secondSelectedElementPos.x + moveOffset.x, placemat.layout.x);
            Assert.AreEqual(secondSelectedElementPos.y - topToTopDistance, placemat.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacematWithCollapsedElementShouldSnap()
        {
            // Config
            //  +---------------+
            //  |     Node1     |
            //  +---------------+
            //     +---------+
            //     | Placemat|
            //     | +-----+ |
            //     | |Node2| |
            //     | +-----+ |
            //     +---------+

            referenceNode1Model = CreateNode("Node1", k_ReferenceNodePos);

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);
            var placematModel = CreatePlacemat(new Rect(m_SnappingNodePos, new Vector2(200, 200)), "Placemat");

            var nodeInsidePlacematPos = m_SnappingNodePos + new Vector2(60, 60);
            var nodeInsidePlacematModel = CreateNode("Node2", nodeInsidePlacematPos);
            MarkGraphViewStateDirty();
            yield return null;

            // Get the UI nodes
            var snappedPlacemat = placematModel.GetView<Placemat>(GraphView);
            m_ReferenceNode1 = referenceNode1Model.GetView<Node>(GraphView);
            var nodeInsidePlacemat = nodeInsidePlacematModel.GetView<Node>(GraphView);

            Assert.IsNotNull(snappedPlacemat);
            Assert.IsNotNull(m_ReferenceNode1);
            Assert.IsNotNull(nodeInsidePlacemat);

            // Changing the nodes' sizes to make it easier to test the snapping
            SetUINodeSize(ref m_ReferenceNode1, 100, 300);
            SetUINodeSize(ref nodeInsidePlacemat, 100, 100);
            yield return null;

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            float rightToRightDistance = m_ReferenceNode1.layout.xMax - snappedPlacemat.layout.xMax;
            float offSetX = k_SnapDistance + rightToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping placemat
            Helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            Helpers.MouseDragEvent(start, end);
            yield return null;

            Helpers.MouseUpEvent(end);
            yield return null;

            // The snapping placemat's right border should snap to the reference node's right border in X: Y should be dragged normally
            Assert.AreEqual(m_ReferenceNode1.layout.xMax, snappedPlacemat.layout.xMax);
            Assert.AreNotEqual(m_ReferenceNode1.layout.xMax, nodeInsidePlacemat.layout.xMax);
            Assert.AreEqual(nodeInsidePlacematPos.x + rightToRightDistance, nodeInsidePlacemat.layout.x);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedPlacemat.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedPlacemat.layout.y);

            yield return null;
        }
    }
}
