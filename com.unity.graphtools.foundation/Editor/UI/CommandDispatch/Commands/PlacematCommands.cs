using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a placemat.
    /// </summary>
    public class CreatePlacematCommand : UndoableCommand
    {
        /// <summary>
        /// The position and size of the new placemat.
        /// </summary>
        public Rect Position;
        /// <summary>
        /// The placemat title.
        /// </summary>
        public string Title;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePlacematCommand"/> class.
        /// </summary>
        public CreatePlacematCommand()
        {
            UndoString = "Create Placemat";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePlacematCommand"/> class.
        /// </summary>
        /// <param name="position">The position of the new placemat.</param>
        /// <param name="title">The title of the new placemat.</param>
        public CreatePlacematCommand(Rect position, string title = null) : this()
        {
            Position = position;
            Title = title;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreatePlacematCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            IPlacematModel placematModel;
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                placematModel = graphModelState.GraphModel.CreatePlacemat(command.Position);
                if (command.Title != null)
                    placematModel.Title = command.Title;

                graphUpdater.MarkNew(placematModel);
                graphUpdater.MarkForRename(placematModel);
            }

            if (placematModel != null)
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection(graphModelState.GraphModel);
                    selectionUpdaters.MainUpdateScope.SelectElement(placematModel, true);
                }
            }
        }
    }

    /// <summary>
    /// Command to change the Z order of placemats.
    /// </summary>
    public class ChangePlacematOrderCommand : ModelCommand<IPlacematModel>
    {
        const string k_MovePlacematForwardStringSingular = "Move Placemat Forward";
        const string k_MovePlacematForwardStringPlural = "Move Placemats Forward";

        const string k_MovePlacematBackwardStringSingular = "Move Placemat Backward";
        const string k_MovePlacematBackwardStringPlural = "Move Placemats Backward";

        const string k_MovePlacematTopStringSingular = "Move Placemat Top";
        const string k_MovePlacematTopStringPlural = "Move Placemats Top";

        const string k_MovePlacematBottomStringSingular = "Move Placemat Bottom";
        const string k_MovePlacematBottomStringPlural = "Move Placemats Bottom";

        /// <summary>
        /// The type of reordering required.
        /// </summary>
        public ZOrderMove OrderingAction;

        /// <summary>
        /// Initializes a new instance of the ChangePlacematOrderCommand class.
        /// </summary>
        /// <param name="orderingAction">The type of reordering required.</param>
        /// <param name="models">The models to reorder.</param>
        public ChangePlacematOrderCommand(ZOrderMove orderingAction, IReadOnlyList<IPlacematModel> models) :
            base("Change placemat order", "Change placemats order", models)
        {
            OrderingAction = orderingAction;
            switch (orderingAction)
            {
                case ZOrderMove.Forward:
                    UndoString = models?.Count > 1 ? k_MovePlacematForwardStringPlural : k_MovePlacematForwardStringSingular;
                    break;
                case ZOrderMove.Backward:
                    UndoString = models?.Count > 1 ? k_MovePlacematBackwardStringPlural : k_MovePlacematBackwardStringSingular;
                    break;
                case ZOrderMove.ToFront:
                    UndoString = models?.Count > 1 ? k_MovePlacematTopStringPlural : k_MovePlacematTopStringSingular;
                    break;
                case ZOrderMove.ToBack:
                    UndoString = models?.Count > 1 ? k_MovePlacematBottomStringPlural : k_MovePlacematBottomStringSingular;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderingAction), orderingAction, null);
            }
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangePlacematOrderCommand command)
        {
            if (command.Models == null || command.Models.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                graphModelState.GraphModel.ReorderPlacemats(command.Models, command.OrderingAction);
                graphUpdater.MarkChanged(command.Models, ChangeHint.Layout);
            }
        }
    }

    /// <summary>
    /// Command to collapse or expand placemats.
    /// </summary>
    public class CollapsePlacematCommand : UndoableCommand
    {
        /// <summary>
        /// The placemat to collapse or expand.
        /// </summary>
        public readonly IPlacematModel PlacematModel;
        /// <summary>
        /// True if the placemat should be collapsed, false otherwise.
        /// </summary>
        public readonly bool Collapse;
        /// <summary>
        /// If collapsing the placemat, the elements hidden by the collapsed placemat.
        /// </summary>
        public readonly IReadOnlyList<IGraphElementModel> CollapsedElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapsePlacematCommand"/> class.
        /// </summary>
        public CollapsePlacematCommand()
        {
            UndoString = "Collapse Or Expand Placemat";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapsePlacematCommand"/> class.
        /// </summary>
        /// <param name="placematModel">The placemat to collapse or expand.</param>
        /// <param name="collapse">True if the placemat should be collapsed, false otherwise.</param>
        /// <param name="collapsedElements">If collapsing the placemat, the elements hidden by the collapsed placemat.</param>
        public CollapsePlacematCommand(IPlacematModel placematModel, bool collapse,
                                       IReadOnlyList<IGraphElementModel> collapsedElements) : this()
        {
            PlacematModel = placematModel;
            Collapse = collapse;
            CollapsedElements = collapsedElements;

            UndoString = Collapse ? "Collapse Placemat" : "Expand Placemat";
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, CollapsePlacematCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                command.PlacematModel.Collapsed = command.Collapse;
                command.PlacematModel.HiddenElements = command.PlacematModel.Collapsed ? command.CollapsedElements : null;

                graphUpdater.MarkChanged(command.PlacematModel, ChangeHint.Layout);
            }
        }
    }
}
