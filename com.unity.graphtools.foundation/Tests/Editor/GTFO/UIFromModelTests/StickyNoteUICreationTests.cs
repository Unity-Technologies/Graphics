using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class StickyNoteUICreationTests : GtfTestFixture
    {
        [Test]
        public void StickyNoteHasExpectedParts()
        {
            var stickyNoteModel = GraphTool.ToolState.GraphModel.CreateStickyNote(Rect.zero);
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, GraphView);

            Assert.IsNotNull(stickyNote.SafeQ<VisualElement>(StickyNote.titleContainerPartName));
            Assert.IsNotNull(stickyNote.SafeQ<VisualElement>(StickyNote.contentContainerPartName));
            Assert.IsNotNull(stickyNote.SafeQ<VisualElement>(StickyNote.resizerPartName));
        }
    }
}
