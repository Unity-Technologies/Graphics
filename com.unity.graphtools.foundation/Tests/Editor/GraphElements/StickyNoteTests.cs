using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class StickyNoteTests : GraphViewTester
    {
        [Test]
        public void StickyNoteCreateAndCheckInitialLayout()
        {
            var database = new GraphElementSearcherDatabase((Stencil)GraphModel.Stencil, GraphModel);
            database.AddStickyNote();

            var graphNodeModelSearcherItem = (GraphNodeModelSearcherItem)database.Items.First();

            GraphView.Dispatch(CreateNodeCommand.OnGraph(graphNodeModelSearcherItem, new Vector2(100, 200)));

            Assert.AreEqual(1, GraphModel.StickyNoteModels.Count);

            var stickyNote = GraphModel.StickyNoteModels.First();

            Assert.AreEqual(new Vector2(100, 200), stickyNote.Position);

            Assert.AreEqual(StickyNote.defaultSize, stickyNote.PositionAndSize.size);
        }

        [UnityTest]
        public IEnumerator StickyNoteUpdateContentsAndCheckUI()
        {
            var database = new GraphElementSearcherDatabase((Stencil)GraphModel.Stencil, GraphModel);
            database.AddStickyNote();

            var graphNodeModelSearcherItem = (GraphNodeModelSearcherItem)database.Items.First();

            GraphView.Dispatch(CreateNodeCommand.OnGraph(graphNodeModelSearcherItem, new Vector2(100, 200)));

            var stickyNote = GraphModel.StickyNoteModels.First();

            GraphView.Dispatch(new UpdateStickyNoteCommand(stickyNote, "Title", "Contents"));

            Assert.AreEqual("Title", stickyNote.Title);
            Assert.AreEqual("Contents", stickyNote.Contents);

            GraphView.RebuildUI();

            yield return null; // waiting for styling

            var stickyNoteUI = stickyNote.GetView<StickyNote>(GraphView);

            Assert.AreEqual(new Vector2(100, 200), new Vector2(stickyNoteUI.resolvedStyle.left, stickyNoteUI.resolvedStyle.top));

            Assert.AreEqual(StickyNote.defaultSize, new Vector2(stickyNoteUI.resolvedStyle.width, stickyNoteUI.resolvedStyle.height));

            var titlePart = (EditableTitlePart)(stickyNoteUI.PartList.GetPart(StickyNote.titleContainerPartName));

            var label = titlePart.TitleLabel.Q<Label>(EditableLabel.labelName);

            Assert.AreEqual("Title", label.text);

            var contentsPart = (StickyNoteContentPart)(stickyNoteUI.PartList.GetPart(StickyNote.contentContainerPartName));

            var contentsLabel = contentsPart.Root.Q<Label>(EditableLabel.labelName);

            Assert.AreEqual("Contents", contentsLabel.text);
        }

        [UnityTest]
        public IEnumerator StickyNoteChangeLayoutAndCheckUI()
        {
            var database = new GraphElementSearcherDatabase((Stencil)GraphModel.Stencil, GraphModel);
            database.AddStickyNote();

            var graphNodeModelSearcherItem = (GraphNodeModelSearcherItem)database.Items.First();

            GraphView.Dispatch(CreateNodeCommand.OnGraph(graphNodeModelSearcherItem, new Vector2(100, 200)));

            var stickyNote = GraphModel.StickyNoteModels.First();

            GraphView.Dispatch(new UpdateStickyNoteCommand(stickyNote, "Title", "Contents"));

            GraphView.Dispatch(new ChangeElementLayoutCommand(stickyNote, new Rect(1, 2, 400, 500)));

            Assert.AreEqual(new Vector2(1, 2), stickyNote.Position);

            Assert.AreEqual(new Vector2(400, 500), stickyNote.PositionAndSize.size);

            GraphView.RebuildUI();

            yield return null; // waiting for styling
            var stickyNoteUI = stickyNote.GetView<StickyNote>(GraphView);

            Assert.AreEqual(new Vector2(1, 2), new Vector2(stickyNoteUI.resolvedStyle.left, stickyNoteUI.resolvedStyle.top));

            Assert.AreEqual(new Vector2(400, 500), new Vector2(stickyNoteUI.resolvedStyle.width, stickyNoteUI.resolvedStyle.height));
        }

        [UnityTest]
        public IEnumerator StickyNoteChangeThemeAndCheckUI()
        {
            GraphView.Dispatch(new CreateStickyNoteCommand(new Rect(10, 20, 100, 200)));

            Assert.AreEqual(1, GraphModel.StickyNoteModels.Count);

            var stickyNote = GraphModel.StickyNoteModels.First();

            GraphView.RebuildUI();

            yield return null; // waiting for styling

            var stickyNoteUI = stickyNote.GetView<StickyNote>(GraphView);

            var originalColor = stickyNoteUI.resolvedStyle.backgroundColor;

            GraphView.Dispatch(new UpdateStickyNoteThemeCommand(StickyNoteColorTheme.Black.ToString(), stickyNote));

            yield return null; // waiting for styling

            Assert.AreNotEqual(originalColor, stickyNoteUI.resolvedStyle.backgroundColor);

            var titlePart = (EditableTitlePart)(stickyNoteUI.PartList.GetPart(StickyNote.titleContainerPartName));

            var label = titlePart.TitleLabel.Q<Label>(EditableLabel.labelName);

            var originalTitleSize = label.resolvedStyle.fontSize;

            var contentsPart = (StickyNoteContentPart)(stickyNoteUI.PartList.GetPart(StickyNote.contentContainerPartName));

            var contentsLabel = contentsPart.Root.Q<Label>(EditableLabel.labelName);

            var originalContentsSize = contentsLabel.resolvedStyle.fontSize;

            GraphView.Dispatch(new UpdateStickyNoteTextSizeCommand(StickyNoteTextSize.Huge.ToString(), stickyNote));

            yield return null; // waiting for styling

            Assert.AreNotEqual(originalTitleSize, label.resolvedStyle.fontSize);
            Assert.AreNotEqual(originalContentsSize, contentsLabel.resolvedStyle.fontSize);
        }
    }
}
