using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    public class GraphElementCommandTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;

        void PurgeAllChangesets(IState state)
        {
            foreach (var stateComponent in state.AllStateComponents)
            {
                stateComponent.PurgeOldChangesets(uint.MaxValue);
            }
        }

        [Test]
        public void ReplacesCurrentSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            GraphTool.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0, node1));
            PurgeAllChangesets(GraphTool.State);
            var changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count());
            var currentVersion = GraphTool.GraphViewSelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node1));

                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0);
                },
                () =>
                {
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));

                    if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(SelectElementsCommand))
                    {
                        changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));

                        Assert.IsTrue(changeset.ChangedModels.Contains(node0));
                        Assert.IsTrue(changeset.ChangedModels.Contains(node1));
                    }
                    else if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }

        [Test]
        public void AddToSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            GraphTool.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0));
            PurgeAllChangesets(GraphTool.State);
            var changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count);
            var currentVersion = GraphTool.GraphViewSelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));
                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, node1);
                },
                () =>
                {
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node1));

                    if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(SelectElementsCommand))
                    {
                        changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));

                        Assert.IsFalse(changeset.ChangedModels.Contains(node0));
                        Assert.IsTrue(changeset.ChangedModels.Contains(node1));
                    }
                    else if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }

        [Test]
        public void RemoveElementFromSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            GraphTool.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0, node1));
            PurgeAllChangesets(GraphTool.State);
            var changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count());
            var currentVersion = GraphTool.GraphViewSelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node1));

                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, node1);
                },
                () =>
                {
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));

                    if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(SelectElementsCommand))
                    {
                        changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));

                        Assert.IsFalse(changeset.ChangedModels.Contains(node0));
                        Assert.IsTrue(changeset.ChangedModels.Contains(node1));
                    }
                    else if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }

        [Test]
        public void ToggleSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            GraphTool.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0));
            PurgeAllChangesets(GraphTool.State);
            var changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count());
            var currentVersion = GraphTool.GraphViewSelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));

                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Toggle, node0, node1);
                },
                () =>
                {
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node1));

                    if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(SelectElementsCommand))
                    {
                        changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));

                        Assert.IsTrue(changeset.ChangedModels.Contains(node0));
                        Assert.IsTrue(changeset.ChangedModels.Contains(node1));
                    }
                    else if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }

        [Test]
        public void ClearSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            GraphTool.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0));
            PurgeAllChangesets(GraphTool.State);
            var changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count());
            var currentVersion = GraphTool.GraphViewSelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));

                    return new ClearSelectionCommand();
                },
                () =>
                {
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));

                    if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(ClearSelectionCommand))
                    {
                        changeset = GraphTool.GraphViewSelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));

                        Assert.IsTrue(changeset.ChangedModels.Contains(node0));
                        Assert.IsFalse(changeset.ChangedModels.Contains(node1));
                    }
                    else if (GraphTool.Dispatcher.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            GraphTool.GraphViewSelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }

        [Test]
        public void ReplaceCurrentSelectionClearsAllOtherSelectionStateComponents([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            var assetKey = PersistedState.MakeAssetKey(GraphTool.ToolState.AssetModel);
            var otherSelection = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, Hash128.Compute(42), assetKey);
            GraphTool.State.AddStateComponent(otherSelection);
            using (var updater = otherSelection.UpdateScope)
            {
                updater.SelectElements(new[] { node0 }, true);
            }

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));
                    Assert.IsTrue(otherSelection.IsSelected(node0));
                    Assert.IsFalse(otherSelection.IsSelected(node1));

                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node1);
                },
                () =>
                {
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node1));
                    Assert.IsFalse(otherSelection.IsSelected(node0));
                    Assert.IsFalse(otherSelection.IsSelected(node1));
                });
        }


        [Test]
        public void AddToCurrentSelectionDoesNotClearOtherSelectionStateComponents([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            var assetKey = PersistedState.MakeAssetKey(GraphTool.ToolState.AssetModel);
            var otherSelection = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, Hash128.Compute(42), assetKey);
            GraphTool.State.AddStateComponent(otherSelection);
            using (var updater = otherSelection.UpdateScope)
            {
                updater.SelectElements(new[] { node0 }, true);
            }

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node1));
                    Assert.IsTrue(otherSelection.IsSelected(node0));
                    Assert.IsFalse(otherSelection.IsSelected(node1));

                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, node1);
                },
                () =>
                {
                    Assert.IsFalse(GraphTool.GraphViewSelectionState.IsSelected(node0));
                    Assert.IsTrue(GraphTool.GraphViewSelectionState.IsSelected(node1));
                    Assert.IsTrue(otherSelection.IsSelected(node0));
                    Assert.IsFalse(otherSelection.IsSelected(node1));
                });
        }
    }
}
