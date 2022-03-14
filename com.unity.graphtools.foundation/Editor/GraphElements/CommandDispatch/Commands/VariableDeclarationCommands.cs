using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a variable.
    /// </summary>
    public class CreateGraphVariableDeclarationCommand : UndoableCommand
    {
        /// <summary>
        /// The name of the variable to create.
        /// </summary>
        public string VariableName;

        /// <summary>
        /// Whether or not the variable is exposed.
        /// </summary>
        public bool IsExposed;

        /// <summary>
        /// The type of variable to create.
        /// </summary>
        public Type VariableType;

        /// <summary>
        /// The type of the variable to create.
        /// </summary>
        public TypeHandle TypeHandle;

        /// <summary>
        /// The SerializableGUID to assign to the newly created variable.
        /// </summary>
        public SerializableGUID Guid;

        /// <summary>
        /// The modifiers to apply to the newly created variable.
        /// </summary>
        public ModifierFlags ModifierFlags;

        /// <summary>
        /// The group to insert the variable in.
        /// </summary>
        public IGroupModel Group;

        /// <summary>
        /// The index in the group where the variable will be inserted
        /// </summary>
        public int IndexInGroup;

        /// <summary>
        /// Initializes a new CreateGraphVariableDeclarationCommand.
        /// </summary>
        public CreateGraphVariableDeclarationCommand()
        {
            UndoString = "Create Variable";
        }

        /// <summary>
        /// Initializes a new CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <remarks>This constructor will create the graph's default variable declaration.</remarks>
        /// <param name="name">The name of the variable to create.</param>
        /// <param name="isExposed">Whether or not the variable is exposed.</param>
        /// <param name="typeHandle">The type of data the new variable declaration to create represents.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. If -1 the variable will be added at the end of the group.</param>
        /// <param name="modifierFlags">The modifiers to apply to the newly created variable.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        public CreateGraphVariableDeclarationCommand(string name, bool isExposed, TypeHandle typeHandle,
                                                     IGroupModel group = null, int indexInGroup = -1,
                                                     ModifierFlags modifierFlags = ModifierFlags.None, SerializableGUID guid = default) : this()
        {
            VariableName = name;
            IsExposed = isExposed;
            TypeHandle = typeHandle;
            Guid = guid.Valid ? guid : SerializableGUID.Generate();
            ModifierFlags = modifierFlags;
            Group = group;
            IndexInGroup = indexInGroup;
        }

        /// <summary>
        /// Initializes a new CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <param name="name">The name of the variable to create.</param>
        /// <param name="isExposed">Whether or not the variable is exposed.</param>
        /// <param name="typeHandle">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableType">The type of variable declaration to create.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. If -1 the variable will be added at the end of the group.</param>
        /// <param name="modifierFlags">The modifiers to apply to the newly created variable.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        public CreateGraphVariableDeclarationCommand(string name, bool isExposed, TypeHandle typeHandle, Type variableType,
                                                     IGroupModel group = null, int indexInGroup = -1,
                                                     ModifierFlags modifierFlags = ModifierFlags.None, SerializableGUID guid = default)
            : this(name, isExposed, typeHandle, group, indexInGroup, modifierFlags, guid)
        {
            VariableType = variableType;
        }

        /// <summary>
        /// Default command handler for CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="blackboardViewState">The blackboard view state component.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, BlackboardViewStateComponent blackboardViewState, CreateGraphVariableDeclarationCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                // Group expanded state is not part of the undo state
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                var graphModel = graphViewState.GraphModel;
                IVariableDeclarationModel variableDeclaration;
                if (command.VariableType != null)
                    variableDeclaration = graphModel.CreateGraphVariableDeclaration(command.VariableType, command.TypeHandle, command.VariableName,
                        command.ModifierFlags, command.IsExposed, command.Group, command.IndexInGroup, null, command.Guid);
                else
                    variableDeclaration = graphModel.CreateGraphVariableDeclaration(command.TypeHandle, command.VariableName,
                        command.ModifierFlags, command.IsExposed, command.Group, command.IndexInGroup, null, command.Guid);

                graphUpdater.MarkNew(variableDeclaration);
                graphUpdater.MarkChanged(variableDeclaration.Group);
            }

            if (command.Group != null)
            {
                using (var bbUpdater = blackboardViewState.UpdateScope)
                {
                    bbUpdater.SetVariableGroupModelExpanded(command.Group, true);
                }
            }
        }
    }

    /// <summary>
    /// Command to reorder variable group items.
    /// </summary>
    public class ReorderGraphVariableDeclarationCommand : UndoableCommand
    {
        /// <summary>
        /// The variable group items to move.
        /// </summary>
        public readonly IReadOnlyList<IGroupItemModel> VariableGroupItems;
        /// <summary>
        /// The variable after which the moved variable group items should be inserted.
        /// </summary>
        public readonly IGroupItemModel InsertAfter;

        /// <summary>
        /// The group in which the variable group items will be added.
        /// </summary>
        public readonly IGroupModel Group;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderGraphVariableDeclarationCommand"/> class.
        /// </summary>
        public ReorderGraphVariableDeclarationCommand()
        {
            UndoString = "Reorder Variable";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderGraphVariableDeclarationCommand"/> class.
        /// </summary>
        /// <param name="group">The group in which the variable group items are reordered. Must be non null.</param>
        /// <param name="insertAfter">The item after which the moved variable group items should be inserted.</param>
        /// <param name="variableGroupItems">The variable group items to move.</param>
        public ReorderGraphVariableDeclarationCommand(IGroupModel group, IGroupItemModel insertAfter,
                                                      IReadOnlyList<IGroupItemModel> variableGroupItems) : this()
        {
            VariableGroupItems = variableGroupItems;
            InsertAfter = insertAfter;
            Group = group;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderGraphVariableDeclarationCommand"/> class.
        /// </summary>
        /// <param name="group">The group in which the variable group items are reordered. Must be non null.</param>
        /// <param name="insertAfter">The variable group item after which the moved variable group items should be inserted.</param>
        /// <param name="variableDeclarationsModels">The variable group items to move.</param>
        public ReorderGraphVariableDeclarationCommand(IGroupModel group, IGroupItemModel insertAfter,
                                                      params IVariableDeclarationModel[] variableDeclarationsModels)
            : this(group, insertAfter, (IReadOnlyList<IGroupItemModel>)variableDeclarationsModels)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="blackboardViewState">The blackboard view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState,
            BlackboardViewStateComponent blackboardViewState, ReorderGraphVariableDeclarationCommand command)
        {
            if (command.Group == null)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                // Group expanded state is not part of the undo state
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                var previousGroups = new HashSet<IGroupModel>(command.VariableGroupItems.Select(t => t.Group));
                if (command.Group.MoveItemsAfter(command.VariableGroupItems, command.InsertAfter))
                {
                    graphUpdater.MarkChanged(previousGroups);

                    graphUpdater.MarkChanged(command.Group);
                }
            }
            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                bbUpdater.SetVariableGroupModelExpanded(command.Group,true);
            }
        }
    }

    /// <summary>
    /// Command to create the initialization value of a variable.
    /// </summary>
    public class InitializeVariableCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to initialize.
        /// </summary>
        public IVariableDeclarationModel VariableDeclarationModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeVariableCommand"/> class.
        /// </summary>
        public InitializeVariableCommand()
        {
            UndoString = "Initialize Variable";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeVariableCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to initialize.</param>
        public InitializeVariableCommand(IVariableDeclarationModel variableDeclarationModel)
            : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, InitializeVariableCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                command.VariableDeclarationModel.CreateInitializationValue();
                graphUpdater.MarkChanged(command.VariableDeclarationModel);
            }
        }
    }

    /// <summary>
    /// Command to change the type of a variable.
    /// </summary>
    public class ChangeVariableTypeCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to update.
        /// </summary>
        public IVariableDeclarationModel VariableDeclarationModel;
        /// <summary>
        /// The new variable type.
        /// </summary>
        public TypeHandle Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableTypeCommand"/> class.
        /// </summary>
        public ChangeVariableTypeCommand()
        {
            UndoString = "Change Variable Type";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableTypeCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="type">The new variable type.</param>
        public ChangeVariableTypeCommand(IVariableDeclarationModel variableDeclarationModel, TypeHandle type) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Type = type;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, ChangeVariableTypeCommand command)
        {
            if (command.Type.IsValid)
            {
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveSingleState(graphViewState, command);
                }

                using (var graphUpdater = graphViewState.UpdateScope)
                {
                    if (command.VariableDeclarationModel.DataType != command.Type)
                        command.VariableDeclarationModel.CreateInitializationValue();

                    command.VariableDeclarationModel.DataType = command.Type;

                    var graphModel = graphViewState.GraphModel;
                    var variableReferences = graphModel.FindReferencesInGraph<IVariableNodeModel>(command.VariableDeclarationModel).ToList();
                    foreach (var usage in variableReferences)
                    {
                        usage.UpdateTypeFromDeclaration();
                    }

                    graphUpdater.MarkChanged(variableReferences);
                    graphUpdater.MarkChanged(command.VariableDeclarationModel);
                }
            }
        }
    }

    /// <summary>
    /// Command to change the Exposed value of a variable.
    /// </summary>
    public class ExposeVariableCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to update.
        /// </summary>
        public IVariableDeclarationModel VariableDeclarationModel;
        /// <summary>
        /// Whether the variable should be exposed.
        /// </summary>
        public bool Exposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposeVariableCommand"/> class.
        /// </summary>
        public ExposeVariableCommand()
        {
            UndoString = "Change Variable Exposition";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposeVariableCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="exposed">Whether the variable should be exposed.</param>
        public ExposeVariableCommand(IVariableDeclarationModel variableDeclarationModel, bool exposed) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Exposed = exposed;

            UndoString = Exposed ? "Show Variable" : "Hide Variable";
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, ExposeVariableCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                command.VariableDeclarationModel.IsExposed = command.Exposed;
                graphUpdater.MarkChanged(command.VariableDeclarationModel);
            }
        }
    }

    /// <summary>
    /// Command to update the tooltip of a variable.
    /// </summary>
    public class UpdateTooltipCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to update.
        /// </summary>
        public IVariableDeclarationModel VariableDeclarationModel;
        /// <summary>
        /// The new tooltip for the variable.
        /// </summary>
        public string Tooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTooltipCommand"/> class.
        /// </summary>
        public UpdateTooltipCommand()
        {
            UndoString = "Edit Tooltip";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTooltipCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="tooltip">The new tooltip for the variable.</param>
        public UpdateTooltipCommand(IVariableDeclarationModel variableDeclarationModel, string tooltip) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Tooltip = tooltip;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, UpdateTooltipCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                command.VariableDeclarationModel.Tooltip = command.Tooltip;

                var graphModel = graphViewState.GraphModel;
                var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(command.VariableDeclarationModel);
                graphUpdater.MarkChanged(references);
                graphUpdater.MarkChanged(command.VariableDeclarationModel);
            }
        }
    }
}
