using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class ShortcutSendsCommandTests : GraphViewTester
    {
        const int k_NodeCount = 4;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            for (int i = 0; i < k_NodeCount; ++i)
            {
                CreateNode("", new Vector2(10 + 50 * i, 30 * i), 2, 1);
            }
        }

        [UnityTest]
        public IEnumerator ShortcutFrameAllSendsCorrectCommand()
        {
            bool commandReceived = false;
            void CommandHandler(ReframeGraphViewCommand command)
            {
                commandReceived = true;
                Assert.AreEqual(0, command.NewSelection?.Count ?? 0);
                Assert.AreNotEqual(Vector3.zero, command.Position);
                Assert.AreNotEqual(Vector3.one, command.Scale);
            }

            GraphView.RegisterCommandHandler<ReframeGraphViewCommand>(CommandHandler);

            ShortcutFrameAllEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(commandReceived);
        }

        [UnityTest]
        public IEnumerator ShortcutFrameOriginSendsCorrectCommand()
        {
            bool commandReceived = false;
            void CommandHandler(ReframeGraphViewCommand command)
            {
                commandReceived = true;
                Assert.AreEqual(0, command.NewSelection?.Count ?? 0);
                Assert.AreEqual(Vector3.zero, command.Position);
                Assert.AreEqual(Vector3.one, command.Scale);
            }

            GraphView.RegisterCommandHandler<ReframeGraphViewCommand>(CommandHandler);

            ShortcutFrameOriginEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(commandReceived);
        }

        [UnityTest]
        public IEnumerator ShortcutFramePreviousSendsCorrectCommand()
        {
            bool commandReceived = false;
            var elemList = GraphView.GraphModel.NodeModels.Where(e => e.IsSelectable()).ToList();

            void CommandHandler(ReframeGraphViewCommand command)
            {
                commandReceived = true;
                Assert.AreEqual(1, command.NewSelection.Count);
                Assert.AreEqual(elemList[k_NodeCount - 2], command.NewSelection[0]);
            }

            GraphView.RegisterCommandHandler<ReframeGraphViewCommand>(CommandHandler);

            // we need the UI
            MarkGraphViewStateDirty();
            yield return null;

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, elemList[k_NodeCount - 1]));

            ShortcutFramePreviousEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(commandReceived);
        }

        [UnityTest]
        public IEnumerator ShortcutFrameNextSendsCorrectCommand()
        {
            bool commandReceived = false;
            var elemList = GraphView.GraphModel.NodeModels.Where(e => e.IsSelectable()).ToList();
            void CommandHandler(ReframeGraphViewCommand command)
            {
                commandReceived = true;
                Assert.AreEqual(1, command.NewSelection.Count);
                Assert.AreEqual(elemList[1], command.NewSelection[0]);
            }

            GraphView.RegisterCommandHandler<ReframeGraphViewCommand>(CommandHandler);

            // we need the UI
            MarkGraphViewStateDirty();
            yield return null;

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, elemList[0]));

            ShortcutFrameNextEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(commandReceived);
        }

        [UnityTest]
        public IEnumerator ShortcutDeleteSendsCorrectCommand()
        {
            bool commandReceived = false;
            var elemList = GraphView.GraphModel.NodeModels.Where(e => e.IsSelectable()).ToList();
            void CommandHandler(DeleteElementsCommand command)
            {
                commandReceived = true;
                Assert.AreEqual(1, command.Models.Count);
                Assert.AreEqual(elemList[0], command.Models[0]);
            }

            GraphView.RegisterCommandHandler<DeleteElementsCommand>(CommandHandler);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, elemList[0]));

            ShortcutDeleteEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(commandReceived);
        }

        [UnityTest]
        public IEnumerator ShortcutConvertConstantToVariableSendsCorrectCommand()
        {
            bool commandReceived = false;
            var constantNode = GraphModel.CreateConstantNode(TypeHandle.Float, "blah", Vector2.zero);

            void CommandHandler(ConvertConstantNodesAndVariableNodesCommand command)
            {
                commandReceived = true;
                Assert.AreEqual(1, command.ConstantNodeModels.Count);
                Assert.AreEqual(0, command.VariableNodeModels.Count);
                Assert.AreEqual(constantNode, command.ConstantNodeModels[0]);
            }

            GraphView.RegisterCommandHandler<ConvertConstantNodesAndVariableNodesCommand>(CommandHandler);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, constantNode));

            ShortcutConvertConstantAndVariableEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(commandReceived);
        }

        [UnityTest]
        public IEnumerator ShortcutAlignNodesSendsCorrectCommand()
        {
            bool commandReceived = false;
            void CommandHandler(AlignNodesCommand command)
            {
                commandReceived = true;
                Assert.IsFalse(command.Follow);
            }

            GraphView.RegisterCommandHandler<AlignNodesCommand>(CommandHandler);

            ShortcutAlignNodesEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(commandReceived);
        }

        [UnityTest]
        public IEnumerator ShortcutAlignNodeHierarchiesSendsCorrectCommand()
        {
            bool commandReceived = false;
            void CommandHandler(AlignNodesCommand command)
            {
                commandReceived = true;
                Assert.IsTrue(command.Follow);
            }

            GraphView.RegisterCommandHandler<AlignNodesCommand>(CommandHandler);

            ShortcutAlignNodeHierarchiesEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(commandReceived);
        }

        [UnityTest]
        public IEnumerator ShortcutCreateStickyNoteSendsCorrectCommand()
        {
            bool commandReceived = false;
            void CommandHandler(CreateStickyNoteCommand command)
            {
                commandReceived = true;
            }

            GraphView.RegisterCommandHandler<CreateStickyNoteCommand>(CommandHandler);

            ShortcutCreateStickyNoteEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(commandReceived);
        }
    }
}
