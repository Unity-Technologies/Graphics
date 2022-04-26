using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class CommandThatMarksNew : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphModelStateComponent graphModelState, CommandThatMarksNew command)
        {
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var placematModel = graphModelState.GraphModel.CreatePlacemat(Rect.zero);
                graphUpdater.MarkNew(placematModel);
            }
        }
    }

    class CommandThatMarksChanged : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphModelStateComponent graphModelState, CommandThatMarksChanged command)
        {
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var placemat = graphModelState.GraphModel.PlacematModels.FirstOrDefault();
                Debug.Assert(placemat != null);
                graphUpdater.MarkChanged(placemat);
            }
        }
    }

    class CommandThatMarksDeleted : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphModelStateComponent graphModelState, CommandThatMarksDeleted command)
        {
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var placemat = graphModelState.GraphModel.PlacematModels.FirstOrDefault();
                graphModelState.GraphModel.DeletePlacemats(new[] { placemat });
                Debug.Assert(placemat != null);
                graphUpdater.MarkDeleted(placemat);
            }
        }
    }

    class CommandThatRebuildsAll : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphModelStateComponent graphModelState, CommandThatRebuildsAll command)
        {
            using (var updater = graphModelState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }
        }
    }

    class CommandThatDoesNothing : UndoableCommand
    {
        public static void DefaultCommandHandler(CommandThatDoesNothing command)
        {
        }
    }

    class GraphViewStateObserver : StateObserver
    {
        GraphModelStateComponent m_GraphModelStateComponent;

        public UpdateType UpdateType { get; set; }

        /// <inheritdoc />
        public GraphViewStateObserver(GraphModelStateComponent graphModelStateComponent)
            : base(graphModelStateComponent)
        {
            m_GraphModelStateComponent = graphModelStateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var observation = this.ObserveState(m_GraphModelStateComponent))
                UpdateType = observation.UpdateType;
        }
    }

    [SuppressMessage("ReSharper", "ConvertToLocalFunction")]
    class UIPerformanceTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        GraphViewStateObserver m_GraphViewStateObserver;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            GraphView.Dispatcher.RegisterCommandHandler<GraphModelStateComponent, CommandThatMarksNew>(CommandThatMarksNew.DefaultCommandHandler, GraphView.GraphViewModel.GraphModelState);
            GraphView.Dispatcher.RegisterCommandHandler<GraphModelStateComponent, CommandThatMarksChanged>(CommandThatMarksChanged.DefaultCommandHandler, GraphView.GraphViewModel.GraphModelState);
            GraphView.Dispatcher.RegisterCommandHandler<GraphModelStateComponent, CommandThatMarksDeleted>(CommandThatMarksDeleted.DefaultCommandHandler, GraphView.GraphViewModel.GraphModelState);
            GraphView.Dispatcher.RegisterCommandHandler<GraphModelStateComponent, CommandThatRebuildsAll>(CommandThatRebuildsAll.DefaultCommandHandler, GraphView.GraphViewModel.GraphModelState);
            GraphView.Dispatcher.RegisterCommandHandler<CommandThatDoesNothing>(CommandThatDoesNothing.DefaultCommandHandler);

            m_GraphViewStateObserver = new GraphViewStateObserver(GraphView.GraphViewModel.GraphModelState);
            GraphTool.ObserverManager.RegisterObserver(m_GraphViewStateObserver);

            // Trigger initial update cycle.
            GraphView.Dispatch(new CreatePlacematCommand(new Rect(0, 0, 200, 200)));
        }

        [TearDown]
        public override void TearDown()
        {
            GraphTool.ObserverManager.UnregisterObserver(m_GraphViewStateObserver);
            base.TearDown();
        }

        static IEnumerable GetSomeCommands()
        {
            yield return new TestCaseData(new CommandThatMarksNew(), UpdateType.Partial).Returns(null);
            yield return new TestCaseData(new CommandThatMarksChanged(), UpdateType.Partial).Returns(null);
            yield return new TestCaseData(new CommandThatMarksDeleted(), UpdateType.Partial).Returns(null);
            yield return new TestCaseData(new CommandThatRebuildsAll(), UpdateType.Complete).Returns(null);
            yield return new TestCaseData(new CommandThatDoesNothing(), UpdateType.None).Returns(null);
        }

        [UnityTest, TestCaseSource(nameof(GetSomeCommands))]
        public IEnumerator TestRebuildType(UndoableCommand command, UpdateType rebuildType)
        {
            // Make sure initial update is done.
            yield return null;

            m_GraphViewStateObserver.UpdateType = UpdateType.None;
            GraphView.Dispatch(command);
            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(rebuildType));

            m_GraphViewStateObserver.UpdateType = UpdateType.None;
            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.None));
        }

        [UnityTest]
        public IEnumerator TestRebuildIsDoneOnce()
        {
            // Make sure initial update is done.
            yield return null;

            m_GraphViewStateObserver.UpdateType = UpdateType.None;
            Type0FakeNodeModel model;
            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                model = GraphModel.CreateNode<Type0FakeNodeModel>("Node 0", Vector2.zero);
                updater.MarkNew(model);
            }
            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.Partial));
            m_GraphViewStateObserver.UpdateType = UpdateType.None;

            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.None));
            m_GraphViewStateObserver.UpdateType = UpdateType.None;

            GraphView.Dispatch(new DeleteElementsCommand(model));
            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.Partial));
            m_GraphViewStateObserver.UpdateType = UpdateType.None;

            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.None));
        }
    }
}
