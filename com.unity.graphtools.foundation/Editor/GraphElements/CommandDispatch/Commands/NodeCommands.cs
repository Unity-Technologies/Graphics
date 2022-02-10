using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to collapse and expand nodes.
    /// </summary>
    public class CollapseNodeCommand : ModelCommand<INodeModel, bool>
    {
        const string k_CollapseUndoStringSingular = "Collapse Node";
        const string k_CollapseUndoStringPlural = "Collapse Nodes";
        const string k_ExpandUndoStringSingular = "Expand Node";
        const string k_ExpandUndoStringPlural = "Expand Nodes";

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseNodeCommand"/> class.
        /// </summary>
        public CollapseNodeCommand()
            : base("Collapse Or Expand Node") {}

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseNodeCommand"/> class.
        /// </summary>
        /// <param name="value">True if the nodes should be collapsed, false otherwise.</param>
        /// <param name="nodes">The nodes to expand or collapse.</param>
        public CollapseNodeCommand(bool value, IReadOnlyList<INodeModel> nodes)
            : base(value ? k_CollapseUndoStringSingular : k_ExpandUndoStringSingular,
                   value ? k_CollapseUndoStringPlural : k_ExpandUndoStringPlural, value, nodes)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseNodeCommand"/> class.
        /// </summary>
        /// <param name="value">True if the nodes should be collapsed, false otherwise.</param>
        /// <param name="nodes">The nodes to expand or collapse.</param>
        public CollapseNodeCommand(bool value, params INodeModel[] nodes)
            : this(value, (IReadOnlyList<INodeModel>)nodes)
        {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, CollapseNodeCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                foreach (var model in command.Models.OfType<ICollapsible>())
                {
                    model.Collapsed = command.Value;
                }

                graphUpdater.MarkChanged(command.Models.OfType<ICollapsible>().OfType<IGraphElementModel>());
            }
        }
    }

    /// <summary>
    /// Command to change the name of a graph element.
    /// </summary>
    public class RenameElementCommand : UndoableCommand
    {
        /// <summary>
        /// The graph element to rename.
        /// </summary>
        public IRenamable Model;
        /// <summary>
        /// The new name.
        /// </summary>
        public string ElementName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameElementCommand"/> class.
        /// </summary>
        public RenameElementCommand()
        {
            UndoString = "Rename Element";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameElementCommand"/> class.
        /// </summary>
        /// <param name="model">The graph element to rename.</param>
        /// <param name="name">The new name.</param>
        public RenameElementCommand(IRenamable model, string name) : this()
        {
            Model = model;
            ElementName = name;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, RenameElementCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                command.Model.Rename(command.ElementName);

                var graphModel = graphViewState.GraphModel;

                if (command.Model is IVariableDeclarationModel variableDeclarationModel)
                {
                    var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel);

                    graphUpdater.MarkChanged(references);
                    graphUpdater.MarkChanged(variableDeclarationModel);
                }
                else if (command.Model is IVariableNodeModel variableModel)
                {
                    variableDeclarationModel = variableModel.VariableDeclarationModel;
                    var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel);

                    graphUpdater.MarkChanged(references);
                    graphUpdater.MarkChanged(variableDeclarationModel);
                }
                else if (command.Model is IEdgePortalModel edgePortalModel)
                {
                    var declarationModel = edgePortalModel.DeclarationModel as IGraphElementModel;
                    var references = graphModel.FindReferencesInGraph<IEdgePortalModel>(edgePortalModel.DeclarationModel);

                    graphUpdater.MarkChanged(references);
                    graphUpdater.MarkChanged(declarationModel);
                }
                else
                {
                    graphUpdater.MarkChanged(command.Model as IGraphElementModel);
                }
            }
        }
    }

    /// <summary>
    /// Command to update the value of a constant.
    /// </summary>
    public class UpdateConstantValueCommand : UndoableCommand
    {
        /// <summary>
        /// The constant to update.
        /// </summary>
        public IConstant Constant;
        /// <summary>
        /// The new value.
        /// </summary>
        public object Value;
        /// <summary>
        /// The node model that owns the constant, if any.
        /// </summary>
        public IGraphElementModel OwnerModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateConstantValueCommand"/> class.
        /// </summary>
        public UpdateConstantValueCommand()
        {
            UndoString = "Update Constant Value";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateConstantValueCommand"/> class.
        /// </summary>
        /// <param name="constant">The constant to update.</param>
        /// <param name="value">The new value.</param>
        /// <param name="owner">The model that owns the constant, if any.</param>
        public UpdateConstantValueCommand(IConstant constant, object value, IGraphElementModel owner) : this()
        {
            Constant = constant;
            Value = value;
            OwnerModel = owner;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, UpdateConstantValueCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                command.Constant.ObjectValue = command.Value;
                if (command.OwnerModel != null)
                {
                    // PF FIXME: BlackboardSectionListPart.UpdatePartFromModel rebuilds everything when
                    // one of the variable changes. This has the effect to make the constant editor
                    // text field lose its focus, stopping the edition of the value.
                    // So until BlackboardSectionListPart.UpdatePartFromModel is smarter,
                    // we cannot MarkChanged the owner if it is a variable declaration model.
                    if (!(command.OwnerModel is IVariableDeclarationModel))
                    {
                        graphUpdater.MarkChanged(command.OwnerModel);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Command to update the default value of a port.
    /// </summary>
    // PF FIXME: merge with UpdateConstantValueCommand
    public class UpdatePortConstantCommand : UndoableCommand
    {
        /// <summary>
        /// The port for which to update the default value.
        /// </summary>
        public IPortModel PortModel;
        /// <summary>
        /// The new value.
        /// </summary>
        public object NewValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePortConstantCommand"/> class.
        /// </summary>
        public UpdatePortConstantCommand()
        {
            UndoString = "Update Port Value";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePortConstantCommand"/> class.
        /// </summary>
        /// <param name="portModel">The port for which to update the default value.</param>
        /// <param name="newValue">The new value.</param>
        public UpdatePortConstantCommand(IPortModel portModel, object newValue) : this()
        {
            PortModel = portModel;
            NewValue = newValue;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, UpdatePortConstantCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                if (command.PortModel.EmbeddedValue is IStringWrapperConstantModel stringWrapperConstantModel)
                    stringWrapperConstantModel.StringValue = (string)command.NewValue;
                else
                    command.PortModel.EmbeddedValue.ObjectValue = command.NewValue;

                graphUpdater.MarkChanged(command.PortModel);
            }
        }
    }

    /// <summary>
    /// Command to remove all edges on nodes.
    /// </summary>
    public class DisconnectNodeCommand : ModelCommand<INodeModel>
    {
        const string k_UndoStringSingular = "Disconnect Node";
        const string k_UndoStringPlural = "Disconnect Nodes";

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectNodeCommand"/> class.
        /// </summary>
        public DisconnectNodeCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectNodeCommand"/> class.
        /// </summary>
        /// <param name="nodeModels">The nodes to disconnect.</param>
        public DisconnectNodeCommand(IReadOnlyList<INodeModel> nodeModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, nodeModels) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectNodeCommand"/> class.
        /// </summary>
        /// <param name="nodeModels">The nodes to disconnect.</param>
        public DisconnectNodeCommand(params INodeModel[] nodeModels)
            : this((IReadOnlyList<INodeModel>)nodeModels) {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, DisconnectNodeCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                var graphModel = graphViewState.GraphModel;

                foreach (var nodeModel in command.Models)
                {
                    var edgeModels = nodeModel.GetConnectedEdges().ToList();
                    graphModel.DeleteEdges(edgeModels);
                    graphUpdater.MarkDeleted(edgeModels);
                }
            }
        }
    }

    /// <summary>
    /// Command to bypass nodes using edges. Optionally deletes the nodes.
    /// </summary>
    public class BypassNodesCommand : ModelCommand<INodeModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        /// <summary>
        /// The nodes to bypass.
        /// </summary>
        public readonly IReadOnlyList<IInputOutputPortsNodeModel> NodesToBypass;

        /// <summary>
        /// Initializes a new instance of the <see cref="BypassNodesCommand"/> class.
        /// </summary>
        public BypassNodesCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="BypassNodesCommand"/> class.
        /// </summary>
        /// <param name="nodesToBypass">The nodes to bypass.</param>
        /// <param name="elementsToRemove">The nodes to delete.</param>
        public BypassNodesCommand(IReadOnlyList<IInputOutputPortsNodeModel> nodesToBypass, IReadOnlyList<INodeModel> elementsToRemove)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToRemove)
        {
            NodesToBypass = nodesToBypass;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, BypassNodesCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            var graphModel = graphViewState.GraphModel;

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                foreach (var model in command.NodesToBypass)
                {
                    var inputEdgeModels = new List<IEdgeModel>();
                    foreach (var portModel in model.InputsByDisplayOrder)
                    {
                        inputEdgeModels.AddRange(graphModel.GetEdgesForPort(portModel));
                    }

                    if (!inputEdgeModels.Any())
                        continue;

                    var outputEdgeModels = new List<IEdgeModel>();
                    foreach (var portModel in model.OutputsByDisplayOrder)
                    {
                        outputEdgeModels.AddRange(graphModel.GetEdgesForPort(portModel));
                    }

                    if (!outputEdgeModels.Any())
                        continue;

                    graphModel.DeleteEdges(inputEdgeModels);
                    graphModel.DeleteEdges(outputEdgeModels);

                    var edge = graphModel.CreateEdge(outputEdgeModels[0].ToPort, inputEdgeModels[0].FromPort);

                    graphUpdater.MarkDeleted(inputEdgeModels);
                    graphUpdater.MarkDeleted(outputEdgeModels);
                    graphUpdater.MarkNew(edge);
                }

                var deletedModels = graphModel.DeleteNodes(command.Models, deleteConnections: false);
                graphUpdater.MarkDeleted(deletedModels);
            }
        }
    }

    /// <summary>
    /// Command to change the state of nodes.
    /// </summary>
    public class ChangeNodeStateCommand : ModelCommand<INodeModel, ModelState>
    {
        const string k_UndoStringSingular = "Change Node State";
        const string k_UndoStringPlural = "Change Nodes State";

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeNodeStateCommand"/> class.
        /// </summary>
        public ChangeNodeStateCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeNodeStateCommand"/> class.
        /// </summary>
        /// <param name="state">The new node state.</param>
        /// <param name="nodeModels">The nodes to modify.</param>
        public ChangeNodeStateCommand(ModelState state, IReadOnlyList<INodeModel> nodeModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, state, nodeModels) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeNodeStateCommand"/> class.
        /// </summary>
        /// <param name="state">The new node state.</param>
        /// <param name="nodeModels">The nodes to modify.</param>
        public ChangeNodeStateCommand(ModelState state, params INodeModel[] nodeModels)
            : this(state, (IReadOnlyList<INodeModel>)nodeModels) {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphViewState">The graph view state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, ChangeNodeStateCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.State = command.Value;
                }

                graphUpdater.MarkChanged(command.Models);
            }
        }
    }
}
