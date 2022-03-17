using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        /// The index in the group where the variable will be inserted.
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
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="modifierFlags">The modifiers to apply to the newly created variable.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        public CreateGraphVariableDeclarationCommand(string name, bool isExposed, TypeHandle typeHandle,
                                                     IGroupModel group = null, int indexInGroup = int.MaxValue,
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
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="modifierFlags">The modifiers to apply to the newly created variable.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        public CreateGraphVariableDeclarationCommand(string name, bool isExposed, TypeHandle typeHandle, Type variableType,
                                                     IGroupModel group = null, int indexInGroup = int.MaxValue,
                                                     ModifierFlags modifierFlags = ModifierFlags.None, SerializableGUID guid = default)
            : this(name, isExposed, typeHandle, group, indexInGroup, modifierFlags, guid)
        {
            VariableType = variableType;
        }

        /// <summary>
        /// Default command handler for CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="blackboardViewState">The blackboard view state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, BlackboardViewStateComponent blackboardViewState, SelectionStateComponent selectionState, CreateGraphVariableDeclarationCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                // Group expanded state is not part of the undo state
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            IVariableDeclarationModel newVariableDeclaration;
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var graphModel = graphModelState.GraphModel;
                if (graphModel.AssetModel.IsContainerGraph() && (command.ModifierFlags == ModifierFlags.Read || command.ModifierFlags == ModifierFlags.Write))
                {
                    Debug.LogWarning("Cannot create an input or an output variable declaration in a container graph.");
                    return;
                }

                if (command.VariableType != null)
                    newVariableDeclaration = graphModel.CreateGraphVariableDeclaration(command.VariableType, command.TypeHandle, command.VariableName,
                        command.ModifierFlags, command.IsExposed, command.Group, command.IndexInGroup, null, command.Guid);
                else
                    newVariableDeclaration = graphModel.CreateGraphVariableDeclaration(command.TypeHandle, command.VariableName,
                        command.ModifierFlags, command.IsExposed, command.Group, command.IndexInGroup, null, command.Guid);

                graphUpdater.MarkNew(newVariableDeclaration);
                graphUpdater.MarkForRename(newVariableDeclaration);
                graphUpdater.MarkChanged(newVariableDeclaration.ParentGroup, ChangeHint.Grouping);

                var recursiveSubgraphNodes = graphModel.GetRecursiveSubgraphNodes().ToList();
                if (recursiveSubgraphNodes.Any())
                {
                    foreach (var recursiveSubgraphNode in recursiveSubgraphNodes)
                        recursiveSubgraphNode.Update();
                    graphUpdater.MarkChanged(recursiveSubgraphNodes, ChangeHint.Data);
                }
            }
            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                var current = newVariableDeclaration.ParentGroup;
                while (current != null)
                {
                    bbUpdater.SetGroupModelExpanded(current, true);
                    current = current.ParentGroup;
                }
            }

            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
            using (var selectionUpdaters = selectionHelper.UpdateScopes)
            {
                foreach (var updater in selectionUpdaters)
                    updater.ClearSelection(graphModelState.GraphModel);
                selectionUpdaters.MainUpdateScope.SelectElement(newVariableDeclaration, true);
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
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, InitializeVariableCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                command.VariableDeclarationModel.CreateInitializationValue();
                graphUpdater.MarkChanged(command.VariableDeclarationModel, ChangeHint.Data);
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
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangeVariableTypeCommand command)
        {
            if (command.Type.IsValid)
            {
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveSingleState(graphModelState, command);
                }

                using (var graphUpdater = graphModelState.UpdateScope)
                {
                    if (command.VariableDeclarationModel.DataType != command.Type)
                        command.VariableDeclarationModel.CreateInitializationValue();

                    command.VariableDeclarationModel.DataType = command.Type;

                    var graphModel = graphModelState.GraphModel;
                    var variableReferences = graphModel.FindReferencesInGraph<IVariableNodeModel>(command.VariableDeclarationModel).ToList();
                    foreach (var usage in variableReferences)
                    {
                        usage.UpdateTypeFromDeclaration();
                    }

                    graphUpdater.MarkChanged(variableReferences, ChangeHint.Data);
                    graphUpdater.MarkChanged(command.VariableDeclarationModel, ChangeHint.Data);
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
        public IReadOnlyList<IVariableDeclarationModel> VariableDeclarationModels;

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
        /// <param name="variableDeclarationModels">The variables to update.</param>
        /// <param name="exposed">Whether the variable should be exposed.</param>
        public ExposeVariableCommand(bool exposed, IReadOnlyList<IVariableDeclarationModel> variableDeclarationModels) : this()
        {
            VariableDeclarationModels = variableDeclarationModels;
            Exposed = exposed;

            UndoString = Exposed ? "Show Variable" : "Hide Variable";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposeVariableCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variables to update.</param>
        /// <param name="exposed">Whether the variable should be exposed.</param>
        public ExposeVariableCommand(bool exposed, params IVariableDeclarationModel[] variableDeclarationModel) : this(exposed, (IReadOnlyList<IVariableDeclarationModel>)variableDeclarationModel)
        { }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ExposeVariableCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var variable in command.VariableDeclarationModels)
                {
                    variable.IsExposed = command.Exposed;
                }

                graphUpdater.MarkChanged(command.VariableDeclarationModels, ChangeHint.Data);

            }
        }
    }

    /// <summary>
    /// Command to update the tooltip of a variable.
    /// </summary>
    public class UpdateTooltipCommand : UndoableCommand
    {
        /// <summary>
        /// The variables to update.
        /// </summary>
        public IReadOnlyList<IVariableDeclarationModel> VariableDeclarationModels;
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
        /// <param name="variableDeclarationModels">The variables to update.</param>
        /// <param name="tooltip">The new tooltip for the variable.</param>
        public UpdateTooltipCommand(string tooltip, IReadOnlyList<IVariableDeclarationModel> variableDeclarationModels) : this()
        {
            VariableDeclarationModels = variableDeclarationModels;
            Tooltip = tooltip;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTooltipCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="tooltip">The new tooltip for the variable.</param>
        public UpdateTooltipCommand(string tooltip, params IVariableDeclarationModel[] variableDeclarationModel) : this(tooltip, (IReadOnlyList<IVariableDeclarationModel>)variableDeclarationModel)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, UpdateTooltipCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var graphModel = graphModelState.GraphModel;

                foreach (var variable in command.VariableDeclarationModels)
                {
                    variable.Tooltip = command.Tooltip;

                    var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(variable);
                    graphUpdater.MarkChanged(references, ChangeHint.Style);
                }
                graphUpdater.MarkChanged(command.VariableDeclarationModels, ChangeHint.Style);
            }
        }
    }
}
