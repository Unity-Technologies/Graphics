using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.TestTools;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class SnapToSpacing : GraphViewSnappingTester
    {
        const float k_NodeSize = 50;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_SnappedNode = null;
            m_ReferenceNode1 = null;
            m_ReferenceNode2 = null;
            m_SnappingNodePos = Vector2.zero;
            m_ReferenceNode1Pos = Vector2.zero;
            m_ReferenceNode2Pos = Vector2.zero;

            GraphViewSettings.UserSettings.EnableSnapToSpacing = true;
            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToPort = false;
            GraphViewSettings.UserSettings.EnableSnapToGrid = false;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToHorizontalSpacingPositionA()
        {
            // Config (snapped node should snap to A)
            //
            //              +-----+         +-----+
            //    A         |  1  |         |  2  |
            //              +-----+         +-----+
            //

            var actions = SetUpUIElements(new Vector2(0, 100), new Vector2(300, 100), new Vector2(450, 100));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.xMax - m_ReferenceNode2.layout.xMin);
            float positionA = m_ReferenceNode1.layout.xMin - node1Node2Spacing;

            float offSetX = positionA - m_SnappedNode.layout.xMax + k_SnapDistance;
            Vector2 moveOffset = new Vector2(offSetX, 0);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(positionA), m_SnappedNode.layout.xMax);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToHorizontalSpacingPositionB()
        {
            // Config (snapped node should snap to B)
            //
            //              +-----+         +-----+
            //              |  1  |    B    |  2  |
            //              +-----+         +-----+
            //

            var actions = SetUpUIElements(new Vector2(0, 100), new Vector2(300, 100), new Vector2(450, 100));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.xMax - m_ReferenceNode2.layout.xMin);
            float positionB = m_ReferenceNode1.layout.xMax + node1Node2Spacing / 2;

            float offSetX = positionB - m_SnappedNode.layout.center.x + k_SnapDistance;
            Vector2 moveOffset = new Vector2(offSetX, 0);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(positionB),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.center.x));
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToHorizontalSpacingPositionC()
        {
            // Config (snapped node should snap to C)
            //
            //              +-----+         +-----+
            //              |  1  |         |  2  |         C
            //              +-----+         +-----+
            //

            var actions = SetUpUIElements(new Vector2(0, 100), new Vector2(300, 100), new Vector2(450, 100));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.xMax - m_ReferenceNode2.layout.xMin);
            float positionC = m_ReferenceNode2.layout.xMax + node1Node2Spacing;

            m_SnappingNodePos = m_SnappedNode.layout.position;
            float offSetX = positionC - m_SnappedNode.layout.xMin + k_SnapDistance;
            Vector2 moveOffset = new Vector2(offSetX, 0);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(positionC), m_SnappedNode.layout.xMin);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldNotSnapToHorizontalSpacingPositions()
        {
            // Config (snapped node should not snap to A)
            //
            //              +-----+         +-----+
            //    A         |  1  |         |  2  |
            //              +-----+         +-----+
            //

            var actions = SetUpUIElements(new Vector2(0, 100), new Vector2(300, 100), new Vector2(450, 100));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.xMax - m_ReferenceNode2.layout.xMin);
            float positionA = m_ReferenceNode1.layout.xMin - node1Node2Spacing;

            float offSetX = positionA - m_SnappedNode.layout.xMax + k_SnapDistance + 1;
            Vector2 moveOffset = new Vector2(offSetX, 0);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(positionA), m_SnappedNode.layout.xMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldNotSnapToHorizontalSpacingPositionB()
        {
            // Config (snapped node should not snap to B)
            //
            //              +-----+         +-----+
            //              |  1  |    B    |  2  |
            //              +-----+         +-----+
            //

            var actions = SetUpUIElements(new Vector2(0, 100), new Vector2(300, 100), new Vector2(450, 100));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }

            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.xMax - m_ReferenceNode2.layout.xMin);
            float positionB = m_ReferenceNode1.layout.xMax + node1Node2Spacing / 2;

            float offSetX = positionB - m_SnappedNode.layout.center.x + k_SnapDistance + 1;
            Vector2 moveOffset = new Vector2(offSetX, 0);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(positionB), m_SnappedNode.layout.center.x);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldNotSnapToHorizontalSpacingPositionC()
        {
            // Config (snapped node should not snap to C)
            //
            //              +-----+         +-----+
            //              |  1  |         |  2  |         C
            //              +-----+         +-----+
            //

            var actions = SetUpUIElements(new Vector2(0, 100), new Vector2(300, 100), new Vector2(450, 100));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.xMax - m_ReferenceNode2.layout.xMin);
            float positionC = m_ReferenceNode2.layout.xMax + node1Node2Spacing;

            float offSetX = positionC - m_SnappedNode.layout.xMin + k_SnapDistance + 1;
            Vector2 moveOffset = new Vector2(offSetX, 0);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(positionC), m_SnappedNode.layout.xMin);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                m_SnappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToVerticalSpacingPositionA()
        {
            // Config (snapped node should snap to A)
            //
            //     A
            //
            //  +-----+
            //  |  1  |
            //  +-----+
            //
            //  +-----+
            //  |  1  |
            //  +-----+


            var actions = SetUpUIElements(new Vector2(100, 0), new Vector2(100, 300), new Vector2(100, 450));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.yMax - m_ReferenceNode2.layout.yMin);

            float positionA = m_ReferenceNode1.layout.yMin - node1Node2Spacing;

            float offSetY = positionA - m_SnappedNode.layout.yMax + k_SnapDistance;
            Vector2 moveOffset = new Vector2(0, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(positionA), m_SnappedNode.layout.yMax);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToVerticalSpacingPositionB()
        {
            // Config (snapped node should snap to B)
            //
            //  +-----+
            //  |  1  |
            //  +-----+
            //     B
            //  +-----+
            //  |  1  |
            //  +-----+

            var actions = SetUpUIElements(new Vector2(100, 0), new Vector2(100, 300), new Vector2(100, 450));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.yMax - m_ReferenceNode2.layout.yMin);
            float positionB = m_ReferenceNode1.layout.yMax + node1Node2Spacing / 2;

            float offSetY = positionB - m_SnappedNode.layout.center.y + k_SnapDistance;
            Vector2 moveOffset = new Vector2(0, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(positionB),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.center.y));
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToVerticalSpacingPositionC()
        {
            // Config (snapped node should snap to C)
            //
            //  +-----+
            //  |  1  |
            //  +-----+
            //
            //  +-----+
            //  |  1  |
            //  +-----+
            //
            //     C
            //

            var actions = SetUpUIElements(new Vector2(100, 0), new Vector2(100, 100), new Vector2(100, 250));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.yMax - m_ReferenceNode2.layout.yMin);
            float positionC = m_ReferenceNode2.layout.yMax + node1Node2Spacing;

            float offSetY = positionC - m_SnappedNode.layout.yMin + k_SnapDistance;
            Vector2 moveOffset = new Vector2(0, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(positionC), m_SnappedNode.layout.yMin);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldNotSnapToVerticalSpacingPositionA()
        {
            // Config (snapped node should not snap to A)
            //
            //     A
            //
            //  +-----+
            //  |  1  |
            //  +-----+
            //
            //  +-----+
            //  |  1  |
            //  +-----+


            var actions = SetUpUIElements(new Vector2(100, 0), new Vector2(100, 300), new Vector2(100, 450));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.yMax - m_ReferenceNode2.layout.yMin);
            float positionA = m_ReferenceNode1.layout.yMin - node1Node2Spacing;

            float offSetY = positionA - m_SnappedNode.layout.yMax + k_SnapDistance + 1;
            Vector2 moveOffset = new Vector2(0, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(positionA), m_SnappedNode.layout.yMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
        }

        [UnityTest]
        public IEnumerator ElementShouldNotSnapToVerticalSpacingPositionB()
        {
            // Config (snapped node should not snap to B)
            //
            //  +-----+
            //  |  1  |
            //  +-----+
            //     B
            //  +-----+
            //  |  1  |
            //  +-----+
            //

            var actions = SetUpUIElements(new Vector2(100, 0), new Vector2(100, 300), new Vector2(100, 450));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.yMax - m_ReferenceNode2.layout.yMin);
            float positionB = m_ReferenceNode1.layout.yMax + node1Node2Spacing / 2;

            float offSetY = positionB - m_SnappedNode.layout.center.y + k_SnapDistance + 1;
            Vector2 moveOffset = new Vector2(0, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(positionB), m_SnappedNode.layout.center.y);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);
        }

        [UnityTest]
        public IEnumerator ElementShouldNotSnapToVerticalSpacingPositionC()
        {
            // Config (snapped node should not snap to C)
            //
            //  +-----+
            //  |  1  |
            //  +-----+
            //
            //  +-----+
            //  |  1  |
            //  +-----+
            //
            //     C
            //

            var actions = SetUpUIElements(new Vector2(100, 0), new Vector2(100, 100), new Vector2(100, 250));
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize), new Vector2(k_NodeSize, k_NodeSize));
            while (actions.MoveNext())
            {
                yield return null;
            }
            float node1Node2Spacing = Math.Abs(m_ReferenceNode1.layout.yMax - m_ReferenceNode2.layout.yMin);
            float positionC = m_ReferenceNode2.layout.yMax + node1Node2Spacing;

            float offSetY = positionC - m_SnappedNode.layout.yMin + k_SnapDistance + 1;
            Vector2 moveOffset = new Vector2(0, offSetY);

            actions = MoveElementWithOffset(moveOffset);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(positionC), m_SnappedNode.layout.yMin);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y);

            yield return null;
        }
    }
}
