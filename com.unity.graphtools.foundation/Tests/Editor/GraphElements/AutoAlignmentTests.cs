using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class AutoAlignmentTests : AutoPlacementTestHelper
    {
        AutoAlignmentHelper m_AlignmentHelper;

        IEnumerator AlignElements(AutoAlignmentHelper.AlignmentReference reference)
        {
            m_AlignmentHelper.SendAlignCommand(reference);
            yield return null;

            // Get the UI elements
            m_FirstNode = FirstNodeModel.GetUI<Node>(graphView);
            m_SecondNode = SecondNodeModel.GetUI<Node>(graphView);
            m_ThirdNode = ThirdNodeModel.GetUI<Node>(graphView);
            m_FourthNode = FourthNodeModel.GetUI<Node>(graphView);
            m_Placemat = PlacematModel.GetUI<Placemat>(graphView);
            m_StickyNote = StickyNoteModel.GetUI<StickyNote>(graphView);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_AlignmentHelper = new AutoAlignmentHelper(graphView);
        }

        [UnityTest]
        public IEnumerator AlignElementsToTop()
        {
            // Config:
            //
            // --+-----+--+-----+--+-----+--+-----+-- top
            //   |Node1|  |Node2|  |place|  |stick|
            //   +-----+  +-----+  +-----+  +-----+

            float expectedTopValue = GraphViewStaticBridge.RoundToPixelGrid(10);

            Vector2 firstNodePos = new Vector2(0, 50);
            Vector2 secondNodePos = new Vector2(200, expectedTopValue);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Top);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedTopValue, m_FirstNode.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_SecondNode.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_Placemat.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_StickyNote.layout.yMin);
        }

        [UnityTest]
        public IEnumerator AlignElementsToBottom()
        {
            // Config:
            //
            //   +-----+  +-----+  +-----+  +-----+
            //   |Node1|  |Node2|  |place|  |stick|
            // --+-----+--+-----+--+-----+--+-----+-- bottom

            float expectedBottomValue = 300;

            Vector2 firstNodePos = new Vector2(0, 50);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, expectedBottomValue);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Bottom);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_FirstNode.layout.height), m_FirstNode.layout.yMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_SecondNode.layout.height), m_SecondNode.layout.yMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_Placemat.layout.height), m_Placemat.layout.yMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_StickyNote.layout.height), m_StickyNote.layout.yMax);
        }

        [UnityTest]
        public IEnumerator AlignElementsToLeft()
        {
            // Config:
            //    |
            //    |-----+
            //    |Node1|
            //    |-----+
            //    |-----+
            //    |Node2|
            //    |-----+
            //    |-----+
            //    |stick|
            //    |-----+
            //    |-----+
            //    |place|
            //    |-----+
            //left|

            float expectedLeftValue = GraphViewStaticBridge.RoundToPixelGrid(0);

            Vector2 firstNodePos = new Vector2(expectedLeftValue, 50);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Left);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedLeftValue, m_FirstNode.layout.xMin);
            Assert.AreEqual(expectedLeftValue, m_SecondNode.layout.xMin);
            Assert.AreEqual(expectedLeftValue, m_Placemat.layout.xMin);
            Assert.AreEqual(expectedLeftValue, m_StickyNote.layout.xMin);
        }

        [UnityTest]
        public IEnumerator AlignElementsToRight()
        {
            // Config:
            //         |
            //   +-----|
            //   |Node1|
            //   +-----|
            //   +-----|
            //   |Node2|
            //   +-----|
            //   +-----|
            //   |stick|
            //   +-----|
            //   +-----|
            //   |place|
            //   +-----|
            //         | right

            const float expectedRightValue = 600;

            Vector2 firstNodePos = new Vector2(0, 50);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(expectedRightValue, 200);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Right);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedRightValue + m_FirstNode.layout.width), m_FirstNode.layout.xMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedRightValue + m_SecondNode.layout.width), m_SecondNode.layout.xMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedRightValue + m_Placemat.layout.width), m_Placemat.layout.xMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedRightValue + m_StickyNote.layout.width), m_StickyNote.layout.xMax);
        }

        [UnityTest]
        public IEnumerator AlignElementsToHorizontalCenter()
        {
            // Config:
            //      |
            //   +--|--+
            //   |Node2|
            //   +--|--+
            //   +--|--+
            //   |stick|
            //   +--|--+
            // +----|----+
            // | placemat|
            // +----|----+
            //   +--|--+
            //   |Node1|
            //   +--|--+
            //      | horizontal center

            Vector2 firstNodePos = new Vector2(0, 400);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float expectedHorizontalCenterValue = GraphViewStaticBridge.RoundToPixelGrid((m_FirstNode.layout.center.x + m_SecondNode.layout.center.x + m_Placemat.layout.center.x + m_StickyNote.layout.center.x) / 4);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.HorizontalCenter);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedHorizontalCenterValue, m_FirstNode.layout.center.x);
            Assert.AreEqual(expectedHorizontalCenterValue, m_SecondNode.layout.center.x);
            Assert.AreEqual(expectedHorizontalCenterValue, m_Placemat.layout.center.x);
            Assert.AreEqual(expectedHorizontalCenterValue, m_StickyNote.layout.center.x);
        }

        [UnityTest]
        public IEnumerator AlignElementsToVerticalCenter()
        {
            // Config:
            //
            //   +-----+  +-----+  +-----+  +-----+
            //  --Node2----stick----place----Node1-- vertical center
            //   +-----+  +-----+  +-----+  +-----+

            Vector2 firstNodePos = new Vector2(0, 400);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float expectedVerticalCenterValue = GraphViewStaticBridge.RoundToPixelGrid((m_FirstNode.layout.center.y + m_SecondNode.layout.center.y + m_Placemat.layout.center.y + m_StickyNote.layout.center.y) / 4);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.VerticalCenter);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedVerticalCenterValue, m_FirstNode.layout.center.y);
            Assert.AreEqual(expectedVerticalCenterValue, m_SecondNode.layout.center.y);
            Assert.AreEqual(expectedVerticalCenterValue, m_Placemat.layout.center.y);
            Assert.AreEqual(expectedVerticalCenterValue, m_StickyNote.layout.center.y);
        }

        [UnityTest]
        public IEnumerator AlignOnlySelectedElements()
        {
            // Config:
            //
            // +-----+--+-----+--+-----+------- top
            // |Node1|  |Node2|  |place|
            // +-----+  +-----+  +-----+
            //
            //                          +-----+
            //                          |stick| <- not selected
            //                          +-----+
            //

            float expectedTopValue = GraphViewStaticBridge.RoundToPixelGrid(0);

            Vector2 firstNodePos = new Vector2(0, expectedTopValue);
            Vector2 secondNodePos = new Vector2(200, 200);
            Vector2 placematPos = new Vector2(400, 400);
            Vector2 stickyNotePos = new Vector2(600, 400);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 selectionPosStickyNote = graphView.ContentViewContainer.LocalToWorld(m_StickyNote.layout.position) + k_SelectionOffset;

            // Unselect StickyNote
            actions = SelectElement(selectionPosStickyNote);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Top);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedTopValue, m_FirstNode.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_SecondNode.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_Placemat.layout.yMin);
            Assert.AreNotEqual(expectedTopValue, m_StickyNote.layout.yMin);
        }

        [UnityTest]
        public IEnumerator AlignPlacemat()
        {
            // Config:
            //   +-----+
            // +-|Node1|-+
            // | +-----+ | +-----+  +------+
            // | placemat| |Node2|  |sticky|
            // +---------+-+-----+--+------+--- bottom

            const float expectedBottomValue = 300;

            Vector2 placematPos = new Vector2(0, 150);
            Vector2 firstNodePos = new Vector2(0, 0); // First node is on the placemat
            Vector2 secondNodePos = new Vector2(200, 200);
            Vector2 stickyNotePos = new Vector2(600, expectedBottomValue);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }


            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Bottom);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // First node follow placemat movement, but does not align to the bottom
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_FirstNode.layout.height), m_FirstNode.layout.yMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_Placemat.layout.height), m_Placemat.layout.yMax);

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_SecondNode.layout.height), m_SecondNode.layout.yMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_StickyNote.layout.height), m_StickyNote.layout.yMax);
        }

        [UnityTest]
        public IEnumerator AlignElementOnPlacemat()
        {
            // Config:
            // +---------+
            // | placemat|
            // |         |
            // | +-----+ | +-----+  +------+
            // +-|Node1|-+ |Node2|  |sticky|
            // --+-----+---+-----+--+------+--- bottom

            const float expectedBottomValue = 300;

            Vector2 placematPos = new Vector2(0, 0);
            Vector2 firstNodePos = new Vector2(0, 150); // First node is on the placemat
            Vector2 secondNodePos = new Vector2(200, 200);
            Vector2 stickyNotePos = new Vector2(600, expectedBottomValue);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }


            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Bottom);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // First node's yMax is greater than placemat's yMax: first node's yMax aligns to bottom, but not the placemat's
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_FirstNode.layout.height), m_FirstNode.layout.yMax);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_Placemat.layout.height), m_Placemat.layout.yMax);

            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_SecondNode.layout.height), m_SecondNode.layout.yMax);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(expectedBottomValue + m_StickyNote.layout.height), m_StickyNote.layout.yMax);
        }

        [UnityTest]
        public IEnumerator AlignConnectedNodesTop()
        {
            // Config:
            //  +-----+-----+-----+-----+-----+----- top
            //  |Node1o--+--oNode3o-----oNode4|
            //  +-----+  |  +-----+     +-----+
            //           |
            //  +-----+  |
            //  |Node2o--+
            //  +-----+
            //

            Vector2 firstNodePos = new Vector2(0, 200);
            Vector2 secondNodePos = new Vector2(0, 400);
            Vector2 thirdNodePos = new Vector2(300, 300);
            Vector2 fourthNodePos = new Vector2(600, 100);

            var actions = CreateConnectedNodes(firstNodePos, secondNodePos, thirdNodePos, fourthNodePos, false);
            while (actions.MoveNext())
            {
                yield return null;
            }

            SelectConnectedNodes();
            yield return null;

            float expectedDistanceBetweenFirstSecond = GraphViewStaticBridge.RoundToPixelGrid(secondNodePos.y - firstNodePos.y);
            float expectedYmin = GraphViewStaticBridge.RoundToPixelGrid(fourthNodePos.y);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Top);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedYmin, m_FirstNode.layout.yMin);
            Assert.AreNotEqual(expectedYmin, m_SecondNode.layout.yMin);
            Assert.AreEqual(expectedDistanceBetweenFirstSecond, m_SecondNode.layout.y - m_FirstNode.layout.y);
            Assert.AreEqual(expectedYmin, m_ThirdNode.layout.yMin);
            Assert.AreEqual(expectedYmin, m_FourthNode.layout.yMin);
        }

        [UnityTest]
        public IEnumerator AlignConnectedNodesBottom()
        {
            // Config:
            //  +-----+
            //  |Node1o--+
            //  +-----+  |
            //           |
            //  +-----+  |  +-----+     +-----+
            //  |Node2o--+--oNode3o-----oNode4|
            //  +-----+-----+-----+-----+-----+----- bottom
            //

            Vector2 firstNodePos = new Vector2(0, 0);
            Vector2 secondNodePos = new Vector2(0, 300);
            Vector2 thirdNodePos = new Vector2(300, 100);
            Vector2 fourthNodePos = new Vector2(600, 400);

            var actions = CreateConnectedNodes(firstNodePos, secondNodePos, thirdNodePos, fourthNodePos, false);
            while (actions.MoveNext())
            {
                yield return null;
            }

            SelectConnectedNodes();
            yield return null;

            float expectedDistanceBetweenFirstSecond = GraphViewStaticBridge.RoundToPixelGrid(secondNodePos.y - firstNodePos.y);
            float expectedYmax = GraphViewStaticBridge.RoundToPixelGrid(fourthNodePos.y + m_FourthNode.layout.height);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Bottom);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(expectedYmax, m_FirstNode.layout.yMax);
            Assert.AreEqual(expectedYmax, m_SecondNode.layout.yMax);
            Assert.AreEqual(expectedDistanceBetweenFirstSecond, m_SecondNode.layout.y - m_FirstNode.layout.y);
            Assert.AreEqual(expectedYmax, m_ThirdNode.layout.yMax);
            Assert.AreEqual(expectedYmax, m_FourthNode.layout.yMax);
        }

        [UnityTest]
        public IEnumerator AlignConnectedNodesLeft()
        {
            // Config:
            //  |
            //  +-----+
            //  |Node1o----+
            //  +-----+    |
            //  |          |
            //  +--------+ |
            //  |Nodes3&4o-+
            //  +--------+ |
            //  |          |
            //  | +-----+  |
            //  | |Node2o--+
            //  | +-----+
            //  |
            //  left

            Vector2 firstNodePos = new Vector2(0, 200);
            Vector2 secondNodePos = new Vector2(10, 400);
            Vector2 thirdNodePos = new Vector2(300, 300);
            Vector2 fourthNodePos = new Vector2(600, 100);

            var actions = CreateConnectedNodes(firstNodePos, secondNodePos, thirdNodePos, fourthNodePos, false);
            while (actions.MoveNext())
            {
                yield return null;
            }

            SelectConnectedNodes();
            yield return null;

            float expectedDistanceBetweenFirstSecond = GraphViewStaticBridge.RoundToPixelGrid(secondNodePos.x - firstNodePos.x);
            float expectedXmin = GraphViewStaticBridge.RoundToPixelGrid(firstNodePos.x);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Left);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedXmin, m_FirstNode.layout.xMin);
            Assert.AreNotEqual(expectedXmin, m_SecondNode.layout.xMin);
            Assert.AreEqual(expectedDistanceBetweenFirstSecond, m_SecondNode.layout.x - m_FirstNode.layout.x);
            Assert.AreEqual(expectedXmin, m_ThirdNode.layout.xMin);
            Assert.AreEqual(expectedXmin, m_FourthNode.layout.xMin);
        }

        [UnityTest]
        public IEnumerator AlignConnectedNodesRight()
        {
            // Config:
            //          |
            //  +-----+ |
            //  |Node1o---+
            //  +-----+ | |
            //          | |
            // +--------+ |
            // |Nodes3&4o-+
            // +--------+ |
            //          | |
            //    +-----+ |
            //    |Node2o-+
            //    +-----+
            //          |
            //          right

            Vector2 firstNodePos = new Vector2(0, 200);
            Vector2 secondNodePos = new Vector2(10, 400);
            Vector2 thirdNodePos = new Vector2(200, 100);
            Vector2 fourthNodePos = new Vector2(400, 100);

            var actions = CreateConnectedNodes(firstNodePos, secondNodePos, thirdNodePos, fourthNodePos, false);
            while (actions.MoveNext())
            {
                yield return null;
            }

            SelectConnectedNodes();
            yield return null;

            float expectedDistanceBetweenFirstSecond = GraphViewStaticBridge.RoundToPixelGrid(secondNodePos.x - firstNodePos.x);
            float expectedXmax = GraphViewStaticBridge.RoundToPixelGrid(fourthNodePos.x + m_FourthNode.layout.width);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Right);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(expectedXmax, m_FirstNode.layout.xMax);
            Assert.AreEqual(expectedXmax, m_SecondNode.layout.xMax);
            Assert.AreEqual(expectedDistanceBetweenFirstSecond, m_SecondNode.layout.x - m_FirstNode.layout.x);
            Assert.AreEqual(expectedXmax, m_ThirdNode.layout.xMax);
            Assert.AreEqual(expectedXmax, m_FourthNode.layout.xMax);
        }

        [UnityTest]
        public IEnumerator AlignConnectedNodesCenterHorizontal()
        {
            // Config:
            //
            //  +-----+
            //  |Node1o-------+
            //  +-----+       |
            //        |       |
            //   +---------+  |
            //   |Nodes3&4 o--+
            //   +---------+  |
            //        |       |
            //        +-----+ |
            //        |Node2o-+
            //        +-----+
            //        |
            //        center horizontal

            Vector2 firstNodePos = new Vector2(0, 0);
            Vector2 secondNodePos = new Vector2(100, 400);
            Vector2 thirdNodePos = new Vector2(300, 300);
            Vector2 fourthNodePos = new Vector2(400, 100);

            var actions = CreateConnectedNodes(firstNodePos, secondNodePos, thirdNodePos, fourthNodePos, false);
            while (actions.MoveNext())
            {
                yield return null;
            }

            SelectConnectedNodes();
            yield return null;

            float expectedDistanceBetweenFirstSecond = m_SecondNode.layout.xMin - m_FirstNode.layout.xMax;
            float expectedCenter = GraphViewStaticBridge.RoundToPixelGrid(((m_SecondNode.layout.center.x + m_FirstNode.layout.center.x) / 2 + m_ThirdNode.layout.center.x + m_FourthNode.layout.center.x) / 3);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.HorizontalCenter);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(expectedCenter, m_FirstNode.layout.center.x);
            Assert.AreNotEqual(expectedCenter, m_SecondNode.layout.center.x);
            Assert.AreEqual(expectedDistanceBetweenFirstSecond, m_SecondNode.layout.xMin - m_FirstNode.layout.xMax);
            Assert.AreEqual(expectedCenter, m_ThirdNode.layout.center.x);
            Assert.AreEqual(expectedCenter, m_FourthNode.layout.center.x);
        }

        [UnityTest]
        public IEnumerator AlignConnectedNodesCenterVertical()
        {
            // Config:
            //  +-----+
            //  |Node1o--+
            //  +-----+  |  +-----+     +-----+
            //  ---------|--oNode3o-----oNode4|------ center vertical
            //  +-----+  |  +-----+     +-----+
            //  |Node2o--+
            //  +-----+
            //

            Vector2 firstNodePos = new Vector2(0, 0);
            Vector2 secondNodePos = new Vector2(0, 300);
            Vector2 thirdNodePos = new Vector2(200, 300);
            Vector2 fourthNodePos = new Vector2(600, 100);

            var actions = CreateConnectedNodes(firstNodePos, secondNodePos, thirdNodePos, fourthNodePos, false);
            while (actions.MoveNext())
            {
                yield return null;
            }

            SelectConnectedNodes();
            yield return null;

            float expectedDistanceBetweenFirstSecond = GraphViewStaticBridge.RoundToPixelGrid(secondNodePos.y - firstNodePos.y);
            float expectedCenter = GraphViewStaticBridge.RoundToPixelGrid(((m_SecondNode.layout.center.y + m_FirstNode.layout.center.y) / 2 + m_ThirdNode.layout.center.y + m_FourthNode.layout.center.y) / 3);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.VerticalCenter);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(expectedCenter, m_FirstNode.layout.center.y);
            Assert.AreNotEqual(expectedCenter, m_SecondNode.layout.center.y);
            Assert.AreEqual(expectedDistanceBetweenFirstSecond, m_SecondNode.layout.y - m_FirstNode.layout.y);
            Assert.AreEqual(expectedCenter, m_ThirdNode.layout.center.y);
            Assert.AreEqual(expectedCenter, m_FourthNode.layout.center.y);
        }

        [UnityTest]
        public IEnumerator AlignConnectedNodesVerticalPorts()
        {
            // Config:
            //                    |
            //  +-----+     +-----+
            //  |Node1|     |Node2|
            //  +--o--+     +--o--+
            //     |           |  |
            //     +-----------+  |
            //                 |  |
            //              +--o--+
            //              |Node3|
            //              +--o--+
            //                 |  |
            //              +--o--+
            //              |Node4|
            //              +--o--+
            //                    | right

            Vector2 firstNodePos = new Vector2(0, 0);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 thirdNodePos = new Vector2(400, 200);
            Vector2 fourthNodePos = new Vector2(600, 400);

            var actions = CreateConnectedNodes(firstNodePos, secondNodePos, thirdNodePos, fourthNodePos, true);
            while (actions.MoveNext())
            {
                yield return null;
            }

            SelectConnectedNodes();
            yield return null;

            float expectedDistanceBetweenFirstSecond = m_SecondNode.layout.xMin - m_FirstNode.layout.xMax;
            float expectedXMax = m_FourthNode.layout.xMax;

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Right);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreNotEqual(expectedXMax, m_FirstNode.layout.xMax);
            Assert.AreEqual(expectedXMax, m_SecondNode.layout.xMax);
            Assert.AreEqual(expectedDistanceBetweenFirstSecond, m_SecondNode.layout.xMin - m_FirstNode.layout.xMax);
            Assert.AreEqual(expectedXMax, m_ThirdNode.layout.xMax);
            Assert.AreEqual(expectedXMax, m_FourthNode.layout.xMax);
        }

        [UnityTest]
        public IEnumerator AlignComplexConnectedNodes()
        {
            // Config
            //           +-------+                      +-------+
            //           | Node2 o--+---------------+---o Node5 |
            //           +-------+  |   +-------+   |   +-------+
            // +-------+ +-------+  +---o Node4 o---+
            // | Node1 o-o Node3 o--+   +-------+
            // +-------+ +-------+

            Vector2 firstNodePos = new Vector2(0, 400);
            Vector2 secondNodePos = new Vector2(200, 10);
            Vector2 thirdNodePos = new Vector2(200, 400);
            Vector2 fourthNodePos = new Vector2(400, 200);
            Vector2 fifthNodePos = new Vector2(600, 0);

            FirstNodeModel = CreateNode("Node1", firstNodePos, 0, 0, 0, 1);
            SecondNodeModel = CreateNode("Node2", secondNodePos, 0, 0, 0, 1);
            ThirdNodeModel = CreateNode("Node3", thirdNodePos, 0, 0, 1, 1);
            FourthNodeModel = CreateNode("Node4", fourthNodePos, 0, 0, 1, 1);
            var fifthNodeModel = CreateNode("Node5", fifthNodePos, 0, 0, 1);

            MarkGraphViewStateDirty();
            yield return null;

            IPortModel outputPortFirstNode = FirstNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(outputPortFirstNode);

            IPortModel outputPortSecondNode = SecondNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(outputPortSecondNode);

            IPortModel inputPortThirdNode = ThirdNodeModel.InputsByDisplayOrder[0];
            IPortModel outputPortThirdNode = ThirdNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortThirdNode);
            Assert.IsNotNull(outputPortThirdNode);

            IPortModel inputPortFourthNode = FourthNodeModel.InputsByDisplayOrder[0];
            IPortModel outputPortFourthNode = FourthNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortFourthNode);
            Assert.IsNotNull(outputPortFourthNode);

            IPortModel inputPortFifthNode = fifthNodeModel.InputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortFifthNode);

            // Connect the ports together
            var actions = ConnectPorts(outputPortFirstNode, inputPortThirdNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortSecondNode, inputPortFourthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortThirdNode, inputPortFourthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortSecondNode, inputPortFifthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortFourthNode, inputPortFifthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(5, GraphModel.EdgeModels.Count, "Not all edges were created.");

            // Get the UI nodes
            m_FirstNode = FirstNodeModel.GetUI<Node>(graphView);
            m_SecondNode = SecondNodeModel.GetUI<Node>(graphView);
            m_ThirdNode = ThirdNodeModel.GetUI<Node>(graphView);
            m_FourthNode = FourthNodeModel.GetUI<Node>(graphView);
            var fifthNode = fifthNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(m_FirstNode);
            Assert.IsNotNull(m_SecondNode);
            Assert.IsNotNull(m_ThirdNode);
            Assert.IsNotNull(m_FourthNode);
            Assert.IsNotNull(fifthNode);

            SelectConnectedNodes();
            yield return null;

            // Select node 5
            Vector2 selectionPosNode5 = graphView.ContentViewContainer.LocalToWorld(fifthNode.layout.position) + k_SelectionOffset;
            actions = SelectElement(selectionPosNode5);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float expectedDistanceBetweenSecondThird = m_ThirdNode.layout.yMin - m_SecondNode.layout.yMax;
            float expectedDistanceBetweenSecondFourth = m_FourthNode.layout.yMin - m_SecondNode.layout.yMax;
            float expectedTop = GraphViewStaticBridge.RoundToPixelGrid(0f);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Top);
            while (actions.MoveNext())
            {
                yield return null;
            }

            fifthNode = fifthNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(fifthNode);

            // Nodes 2, 3 and 4 are part of the same group: they move together
            Assert.AreEqual(expectedTop, m_SecondNode.layout.yMin);
            Assert.AreNotEqual(expectedTop, m_ThirdNode.layout.yMin);
            Assert.AreNotEqual(expectedTop, m_FourthNode.layout.yMin);
            Assert.AreEqual(expectedDistanceBetweenSecondThird, m_ThirdNode.layout.yMin - m_SecondNode.layout.yMax);
            Assert.AreEqual(expectedDistanceBetweenSecondFourth, m_FourthNode.layout.yMin - m_SecondNode.layout.yMax);

            // Node 1 and node 5 move individually
            Assert.AreEqual(expectedTop, m_FirstNode.layout.yMin);
            Assert.AreEqual(expectedTop, fifthNode.layout.yMin);
        }

        [UnityTest]
        public IEnumerator GetNodeDependencies()
        {
            // Config
            //           +-------+                      +-------+
            //           | Node2 o--+---------------+---o Node5 |
            //           +-------+  |   +-------+   |   +-------+
            // +-------+ +-------+  +---o Node4 o---+
            // | Node1 o-o Node3 o--+   +-------+
            // +-------+ +-------+

            Vector2 firstNodePos = new Vector2(0, 400);
            Vector2 secondNodePos = new Vector2(200, 10);
            Vector2 thirdNodePos = new Vector2(200, 400);
            Vector2 fourthNodePos = new Vector2(400, 200);
            Vector2 fifthNodePos = new Vector2(600, 0);

            FirstNodeModel = CreateNode("Node1", firstNodePos, 0, 0, 0, 1);
            SecondNodeModel = CreateNode("Node2", secondNodePos, 0, 0, 0, 1);
            ThirdNodeModel = CreateNode("Node3", thirdNodePos, 0, 0, 1, 1);
            FourthNodeModel = CreateNode("Node4", fourthNodePos, 0, 0, 1, 1);
            var fifthNodeModel = CreateNode("Node5", fifthNodePos, 0, 0, 1);

            MarkGraphViewStateDirty();
            yield return null;

            IPortModel outputPortFirstNode = FirstNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(outputPortFirstNode);

            IPortModel outputPortSecondNode = SecondNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(outputPortSecondNode);

            IPortModel inputPortThirdNode = ThirdNodeModel.InputsByDisplayOrder[0];
            IPortModel outputPortThirdNode = ThirdNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortThirdNode);
            Assert.IsNotNull(outputPortThirdNode);

            IPortModel inputPortFourthNode = FourthNodeModel.InputsByDisplayOrder[0];
            IPortModel outputPortFourthNode = FourthNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortFourthNode);
            Assert.IsNotNull(outputPortFourthNode);

            IPortModel inputPortFifthNode = fifthNodeModel.InputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortFifthNode);

            // Connect the ports together
            var actions = ConnectPorts(outputPortFirstNode, inputPortThirdNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortSecondNode, inputPortFourthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortThirdNode, inputPortFourthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortSecondNode, inputPortFifthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortFourthNode, inputPortFifthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(5, GraphModel.EdgeModels.Count, "Not all edges were created.");

            // Get the UI nodes
            m_FirstNode = FirstNodeModel.GetUI<Node>(graphView);
            m_SecondNode = SecondNodeModel.GetUI<Node>(graphView);
            m_ThirdNode = ThirdNodeModel.GetUI<Node>(graphView);
            m_FourthNode = FourthNodeModel.GetUI<Node>(graphView);
            var fifthNode = fifthNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(m_FirstNode);
            Assert.IsNotNull(m_SecondNode);
            Assert.IsNotNull(m_ThirdNode);
            Assert.IsNotNull(m_FourthNode);
            Assert.IsNotNull(fifthNode);

            SelectConnectedNodes();
            yield return null;

            // Select node 5
            Vector2 selectionPosNode5 = graphView.ContentViewContainer.LocalToWorld(fifthNode.layout.position) + k_SelectionOffset;
            actions = SelectElement(selectionPosNode5);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Top);
            while (actions.MoveNext())
            {
                yield return null;
            }

            fifthNode = fifthNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(fifthNode);

            var nodeDependencies = m_AlignmentHelper.NodeDependencies;

            // There should be two parents
            Assert.AreEqual(2, nodeDependencies.Keys.Count);

            // Node 3 should be parent to Node 1
            Assert.True(nodeDependencies.ContainsKey(ThirdNodeModel));
            Assert.AreEqual(1, nodeDependencies[ThirdNodeModel].Count);
            Assert.True(nodeDependencies[ThirdNodeModel].Contains(FirstNodeModel));
        }

        [UnityTest]
        public IEnumerator NodeIsItsOwnParent()
        {
            // Config   +---------+
            //          |+-------+|                    +-------+
            //          +o Node2 o+-+                +-o Node5 |
            //           +-------+  |   +-------+    | +-------+
            // +-------+ +-------+  +-+-o Node4 o-+--+
            // | Node1 o-o Node3 o--+   +-------+
            // +-------+ +-------+

            Vector2 firstNodePos = new Vector2(0, 400);
            Vector2 secondNodePos = new Vector2(200, 0);
            Vector2 thirdNodePos = new Vector2(200, 400);
            Vector2 fourthNodePos = new Vector2(400, 200);
            Vector2 fifthNodePos = new Vector2(600, 10);

            FirstNodeModel = CreateNode("Node1", firstNodePos, 0, 0, 0, 1);
            SecondNodeModel = CreateNode("Node2", secondNodePos, 0, 0, 1, 1);
            ThirdNodeModel = CreateNode("Node3", thirdNodePos, 0, 0, 1, 1);
            FourthNodeModel = CreateNode("Node4", fourthNodePos, 0, 0, 1, 1);
            var fifthNodeModel = CreateNode("Node5", fifthNodePos, 0, 0, 1);

            MarkGraphViewStateDirty();
            yield return null;

            IPortModel outputPortFirstNode = FirstNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(outputPortFirstNode);

            IPortModel inputPortSecondNode = SecondNodeModel.InputsByDisplayOrder[0];
            IPortModel outputPortSecondNode = SecondNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortSecondNode);
            Assert.IsNotNull(outputPortSecondNode);

            IPortModel inputPortThirdNode = ThirdNodeModel.InputsByDisplayOrder[0];
            IPortModel outputPortThirdNode = ThirdNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortThirdNode);
            Assert.IsNotNull(outputPortThirdNode);

            IPortModel inputPortFourthNode = FourthNodeModel.InputsByDisplayOrder[0];
            IPortModel outputPortFourthNode = FourthNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortFourthNode);
            Assert.IsNotNull(outputPortFourthNode);

            IPortModel inputPortFifthNode = fifthNodeModel.InputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortFifthNode);

            // Connect the ports together
            var actions = ConnectPorts(outputPortFirstNode, inputPortThirdNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Connect to itself
            actions = ConnectPorts(outputPortSecondNode, inputPortSecondNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortSecondNode, inputPortFourthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortThirdNode, inputPortFourthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortFourthNode, inputPortFifthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(5, GraphModel.EdgeModels.Count, "Not all edges were created.");

            // Get the UI nodes
            m_FirstNode = FirstNodeModel.GetUI<Node>(graphView);
            m_SecondNode = SecondNodeModel.GetUI<Node>(graphView);
            m_ThirdNode = ThirdNodeModel.GetUI<Node>(graphView);
            m_FourthNode = FourthNodeModel.GetUI<Node>(graphView);
            var fifthNode = fifthNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(m_FirstNode);
            Assert.IsNotNull(m_SecondNode);
            Assert.IsNotNull(m_ThirdNode);
            Assert.IsNotNull(m_FourthNode);
            Assert.IsNotNull(fifthNode);

            SelectConnectedNodes();
            yield return null;

            // Select node 5
            Vector2 selectionPosNode5 = graphView.ContentViewContainer.LocalToWorld(fifthNode.layout.position) + k_SelectionOffset;
            actions = SelectElement(selectionPosNode5);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float expectedDistanceBetweenSecondThird = m_ThirdNode.layout.yMin - m_SecondNode.layout.yMax;
            float expectedTop = GraphViewStaticBridge.RoundToPixelGrid(0f);

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Top);
            while (actions.MoveNext())
            {
                yield return null;
            }

            fifthNode = fifthNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(fifthNode);

            // Nodes 2, 3 are part of the same group: they move together
            Assert.AreEqual(expectedTop, m_SecondNode.layout.yMin);
            Assert.AreNotEqual(expectedTop, m_ThirdNode.layout.yMin);
            Assert.AreEqual(expectedDistanceBetweenSecondThird, m_ThirdNode.layout.yMin - m_SecondNode.layout.yMax);

            // Nodes 1, 4 and 5 move individually
            Assert.AreEqual(expectedTop, m_FirstNode.layout.yMin);
            Assert.AreEqual(expectedTop, m_FourthNode.layout.yMin);
            Assert.AreEqual(expectedTop, fifthNode.layout.yMin);

            var nodeDependencies = m_AlignmentHelper.NodeDependencies;

            // There should be 3 parents
            Assert.AreEqual(3, nodeDependencies.Keys.Count);

            // Node 2 should not be considered a parent even if it is its own parent
            Assert.False(nodeDependencies.ContainsKey(SecondNodeModel));

            // Node 3 should be parent to Node 1
            Assert.True(nodeDependencies.ContainsKey(ThirdNodeModel));
            Assert.AreEqual(1, nodeDependencies[ThirdNodeModel].Count);
            Assert.True(nodeDependencies[ThirdNodeModel].Contains(FirstNodeModel));

            // Node 4 should be parent to Nodes 2,3
            Assert.True(nodeDependencies.ContainsKey(FourthNodeModel));
            Assert.AreEqual(2, nodeDependencies[FourthNodeModel].Count);
            Assert.True(nodeDependencies[FourthNodeModel].Contains(SecondNodeModel));
            Assert.True(nodeDependencies[FourthNodeModel].Contains(ThirdNodeModel));

            // Node 5 should be parent to Node 4
            Assert.True(nodeDependencies.ContainsKey(fifthNodeModel));
            Assert.AreEqual(1, nodeDependencies[fifthNodeModel].Count);
            Assert.True(nodeDependencies[fifthNodeModel].Contains(FourthNodeModel));
        }
    }
}
