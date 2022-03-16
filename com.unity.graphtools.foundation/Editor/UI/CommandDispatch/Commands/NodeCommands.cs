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
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, CollapseNodeCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var model in command.Models.OfType<ICollapsible>())
                {
                    model.Collapsed = command.Value;
                }

                graphUpdater.MarkChanged(command.Models.OfType<ICollapsible>().OfType<IGraphElementModel>(),
                    ChangeHint.Layout);
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
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, RenameElementCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                command.Model.Rename(command.ElementName);

                var graphModel = graphModelState.GraphModel;

                if (command.Model is IVariableDeclarationModel variableDeclarationModel)
                {
                    var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel);

                    graphUpdater.MarkChanged(references, ChangeHint.Data);
                    graphUpdater.MarkChanged(variableDeclarationModel, ChangeHint.Data);

                    var recursiveSubgraphNodes = graphModel.GetRecursiveSubgraphNodes().ToList();
                    if (recursiveSubgraphNodes.Any() && variableDeclarationModel.IsInputOrOutput())
                    {
                        foreach (var recursiveSubgraphNode in recursiveSubgraphNodes)
                            recursiveSubgraphNode.Update();
                        graphUpdater.MarkChanged(recursiveSubgraphNodes, ChangeHint.Data);
                    }
                }
                else if (command.Model is IVariableNodeModel variableModel)
                {
                    variableDeclarationModel = variableModel.VariableDeclarationModel;
                    var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel);

                    graphUpdater.MarkChanged(references, ChangeHint.Data);
                    graphUpdater.MarkChanged(variableDeclarationModel, ChangeHint.Data);
                }
                else if (command.Model is IEdgePortalModel edgePortalModel)
                {
                    var declarationModel = edgePortalModel.DeclarationModel as IGraphElementModel;
                    var references = graphModel.FindReferencesInGraph<IEdgePortalModel>(edgePortalModel.DeclarationModel);

                    graphUpdater.MarkChanged(references, ChangeHint.Data);
                    graphUpdater.MarkChanged(declarationModel, ChangeHint.Data);
                }
                else
                {
                    graphUpdater.MarkChanged(command.Model as IGraphElementModel, ChangeHint.Data);
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
            UndoString = "Update Value";
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
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, UpdateConstantValueCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                command.Constant.ObjectValue = command.Value;
                if (command.OwnerModel != null)
                {
                    graphUpdater.MarkChanged(command.OwnerModel, ChangeHint.Data);
                }
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
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, DisconnectNodeCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var graphModel = graphModelState.GraphModel;

                foreach (var nodeModel in command.Models)
                {
                    var edgeModels = nodeModel.GetConnectedEdges().ToList();
                    var deletedModels = graphModel.DeleteEdges(edgeModels);
                    graphUpdater.MarkDeleted(deletedModels);
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
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state of the graph view.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, BypassNodesCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(new IUndoableStateComponent[] { graphModelState, selectionState }, command);
            }

            var graphModel = graphModelState.GraphModel;
            IReadOnlyCollection<IGraphElementModel> deletedModels = null;

            using (var selectionUpdater = selectionState.UpdateScope)
            using (var graphUpdater = graphModelState.UpdateScope)
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

                    deletedModels = graphModel.DeleteEdges(inputEdgeModels);
                    graphUpdater.MarkDeleted(deletedModels);
                    deletedModels = graphModel.DeleteEdges(outputEdgeModels);
                    graphUpdater.MarkDeleted(deletedModels);

                    var edge = graphModel.CreateEdge(outputEdgeModels[0].ToPort, inputEdgeModels[0].FromPort);

                    graphUpdater.MarkNew(edge);
                }

                // [GTF-663] We delete nodes with deleteConnection = true because it may happens that one of the newly
                // added edge is connected to a node that will be deleted.
                deletedModels = graphModel.DeleteNodes(command.Models, deleteConnections: true);
                graphUpdater.MarkDeleted(deletedModels);

                var selectedModels = deletedModels.Where(selectionState.IsSelected).ToList();
                if (selectedModels.Any())
                {
                    selectionUpdater.SelectElements(selectedModels, false);
                }
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
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangeNodeStateCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.State = command.Value;
                }

                graphUpdater.MarkChanged(command.Models, ChangeHint.Data);
            }
        }
    }
}
