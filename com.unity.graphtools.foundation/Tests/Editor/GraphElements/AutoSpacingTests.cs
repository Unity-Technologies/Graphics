using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class AutoSpacingTests : AutoPlacementTestHelper
    {
        AutoSpacingHelper m_AutoSpacingHelper;

        IEnumerator SpaceElements(PortOrientation orientation)
        {
            m_AutoSpacingHelper.SendSpacingCommand(orientation);
            yield return null;

            // Get the UI elements
            m_FirstNode = FirstNodeModel.GetView<Node>(GraphView);
            m_SecondNode = SecondNodeModel.GetView<Node>(GraphView);
            m_ThirdNode = ThirdNodeModel.GetView<Node>(GraphView);
            m_FourthNode = FourthNodeModel.GetView<Node>(GraphView);
            m_Placemat = PlacematModel.GetView<Placemat>(GraphView);
            m_StickyNote = StickyNoteModel.GetView<StickyNote>(GraphView);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_AutoSpacingHelper = new AutoSpacingHelper(GraphView);
        }

        [UnityTest]
        public IEnumerator SpaceElementsHorizontally()
        {
            // Config:
            //          +-----+
            //          |Node2| +-----+
            //  +-----+ +-----+ |stick|
            //  |Node1|         +-----+ +-----+
            //  +-----+                 |place|
            //                          +-----+

            GraphViewSettings.UserSettings.SpacingMarginValue = 10f;
            float expectedSpacingMargin = GraphViewStaticBridge.RoundToPixelGrid(GraphViewSettings.UserSettings.SpacingMarginValue);

            Vector2 firstNodePos = new Vector2(50, 200);
            Vector2 secondNodePos = new Vector2(200, 0);
            Vector2 stickyNotePos = new Vector2(350, 100);
            Vector2 placematPos = new Vector2(600, 300);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = SpaceElements(PortOrientation.Horizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float marginBetweenFirstNodeSecondNode = m_SecondNode.layout.xMin - m_FirstNode.layout.xMax;
            float marginBetweenSecondNodeStickyNote = m_StickyNote.layout.xMin - m_SecondNode.layout.xMax;
            float marginBetweenStickyNotePlacemat = m_Placemat.layout.xMin - m_StickyNote.layout.xMax;

            Assert.AreEqual(expectedSpacingMargin, marginBetweenFirstNodeSecondNode);
            Assert.AreEqual(expectedSpacingMargin, marginBetweenSecondNodeStickyNote);
            Assert.AreEqual(expectedSpacingMargin, marginBetweenStickyNotePlacemat);
        }

        [UnityTest]
        public IEnumerator SpaceElementsVertically()
        {
            // Config:
            //  +-----+
            //  |Node1|
            //  +-----+
            //       +-----+
            //       |Node2|
            //       +-----+
            //            +-----+
            //            |stick|
            //            +-----+
            //                   +-----+
            //                   |place|
            //                   +-----+

            GraphViewSettings.UserSettings.SpacingMarginValue = 0f;
            float expectedSpacingMargin = GraphViewStaticBridge.RoundToPixelGrid(GraphViewSettings.UserSettings.SpacingMarginValue);

            Vector2 firstNodePos = new Vector2(50, 0);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 stickyNotePos = new Vector2(350, 350);
            Vector2 placematPos = new Vector2(600, 450);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = SpaceElements(PortOrientation.Vertical);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float marginBetweenFirstNodeSecondNode = m_SecondNode.layout.yMin - m_FirstNode.layout.yMax;
            float marginBetweenSecondNodeStickyNote = m_StickyNote.layout.yMin - m_SecondNode.layout.yMax;
            float marginBetweenStickyNotePlacemat = m_Placemat.layout.yMin - m_StickyNote.layout.yMax;

            Assert.AreEqual(expectedSpacingMargin, marginBetweenFirstNodeSecondNode);
            Assert.AreEqual(expectedSpacingMargin, marginBetweenSecondNodeStickyNote);
            Assert.AreEqual(expectedSpacingMargin, marginBetweenStickyNotePlacemat);
        }

        [UnityTest]
        public IEnumerator SpaceOnlySelectedElements()
        {
            // Config:
            //          +-----+
            //          |Node2| +-----+
            //  +-----+ +-----+ |stick|
            //  |Node1|         +-----+ +-----+
            //  +-----+                 |place|
            //                          +-----+

            GraphViewSettings.UserSettings.SpacingMarginValue = 10f;

            float expectedSpacingMargin = GraphViewStaticBridge.RoundToPixelGrid(GraphViewSettings.UserSettings.SpacingMarginValue);

            Vector2 firstNodePos = new Vector2(50, 200);
            Vector2 secondNodePos = new Vector2(200, 0);
            Vector2 stickyNotePos = new Vector2(350, 100);
            Vector2 placematPos = new Vector2(600, 300);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 selectionPosStickyNote = GraphView.ContentViewContainer.LocalToWorld(m_StickyNote.layout.position) + k_SelectionOffset;

            // Unselect StickyNote
            actions = SelectElement(selectionPosStickyNote);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = SpaceElements(PortOrientation.Horizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float marginBetweenFirstNodeSecondNode = m_SecondNode.layout.xMin - m_FirstNode.layout.xMax;
            float marginBetweenSecondNodePlacemat = m_Placemat.layout.xMin - m_SecondNode.layout.xMax;

            Assert.AreEqual(expectedSpacingMargin, marginBetweenFirstNodeSecondNode);
            Assert.AreEqual(expectedSpacingMargin, marginBetweenSecondNodePlacemat);
            // Sticky note was not selected, it doesn't move
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(stickyNotePos.x), m_StickyNote.layout.x);
        }

        [UnityTest]
        public IEnumerator SpaceWithPlacemat()
        {
            // Config:
            //                +------+
            //  +-----+  +-----+place|
            //  |Node1|  |Node2|-----+  +-----+
            //  +-----+  +-----+        |stick|
            //                          +-----+

            GraphViewSettings.UserSettings.SpacingMarginValue = 10f;
            float expectedSpacingMargin = GraphViewStaticBridge.RoundToPixelGrid(GraphViewSettings.UserSettings.SpacingMarginValue);

            Vector2 firstNodePos = new Vector2(0, 200);
            Vector2 secondNodePos = new Vector2(200, 300);
            Vector2 placematPos = new Vector2(350, 250);
            Vector2 stickyNotePos = new Vector2(600, 100);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = SpaceElements(PortOrientation.Horizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float marginBetweenFirstNodeSecondNode = m_SecondNode.layout.xMin - m_FirstNode.layout.xMax;
            float marginBetweenPlacematStickyNote = m_StickyNote.layout.xMin - m_Placemat.layout.xMax;

            Assert.AreEqual(expectedSpacingMargin, marginBetweenFirstNodeSecondNode);
            Assert.True(m_SecondNode.layout.Overlaps(m_Placemat.layout));
            Assert.AreEqual(expectedSpacingMargin, marginBetweenPlacematStickyNote);
        }

        [UnityTest]
        public IEnumerator SpaceConnectedNodes()
        {
            // Config:
            //  +-----+
            //  |Node1o--+
            //  +-----+  |  +-----+     +-----+
            //           |--oNode3o-----oNode4|
            //  +-----+  |  +-----+     +-----+
            //  |Node2o--+
            //  +-----+
            //

            GraphViewSettings.UserSettings.SpacingMarginValue = 10f;
            float expectedSpacingMargin = GraphViewStaticBridge.RoundToPixelGrid(GraphViewSettings.UserSettings.SpacingMarginValue);

            Vector2 firstNodePos = new Vector2(0, 50);
            Vector2 secondNodePos = new Vector2(0, 350);
            Vector2 thirdNodePos = new Vector2(300, 300);
            Vector2 fourthNodePos = new Vector2(600, 100);


            var actions = CreateConnectedNodes(firstNodePos, secondNodePos, thirdNodePos, fourthNodePos, false);
            while (actions.MoveNext())
            {
                yield return null;
            }

            SelectConnectedNodes();
            yield return null;

            actions = SpaceElements(PortOrientation.Horizontal);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float marginBetweenFirstSecondNodes = m_SecondNode.layout.xMin - m_FirstNode.layout.xMax;
            float marginBetweenSecondThirdNodes = m_ThirdNode.layout.xMin - m_SecondNode.layout.xMax;
            float marginBetweenThirdFourthNodes = m_FourthNode.layout.xMin - m_ThirdNode.layout.xMax;

            Assert.AreEqual(expectedSpacingMargin, marginBetweenFirstSecondNodes);
            Assert.AreEqual(expectedSpacingMargin, marginBetweenSecondThirdNodes);
            Assert.AreEqual(expectedSpacingMargin, marginBetweenThirdFourthNodes);
        }
    }
}
