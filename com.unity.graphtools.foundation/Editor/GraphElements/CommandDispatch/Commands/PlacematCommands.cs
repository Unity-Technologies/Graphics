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
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, CreatePlacematCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                var placematModel = graphViewState.GraphModel.CreatePlacemat(command.Position);
                if (command.Title != null)
                    placematModel.Title = command.Title;

                graphUpdater.MarkNew(placematModel);
            }
        }
    }

    /// <summary>
    /// Command to change the Z order of placemats.
    /// </summary>
    public class ChangePlacematOrderCommand : ModelCommand<IPlacematModel>
    {
        const string k_ChangeOrderUndoStringSingular = "Change Placemat Order";
        const string k_ChangeOrderUndoStringPlural = "Change Placemats Order";

        const string k_MovePlacematForwardStringSingular = "Move Placemat Forward";
        const string k_MovePlacematForwardStringPlural = "Move Placemats Forward";

        const string k_MovePlacematBackwardStringSingular = "Move Placemat Backward";
        const string k_MovePlacematBackwardStringPlural = "Move Placemats Backward";

        const string k_MovePlacematTopStringSingular = "Move Placemat Top";
        const string k_MovePlacematTopStringPlural = "Move Placemats Top";

        const string k_MovePlacematBottomStringSingular = "Move Placemat Bottom";
        const string k_MovePlacematBottomStringPlural = "Move Placemats Bottom";

        /// <summary>
        /// The types of reordering possible for placemats.
        /// </summary>
        public enum PlacematOrderingAction
        {
            /// <summary>
            /// Move the placement one up
            /// </summary>
            MoveForward,
            /// <summary>
            /// Move the placement one down
            /// </summary>
            MoveBackward,
            /// <summary>
            /// Move the placement all the way up
            /// </summary>
            MoveTop,
            /// <summary>
            /// Move the placement all the way down
            /// </summary>
            MoveBottom,
        }

        /// <summary>
        /// The type of reordering required.
        /// </summary>
        public PlacematOrderingAction OrderingAction;

        ChangePlacematOrderCommand(IReadOnlyList<IPlacematModel> models) :
            base(k_ChangeOrderUndoStringSingular, k_ChangeOrderUndoStringPlural, models) {}

        /// <summary>
        /// Initializes a new instance of the ChangePlacematOrderCommand class.
        /// </summary>
        /// <param name="orderingAction">The type of reordering required.</param>
        /// <param name="models">The models to reorder.</param>
        public ChangePlacematOrderCommand(PlacematOrderingAction orderingAction, IReadOnlyList<IPlacematModel> models) :
            base("Change placemat order", "Change placemats order", models)
        {
            OrderingAction = orderingAction;
            switch (orderingAction)
            {
                case PlacematOrderingAction.MoveForward:
                    UndoString = models?.Count > 1 ? k_MovePlacematForwardStringPlural : k_MovePlacematForwardStringSingular;
                    break;
                case PlacematOrderingAction.MoveBackward:
                    UndoString = models?.Count > 1 ? k_MovePlacematBackwardStringPlural : k_MovePlacematBackwardStringSingular;
                    break;
                case PlacematOrderingAction.MoveTop:
                    UndoString = models?.Count > 1 ? k_MovePlacematTopStringPlural : k_MovePlacematTopStringSingular;
                    break;
                case PlacematOrderingAction.MoveBottom:
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
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, ChangePlacematOrderCommand command)
        {
            if (command.Models == null || command.Models.Count == 0)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                switch (command.OrderingAction)
                {
                    case PlacematOrderingAction.MoveForward:
                        graphViewState.GraphModel.MoveForward(command.Models);
                        break;
                    case PlacematOrderingAction.MoveBackward:
                        graphViewState.GraphModel.MoveBackward(command.Models);
                        break;
                    case PlacematOrderingAction.MoveTop:
                        graphViewState.GraphModel.MoveForward(command.Models, true);
                        break;
                    case PlacematOrderingAction.MoveBottom:
                        graphViewState.GraphModel.MoveBackward(command.Models, true);
                        break;
                }
                graphUpdater.MarkChanged(command.Models);
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
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, CollapsePlacematCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                command.PlacematModel.Collapsed = command.Collapse;
                command.PlacematModel.HiddenElements = command.PlacematModel.Collapsed ? command.CollapsedElements : null;

                graphUpdater.MarkChanged(command.PlacematModel);
            }
        }
    }
}
