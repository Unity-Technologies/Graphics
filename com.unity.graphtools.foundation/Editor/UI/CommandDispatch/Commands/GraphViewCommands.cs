using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to change the position of graph elements.
    /// </summary>
    public class MoveElementsCommand : ModelCommand<IMovable, Vector2>
    {
        const string k_UndoStringSingular = "Move Element";
        const string k_UndoStringPlural = "Move Elements";

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveElementsCommand"/> class.
        /// </summary>
        public MoveElementsCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveElementsCommand"/> class.
        /// </summary>
        /// <param name="delta">The amount of the move.</param>
        /// <param name="models">The models to move.</param>
        public MoveElementsCommand(Vector2 delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, delta, models) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveElementsCommand"/> class.
        /// </summary>
        /// <param name="delta">The amount of the move.</param>
        /// <param name="models">The models to move.</param>
        public MoveElementsCommand(Vector2 delta, params IMovable[] models)
            : this(delta, (IReadOnlyList<IMovable>)models) {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, MoveElementsCommand command)
        {
            if (command.Models == null || command.Value == Vector2.zero)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var movable in command.Models)
                {
                    movable.Move(command.Value);
                }

                graphUpdater.MarkChanged(command.Models.OfType<IGraphElementModel>(), ChangeHint.Layout);
            }
        }
    }

    /// <summary>
    /// Command to change the position of graph elements as the result of an automatic
    /// placement (auto-spacing or auto-align).
    /// </summary>
    // PF FIXME merge with MoveElementsCommand?
    public class AutoPlaceElementsCommand : ModelCommand<IMovable>
    {
        const string k_UndoStringSingular = "Auto Place Element";
        const string k_UndoStringPlural = "Auto Place Elements";

        /// <summary>
        /// The delta to apply to the model positions.
        /// </summary>
        public IReadOnlyList<Vector2> Deltas;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoPlaceElementsCommand"/> class.
        /// </summary>
        public AutoPlaceElementsCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoPlaceElementsCommand"/> class.
        /// </summary>
        /// <param name="delta">The amount of the move.</param>
        /// <param name="models">The models to move.</param>
        public AutoPlaceElementsCommand(IReadOnlyList<Vector2> delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
            Deltas = delta;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, AutoPlaceElementsCommand command)
        {
            if (command.Models == null || command.Deltas == null || command.Models.Count != command.Deltas.Count)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                for (int i = 0; i < command.Models.Count; ++i)
                {
                    IMovable model = command.Models[i];
                    Vector2 delta = command.Deltas[i];
                    model.Move(delta);
                }

                graphUpdater.MarkChanged(command.Models.OfType<IGraphElementModel>(), ChangeHint.Layout);
            }
        }
    }

    /// <summary>
    /// Command to delete graph elements.
    /// </summary>
    public class DeleteElementsCommand : ModelCommand<IGraphElementModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteElementsCommand"/> class.
        /// </summary>
        public DeleteElementsCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteElementsCommand"/> class.
        /// </summary>
        /// <param name="elementsToDelete">The elements to delete.</param>
        public DeleteElementsCommand(IReadOnlyList<IGraphElementModel> elementsToDelete)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToDelete)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteElementsCommand"/> class.
        /// </summary>
        /// <param name="elementsToDelete">The elements to delete.</param>
        public DeleteElementsCommand(params IGraphElementModel[] elementsToDelete)
            : this((IReadOnlyList<IGraphElementModel>)elementsToDelete)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, DeleteElementsCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(new IUndoableStateComponent[] { graphModelState, selectionState }, command);
            }

            using (var selectionUpdater = selectionState.UpdateScope)
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var changeDescription = graphModelState.GraphModel.DeleteElements(command.Models);
                var deletedModels = changeDescription.DeletedModels.ToList();

                foreach (var edgeToDelete in deletedModels.OfType<IEdgeModel>())
                {
                    var toPort = edgeToDelete.ToPort;
                    var fromPort = edgeToDelete.FromPort;

                    graphUpdater.MarkChanged(toPort, ChangeHint.GraphTopology);
                    graphUpdater.MarkChanged(fromPort, ChangeHint.GraphTopology);

                    if (toPort.PortType == PortType.MissingPort && !toPort.GetConnectedEdges().Any())
                    {
                        toPort.NodeModel.RemoveUnusedMissingPort(toPort);
                        graphUpdater.MarkChanged(toPort.NodeModel, ChangeHint.GraphTopology);
                    }
                    if (fromPort.PortType == PortType.MissingPort && !fromPort.GetConnectedEdges().Any())
                    {
                        fromPort.NodeModel.RemoveUnusedMissingPort(fromPort);
                        graphUpdater.MarkChanged(fromPort.NodeModel, ChangeHint.GraphTopology);
                    }
                }

                var recursiveSubgraphNodes = graphModelState.GraphModel.GetRecursiveSubgraphNodes().ToList();
                if (recursiveSubgraphNodes.Any() && deletedModels.Any(model => model is IVariableDeclarationModel variable && variable.IsInputOrOutput()))
                {
                    foreach (var recursiveSubgraphNode in recursiveSubgraphNodes)
                        recursiveSubgraphNode.Update();
                    graphUpdater.MarkChanged(graphModelState.GraphModel.GetRecursiveSubgraphNodes(), ChangeHint.Data);
                }

                var selectedModels = deletedModels.Where(selectionState.IsSelected).ToList();
                if (selectedModels.Any())
                {
                    selectionUpdater.SelectElements(selectedModels, false);
                }

                graphUpdater.MarkChanged(changeDescription.ChangedModels);
                graphUpdater.MarkDeleted(deletedModels);
            }
        }
    }

    /// <summary>
    /// Command to start the graph compilation.
    /// </summary>
    public class BuildAllEditorCommand : UndoableCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildAllEditorCommand"/> class.
        /// </summary>
        public BuildAllEditorCommand()
        {
            UndoString = "Compile Graph";
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(BuildAllEditorCommand command)
        {
        }
    }

    /// <summary>
    /// Command to paste elements in the graph.
    /// </summary>
    public class PasteSerializedDataCommand : UndoableCommand
    {
        /// <summary>
        /// The delta to apply to the pasted models.
        /// </summary>
        public Vector2 Delta;
        /// <summary>
        /// The data representing the graph element models to paste.
        /// </summary>
        public readonly CopyPasteData Data;

        /// <summary>
        /// The selected group, if any.
        /// </summary>
        public IGroupModel SelectedGroup;

        /// <summary>
        /// The operation that triggers this command.
        /// </summary>
        public PasteOperation Operation;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasteSerializedDataCommand"/> class.
        /// </summary>
        public PasteSerializedDataCommand()
        {
            UndoString = "Paste";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasteSerializedDataCommand"/> class.
        /// </summary>
        /// <param name="operation">The operation that triggers this command.</param>
        /// <param name="undoString">The name of the paste operation (Paste, Duplicate, etc.).</param>
        /// <param name="delta">The delta to apply on the pasted elements position.</param>
        /// <param name="data">The elements to paste.</param>
        /// <param name="selectedGroup">The selected group, If any.</param>
        public PasteSerializedDataCommand(PasteOperation operation, string undoString, Vector2 delta, CopyPasteData data, IGroupModel selectedGroup = null) : this()
        {
            if (!string.IsNullOrEmpty(undoString))
                UndoString = undoString;

            Delta = delta;
            Data = data;
            SelectedGroup = selectedGroup;
            Operation = operation;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, PasteSerializedDataCommand command)
        {
            if (!command.Data.IsEmpty())
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    var undoableStates = selectionHelper.UndoableSelectionStates.Append(graphModelState);
                    undoStateUpdater.SaveStates(undoableStates.ToArray(), command);
                }

                using (var graphViewUpdater = graphModelState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var selectionUpdater in selectionUpdaters)
                    {
                        selectionUpdater.ClearSelection(graphModelState.GraphModel);
                    }

                    CopyPasteData.PasteSerializedData(command.Operation, command.Delta, graphViewUpdater,
                        null, selectionUpdaters.MainUpdateScope, command.Data, graphModelState.GraphModel, command.SelectedGroup);
                }
            }
        }
    }

    /// <summary>
    /// A command to change the graph view position and zoom and optionally change the selection.
    /// </summary>
    public class ReframeGraphViewCommand : ICommand
    {
        /// <summary>
        /// The new position.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// The new zoom factor.
        /// </summary>
        public Vector3 Scale;
        /// <summary>
        /// The elements to select, in replacement of the current selection.
        /// </summary>
        public List<IGraphElementModel> NewSelection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReframeGraphViewCommand" /> class.
        /// </summary>
        public ReframeGraphViewCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReframeGraphViewCommand" /> class.
        /// </summary>
        /// <param name="position">The new position.</param>
        /// <param name="scale">The new zoom factor.</param>
        /// <param name="newSelection">If not null, the elements to select, in replacement of the current selection.
        /// If null, the selection is not changed.</param>
        public ReframeGraphViewCommand(Vector3 position, Vector3 scale, List<IGraphElementModel> newSelection = null) : this()
        {
            Position = position;
            Scale = scale;
            NewSelection = newSelection;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="graphModelStateComponent">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(GraphViewStateComponent graphViewState, GraphModelStateComponent graphModelStateComponent, SelectionStateComponent selectionState, ReframeGraphViewCommand command)
        {
            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                graphUpdater.Position = command.Position;
                graphUpdater.Scale = command.Scale;

                if (command.NewSelection != null)
                {
                    using (var selectionUpdaters = selectionHelper.UpdateScopes)
                    {
                        foreach (var selectionUpdater in selectionUpdaters)
                        {
                            selectionUpdater.ClearSelection(graphModelStateComponent.GraphModel);
                        }
                        selectionUpdaters.MainUpdateScope.SelectElements(command.NewSelection, true);
                    }
                }
            }
        }
    }
}
