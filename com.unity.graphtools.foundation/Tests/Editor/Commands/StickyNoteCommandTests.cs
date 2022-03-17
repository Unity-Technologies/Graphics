using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    [Category("Sticky Notes")]
    [Category("Command")]
    class StickyNoteCommandTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        static readonly Rect k_StickyNoteRect = new Rect(Vector2.zero, new Vector2(100, 100));
        static readonly Rect k_StickyNote2Rect = new Rect(Vector2.right * 100, new Vector2(50, 50));

        [Test]
        public void Test_CreateStickyNoteCommand([Values] TestingMode mode)
        {
            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(0));
                    return new CreateStickyNoteCommand(k_StickyNoteRect);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.IsTrue(string.IsNullOrEmpty(GetStickyNote(0).Contents));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(k_StickyNoteRect));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    return new CreateStickyNoteCommand(k_StickyNote2Rect);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(2));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(k_StickyNoteRect));
                    Assert.That(GetStickyNote(1).PositionAndSize, Is.EqualTo(k_StickyNote2Rect));
                });
        }

        [Test]
        public void Test_ResizeStickyNoteCommand([Values] TestingMode mode)
        {
            GraphModel.CreateStickyNote(k_StickyNoteRect);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    IStickyNoteModel stickyNote = GetStickyNote(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(stickyNote.PositionAndSize, Is.EqualTo(k_StickyNoteRect));
                    return new ChangeElementLayoutCommand(stickyNote, k_StickyNote2Rect);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(k_StickyNote2Rect));
                });
        }

        [Test]
        public void Test_UpdateStickyNoteCommand([Values] TestingMode mode)
        {
            var stickyNote = GraphModel.CreateStickyNote(k_StickyNoteRect);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(string.IsNullOrEmpty(GetStickyNote(0).Title));
                    Assert.IsTrue(string.IsNullOrEmpty(GetStickyNote(0).Contents));
                    return new UpdateStickyNoteCommand(stickyNote, "stickyNote2", "This is a note");
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).Title, Is.EqualTo("stickyNote2"));
                    Assert.That(GetStickyNote(0).Contents, Is.EqualTo("This is a note"));
                });
        }

        [Test]
        public void Test_UpdateStickyNoteThemeCommand([Values] TestingMode mode)
        {
            var stickyNote = GraphModel.CreateStickyNote(k_StickyNoteRect);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).Theme, Is.EqualTo(StickyNoteColorTheme.Classic.ToString()));
                    return new UpdateStickyNoteThemeCommand(StickyNoteColorTheme.Teal.ToString(), stickyNote);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).Theme, Is.EqualTo(StickyNoteColorTheme.Teal.ToString()));
                });
        }

        [Test]
        public void Test_UpdateStickyNoteTextSizeCommand([Values] TestingMode mode)
        {
            var stickyNote = GraphModel.CreateStickyNote(k_StickyNoteRect);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).TextSize, Is.EqualTo(StickyNoteTextSize.Small.ToString()));
                    return new UpdateStickyNoteTextSizeCommand(StickyNoteTextSize.Huge.ToString(), new[] { stickyNote });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).TextSize, Is.EqualTo(StickyNoteTextSize.Huge.ToString()));
                });
        }
    }
}
