using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class StickyNoteCommandTests : GtfTestFixture
    {
        [UnityTest]
        public IEnumerator RenameStickyNoteRenamesModel()
        {
            const string newName = "New Name";

            var stickyNoteModel = GraphModel.CreateStickyNote(Rect.zero);
            stickyNoteModel.Title = "My Note";
            MarkGraphViewStateDirty();
            yield return null;

            var stickyNote = stickyNoteModel.GetUI<GraphElement>(GraphView);
            var label = stickyNote.SafeQ(StickyNote.titleContainerPartName).SafeQ(EditableLabel.labelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            Helpers.Click(clickPosition, clickCount: 2);

            Helpers.Type(newName);

            // Commit the changes by clicking outside the field.
            Helpers.Click(GraphView.layout.min);

            yield return null;

            Assert.AreEqual(newName, stickyNoteModel.Title);
        }

        [UnityTest]
        public IEnumerator ChangeStickyNoteContentUpdatesModel()
        {
            const string newContent = "New Content";

            var stickyNoteModel = GraphModel.CreateStickyNote(new Rect(0, 0, 200, 200));
            stickyNoteModel.Title = "My Note";
            stickyNoteModel.Contents = "Old Content";
            MarkGraphViewStateDirty();
            yield return null;

            var stickyNote = stickyNoteModel.GetUI<GraphElement>(GraphView);
            var label = stickyNote.SafeQ(StickyNote.contentContainerPartName).SafeQ(EditableLabel.labelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            Helpers.Click(clickPosition, clickCount: 2);

            Helpers.Type(newContent);

            // Commit the changes by clicking outside the field.
            Helpers.Click(GraphView.layout.min);

            yield return null;

            Assert.AreEqual(newContent, stickyNoteModel.Contents);
        }

        [UnityTest]
        public IEnumerator ResizeStickyNoteChangeModelRect()
        {
            var originalRect = new Rect(0, 0, 100, 100);
            var move = new Vector2(100, 0);

            var stickyNoteModel = GraphModel.CreateStickyNote(originalRect);
            stickyNoteModel.Title = "Placemat";
            stickyNoteModel.PositionAndSize = originalRect;
            MarkGraphViewStateDirty();
            yield return null;

            var stickyNote = stickyNoteModel.GetUI<GraphElement>(GraphView);
            var rightResizer = stickyNote.SafeQ(Placemat.resizerPartName).SafeQ("right-resize");
            var clickPosition = rightResizer.parent.LocalToWorld(rightResizer.layout.center);
            Helpers.DragTo(clickPosition, clickPosition + move);
            yield return null;

            var newRect = new Rect(originalRect.position, originalRect.size + move);
            Assert.AreEqual(newRect, stickyNoteModel.PositionAndSize);
        }
    }
}
