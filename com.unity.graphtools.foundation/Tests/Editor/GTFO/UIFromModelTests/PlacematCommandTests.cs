using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PlacematCommandTests : GtfTestFixture
    {
        [UnityTest]
        public IEnumerator PlacematCollapsesOnValueChanged()
        {
            var placematModel = GraphModel.CreatePlacemat(Rect.zero);
            MarkGraphViewStateDirty();
            yield return null;

            var placemat = placematModel.GetUI<GraphElement>(GraphView);
            var collapseButton = placemat.SafeQ<CollapseButton>(Placemat.collapseButtonPartName);
            Assert.IsNotNull(collapseButton);
            Assert.IsFalse(collapseButton.value);
            Assert.IsFalse(placematModel.Collapsed);

            collapseButton.value = true;
            yield return null;

            Assert.IsTrue(placematModel.Collapsed);
        }

        [UnityTest]
        public IEnumerator PlacematCollapsesOnClick()
        {
            var placematModel = GraphModel.CreatePlacemat(Rect.zero);
            MarkGraphViewStateDirty();
            yield return null;

            var placemat = placematModel.GetUI<GraphElement>(GraphView);
            var collapseButton = placemat.SafeQ<CollapseButton>(Placemat.collapseButtonPartName);
            Assert.IsNotNull(collapseButton);
            Assert.IsFalse(collapseButton.value);
            Assert.IsFalse(placematModel.Collapsed);

            var clickPosition = collapseButton.parent.LocalToWorld(collapseButton.layout.center);

            // Move the mouse over to make the button appear.
            Helpers.MouseMoveEvent(new Vector2(0, 0), clickPosition);
            Helpers.MouseMoveEvent(clickPosition, clickPosition + Vector2.down);

            Helpers.Click(clickPosition);
            yield return null;

            Assert.IsTrue(placematModel.Collapsed);
        }

        [UnityTest]
        public IEnumerator RenamePlacematRenamesModel()
        {
            const string newName = "New Name";

            var placematModel = GraphModel.CreatePlacemat(Rect.zero);
            placematModel.Title = "Placemat";
            MarkGraphViewStateDirty();
            yield return null;

            var placemat = placematModel.GetUI<GraphElement>(GraphView);
            var label = placemat.SafeQ(Placemat.titleContainerPartName).SafeQ(EditableLabel.labelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            Helpers.Click(clickPosition, clickCount: 2);

            Helpers.Type(newName);

            Helpers.Click(GraphView.layout.max);
            yield return null;

            Assert.AreEqual(newName, placematModel.Title);
        }

        [UnityTest]
        public IEnumerator ResizePlacematChangeModelRect()
        {
            var originalRect = new Rect(0, 0, 200, 100);
            var move = new Vector2(100, 0);

            var placematModel = GraphModel.CreatePlacemat(originalRect);
            placematModel.Title = "Placemat";
            MarkGraphViewStateDirty();
            yield return null;

            var placemat = placematModel.GetUI<GraphElement>(GraphView);
            var rightResizer = placemat.SafeQ(Placemat.resizerPartName).SafeQ("right-resize");
            var clickPosition = rightResizer.parent.LocalToWorld(rightResizer.layout.center);
            Helpers.DragTo(clickPosition, clickPosition + move);
            yield return null;

            var newRect = new Rect(originalRect.position, originalRect.size + move);
            Assert.AreEqual(newRect, placematModel.PositionAndSize);
        }
    }
}
