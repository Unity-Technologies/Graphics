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
        public static void DefaultCommandHandler(GraphViewStateComponent graphViewState, CommandThatMarksNew command)
        {
            using (var graphUpdater = graphViewState.UpdateScope)
            {
                var placematModel = graphViewState.GraphModel.CreatePlacemat(Rect.zero);
                graphUpdater.MarkNew(placematModel);
            }
        }
    }

    class CommandThatMarksChanged : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphViewStateComponent graphViewState, CommandThatMarksChanged command)
        {
            using (var graphUpdater = graphViewState.UpdateScope)
            {
                var placemat = graphViewState.GraphModel.PlacematModels.FirstOrDefault();
                Debug.Assert(placemat != null);
                graphUpdater.MarkChanged(placemat);
            }
        }
    }

    class CommandThatMarksDeleted : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphViewStateComponent graphViewState, CommandThatMarksDeleted command)
        {
            using (var graphUpdater = graphViewState.UpdateScope)
            {
                var placemat = graphViewState.GraphModel.PlacematModels.FirstOrDefault();
                graphViewState.GraphModel.DeletePlacemats(new[] { placemat });
                Debug.Assert(placemat != null);
                graphUpdater.MarkDeleted(placemat);
            }
        }
    }

    class CommandThatRebuildsAll : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphViewStateComponent graphViewState, CommandThatRebuildsAll command)
        {
            using (var updater = graphViewState.UpdateScope)
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
        GraphViewStateComponent m_GraphViewStateComponent;

        public UpdateType UpdateType { get; set; }

        /// <inheritdoc />
        public GraphViewStateObserver(GraphViewStateComponent graphViewStateComponent)
            : base(graphViewStateComponent)
        {
            m_GraphViewStateComponent = graphViewStateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var observation = this.ObserveState(m_GraphViewStateComponent))
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

            GraphView.Dispatcher.RegisterCommandHandler<GraphViewStateComponent, CommandThatMarksNew>(CommandThatMarksNew.DefaultCommandHandler, GraphView.GraphViewState);
            GraphView.Dispatcher.RegisterCommandHandler<GraphViewStateComponent, CommandThatMarksChanged>(CommandThatMarksChanged.DefaultCommandHandler, GraphView.GraphViewState);
            GraphView.Dispatcher.RegisterCommandHandler<GraphViewStateComponent, CommandThatMarksDeleted>(CommandThatMarksDeleted.DefaultCommandHandler, GraphView.GraphViewState);
            GraphView.Dispatcher.RegisterCommandHandler<GraphViewStateComponent, CommandThatRebuildsAll>(CommandThatRebuildsAll.DefaultCommandHandler, GraphView.GraphViewState);
            GraphView.Dispatcher.RegisterCommandHandler<CommandThatDoesNothing>(CommandThatDoesNothing.DefaultCommandHandler);

            m_GraphViewStateObserver = new GraphViewStateObserver(GraphView.GraphViewState);
            GraphTool.ObserverManager.RegisterObserver(m_GraphViewStateObserver);

            // Trigger initial update cycle.
            GraphView.Dispatch(new CreatePlacematCommand(new Rect(0, 0, 200, 200)));
            GraphTool.Update();
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
            m_GraphViewStateObserver.UpdateType = UpdateType.None;
            Type0FakeNodeModel model;
            using (var updater = GraphView.GraphViewState.UpdateScope)
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
