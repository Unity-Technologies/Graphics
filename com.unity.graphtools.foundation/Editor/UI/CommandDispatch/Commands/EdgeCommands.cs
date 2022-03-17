using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    static class EdgeCommandConfig
    {
        public const int nodeOffset = 60;
    }

    /// <summary>
    /// Command to create a new edge.
    /// </summary>
    public class CreateEdgeCommand : UndoableCommand
    {
        const string k_UndoString = "Create Edge";

        /// <summary>
        /// Destination port.
        /// </summary>
        public IPortModel ToPortModel;
        /// <summary>
        /// Origin port.
        /// </summary>
        public IPortModel FromPortModel;
        /// <summary>
        /// List of edges to delete.
        /// </summary>
        public IReadOnlyList<IEdgeModel> EdgeModelsToDelete;
        /// <summary>
        /// Align the node that owns the <see cref="FromPortModel"/> to the <see cref="ToPortModel"/>.
        /// </summary>
        public bool AlignFromNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEdgeCommand" /> class.
        /// </summary>
        public CreateEdgeCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEdgeCommand" /> class.
        /// </summary>
        /// <param name="toPortModel">Destination port.</param>
        /// <param name="fromPortModel">Origin port.</param>
        /// <param name="alignFromNode">Set to true if the node that owns the <paramref name="fromPortModel"/> should be aligned on the <paramref name="toPortModel"/>.</param>
        public CreateEdgeCommand(IPortModel toPortModel, IPortModel fromPortModel, bool alignFromNode = false)
            : this(toPortModel, fromPortModel, null, alignFromNode) { }

        // TODO VladN: remove once GTF-723 is addressed and we don't need to pass EdgeToDelete anymore
        internal CreateEdgeCommand(IPortModel toPortModel, IPortModel fromPortModel,
            IReadOnlyList<IEdgeModel> edgeModelsToDelete,
            bool alignFromNode = false)
            : this()
        {
            Assert.IsTrue(toPortModel == null || toPortModel.Direction == PortDirection.Input);
            Assert.IsTrue(fromPortModel == null || fromPortModel.Direction == PortDirection.Output);
            ToPortModel = toPortModel;
            FromPortModel = fromPortModel;
            EdgeModelsToDelete = edgeModelsToDelete;
            AlignFromNode = alignFromNode;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="preferences">The tool preferences.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, Preferences preferences, CreateEdgeCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            var createdElements = new List<IGraphElementModel>();
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var graphModel = graphModelState.GraphModel;

                var fromPortModel = command.FromPortModel;
                var toPortModel = command.ToPortModel;

                var edgesToDelete = (command.EdgeModelsToDelete ?? new List<IEdgeModel>())
                    .Concat(GetDropEdgeModelsToDelete(command.FromPortModel))
                    .Concat(GetDropEdgeModelsToDelete(command.ToPortModel))
                    .ToList();

                if (edgesToDelete.Count > 0)
                {
                    var deletedElements = graphModel.DeleteEdges(edgesToDelete);

                    foreach (var edgeToDelete in edgesToDelete)
                    {
                        var toPort = edgeToDelete.ToPort;
                        var fromPort = edgeToDelete.FromPort;
                        if (toPort.NodeModel is ISubgraphNodeModel toPortSubgraphNodeModel && toPort.PortType == PortType.MissingPort && !toPort.GetConnectedEdges().Any())
                        {
                            toPortSubgraphNodeModel.RemoveUnusedMissingPort(toPort);
                            graphUpdater.MarkChanged(toPortSubgraphNodeModel, ChangeHint.GraphTopology);
                        }

                        if (fromPort.NodeModel is ISubgraphNodeModel fromPortSubgraphNodeModel && fromPort.PortType == PortType.MissingPort && !fromPort.GetConnectedEdges().Any())
                        {
                            fromPortSubgraphNodeModel.RemoveUnusedMissingPort(fromPort);
                            graphUpdater.MarkChanged(fromPortSubgraphNodeModel, ChangeHint.GraphTopology);
                        }
                    }

                    graphUpdater.MarkDeleted(deletedElements);
                }

                IEdgeModel edgeModel;
                // Auto-itemization preferences will determine if a new node is created or not
                if ((fromPortModel.NodeModel is IConstantNodeModel && preferences.GetBool(BoolPref.AutoItemizeConstants)) ||
                    (fromPortModel.NodeModel is IVariableNodeModel && preferences.GetBool(BoolPref.AutoItemizeVariables)))
                {
                    var itemizedNode = graphModel.CreateItemizedNode(EdgeCommandConfig.nodeOffset, ref fromPortModel);
                    if (itemizedNode != null)
                    {
                        graphUpdater.MarkNew(itemizedNode);
                        createdElements.Add(itemizedNode);
                    }
                    edgeModel = graphModel.CreateEdge(toPortModel, fromPortModel);
                }
                else
                {
                    edgeModel = graphModel.CreateEdge(toPortModel, fromPortModel);
                    createdElements.Add(edgeModel);
                }
                graphUpdater.MarkNew(edgeModel);

                if (toPortModel != null)
                {
                    graphUpdater.MarkChanged(toPortModel, ChangeHint.GraphTopology);
                }

                if (fromPortModel != null)
                {
                    graphUpdater.MarkChanged(fromPortModel, ChangeHint.GraphTopology);
                }

                if (command.AlignFromNode)
                {
                    graphUpdater.MarkModelToAutoAlign(edgeModel);
                }
            }

            if (createdElements.Any())
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection(graphModelState.GraphModel);
                    selectionUpdaters.MainUpdateScope.SelectElements(createdElements, true);
                }
            }
        }

        static IEnumerable<IEdgeModel> GetDropEdgeModelsToDelete(IPortModel portModel)
        {
            if (portModel == null || portModel.Capacity == PortCapacity.Multi)
                return Enumerable.Empty<IEdgeModel>();
            return portModel.GetConnectedEdges().Where(e => !(e is IGhostEdge));
        }
    }

    /// <summary>
    /// Command to delete one or more edges.
    /// </summary>
    public class DeleteEdgeCommand : ModelCommand<IEdgeModel>
    {
        const string k_UndoStringSingular = "Delete Edge";
        const string k_UndoStringPlural = "Delete Edges";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEdgeCommand" /> class.
        /// </summary>
        public DeleteEdgeCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEdgeCommand" /> class.
        /// </summary>
        /// <param name="edgesToDelete">The list of edges to delete.</param>
        public DeleteEdgeCommand(IReadOnlyList<IEdgeModel> edgesToDelete)
            : base(k_UndoStringSingular, k_UndoStringPlural, edgesToDelete)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEdgeCommand" /> class.
        /// </summary>
        /// <param name="edgesToDelete">The list of edges to delete.</param>
        public DeleteEdgeCommand(params IEdgeModel[] edgesToDelete)
            : this((IReadOnlyList<IEdgeModel>)edgesToDelete)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, DeleteEdgeCommand command)
        {
            if (command.Models == null || !command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var edge in command.Models)
                {
                    if (edge.ToPort != null)
                    {
                        graphUpdater.MarkChanged(edge.ToPort, ChangeHint.GraphTopology);
                    }

                    if (edge.FromPort != null)
                    {
                        graphUpdater.MarkChanged(edge.FromPort, ChangeHint.GraphTopology);
                    }
                }

                var deletedElements = graphModelState.GraphModel.DeleteEdges(command.Models);
                graphUpdater.MarkDeleted(deletedElements);
            }
        }
    }

    /// <summary>
    /// Command to change the order of an edge.
    /// </summary>
    public class ReorderEdgeCommand : UndoableCommand
    {
        /// <summary>
        /// Reorder operations.
        /// </summary>
        public enum ReorderType
        {
            /// <summary>
            /// Make the edge the first edge.
            /// </summary>
            MoveFirst,
            /// <summary>
            /// Move the edge one position towards the beginning.
            /// </summary>
            MoveUp,
            /// <summary>
            /// Move the edge one position towards the end.
            /// </summary>
            MoveDown,
            /// <summary>
            /// Make the edge the last edge.
            /// </summary>
            MoveLast
        }

        /// <summary>
        /// The edge to reorder.
        /// </summary>
        public readonly IEdgeModel EdgeModel;
        /// <summary>
        /// The reorder operation to apply.
        /// </summary>
        public readonly ReorderType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderEdgeCommand"/> class.
        /// </summary>
        public ReorderEdgeCommand()
        {
            UndoString = "Reorder Edge";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderEdgeCommand"/> class.
        /// </summary>
        /// <param name="edgeModel">The edge to reorder.</param>
        /// <param name="type">The reorder operation to apply.</param>
        public ReorderEdgeCommand(IEdgeModel edgeModel, ReorderType type) : this()
        {
            EdgeModel = edgeModel;
            Type = type;

            switch (Type)
            {
                case ReorderType.MoveFirst:
                    UndoString = "Move Edge First";
                    break;
                case ReorderType.MoveUp:
                    UndoString = "Move Edge Up";
                    break;
                case ReorderType.MoveDown:
                    UndoString = "Move Edge Down";
                    break;
                case ReorderType.MoveLast:
                    UndoString = "Move Edge Last";
                    break;
            }
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ReorderEdgeCommand command)
        {
            if (command.EdgeModel?.FromPort is IReorderableEdgesPortModel fromPort && fromPort.HasReorderableEdges)
            {
                var siblingEdges = fromPort.GetConnectedEdges().ToList();
                var siblingEdgesCount = siblingEdges.Count;
                if (siblingEdgesCount > 1)
                {
                    var index = siblingEdges.IndexOf(command.EdgeModel);
                    Action<IEdgeModel> reorderAction = null;
                    switch (command.Type)
                    {
                        case ReorderType.MoveFirst when index > 0:
                            reorderAction = fromPort.MoveEdgeFirst;
                            break;
                        case ReorderType.MoveUp when index > 0:
                            reorderAction = fromPort.MoveEdgeUp;
                            break;
                        case ReorderType.MoveDown when index < siblingEdgesCount - 1:
                            reorderAction = fromPort.MoveEdgeDown;
                            break;
                        case ReorderType.MoveLast when index < siblingEdgesCount - 1:
                            reorderAction = fromPort.MoveEdgeLast;
                            break;
                    }

                    if (reorderAction != null)
                    {
                        using (var undoStateUpdater = undoState.UpdateScope)
                        {
                            undoStateUpdater.SaveSingleState(graphModelState, command);
                        }

                        using (var graphUpdater = graphModelState.UpdateScope)
                        {
                            reorderAction(command.EdgeModel);

                            graphUpdater.MarkChanged(siblingEdges, ChangeHint.GraphTopology);
                            graphUpdater.MarkChanged(fromPort.NodeModel, ChangeHint.GraphTopology);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Command to insert a node in the middle of an edge.
    /// </summary>
    public class SplitEdgeAndInsertExistingNodeCommand : UndoableCommand
    {
        public readonly IEdgeModel EdgeModel;
        public readonly IInputOutputPortsNodeModel NodeModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitEdgeAndInsertExistingNodeCommand"/> class.
        /// </summary>
        public SplitEdgeAndInsertExistingNodeCommand()
        {
            UndoString = "Insert Node On Edge";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitEdgeAndInsertExistingNodeCommand"/> class.
        /// </summary>
        /// <param name="edgeModel">The edge on which to insert a node.</param>
        /// <param name="nodeModel">The node to insert.</param>
        public SplitEdgeAndInsertExistingNodeCommand(IEdgeModel edgeModel, IInputOutputPortsNodeModel nodeModel) : this()
        {
            EdgeModel = edgeModel;
            NodeModel = nodeModel;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SplitEdgeAndInsertExistingNodeCommand command)
        {
            Assert.IsTrue(command.NodeModel.InputsById.Count > 0);
            Assert.IsTrue(command.NodeModel.OutputsById.Count > 0);

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var graphModel = graphModelState.GraphModel;
                var edgeInput = command.EdgeModel.ToPort;
                var edgeOutput = command.EdgeModel.FromPort;
                var deletedModels = graphModel.DeleteEdge(command.EdgeModel);
                var edge1 = graphModel.CreateEdge(edgeInput, command.NodeModel.OutputsByDisplayOrder.First(p => p?.PortType == edgeInput?.PortType));
                var edge2 = graphModel.CreateEdge(command.NodeModel.InputsByDisplayOrder.First(p => p?.PortType == edgeOutput?.PortType), edgeOutput);

                graphUpdater.MarkDeleted(deletedModels);
                graphUpdater.MarkNew(edge1);
                graphUpdater.MarkNew(edge2);
            }
        }
    }

    /// <summary>
    /// Command to convert edges to portal nodes.
    /// </summary>
    public class ConvertEdgesToPortalsCommand : UndoableCommand
    {
        const string k_UndoStringSingular = "Convert Edge to Portal";
        const string k_UndoStringPlural = "Convert Edges to Portals";

        static readonly Vector2 k_EntryPortalBaseOffset = Vector2.right * 75;
        static readonly Vector2 k_ExitPortalBaseOffset = Vector2.left * 250;
        const int k_PortalHeight = 24;

        /// <summary>
        /// Data describing which edge to transform and the position of the portals.
        /// </summary>
        public List<(IEdgeModel edge, Vector2 startPortPos, Vector2 endPortPos)> EdgeData;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertEdgesToPortalsCommand"/> class.
        /// </summary>
        public ConvertEdgesToPortalsCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertEdgesToPortalsCommand"/> class.
        /// </summary>
        /// <param name="edgeData">A list of tuple, each tuple containing the edge to convert, the position of the entry portal node and the position of the exit portal node.</param>
        public ConvertEdgesToPortalsCommand(IReadOnlyList<(IEdgeModel, Vector2, Vector2)> edgeData) : this()
        {
            EdgeData = edgeData?.ToList();
            UndoString = (EdgeData?.Count ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        // TODO JOCE: Move to GraphView or something. We should be able to create from edge without a command handler (for tests, for example)
        // TODO VladN: GTF-401 was created to address this
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, ConvertEdgesToPortalsCommand command)
        {
            if (command.EdgeData == null || !command.EdgeData.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            var createdElements = new List<IGraphElementModel>();
            using (var updater = graphModelState.UpdateScope)
            {
                var graphModel = graphModelState.GraphModel;
                var existingPortalEntries = new Dictionary<IPortModel, IEdgePortalEntryModel>();
                var existingPortalExits = new Dictionary<IPortModel, List<IEdgePortalExitModel>>();

                foreach (var edgeModel in command.EdgeData)
                    ConvertEdgeToPortals(edgeModel);

                // Adjust placement in case of multiple incoming exit portals so they don't overlap
                foreach (var portalList in existingPortalExits.Values.Where(l => l.Count > 1))
                {
                    var cnt = portalList.Count;
                    bool isEven = cnt % 2 == 0;
                    int offset = isEven ? k_PortalHeight / 2 : 0;
                    for (int i = (cnt - 1) / 2; i >= 0; i--)
                    {
                        portalList[i].Position = new Vector2(portalList[i].Position.x, portalList[i].Position.y - offset);
                        portalList[cnt - 1 - i].Position = new Vector2(portalList[cnt - 1 - i].Position.x, portalList[cnt - 1 - i].Position.y + offset);
                        offset += k_PortalHeight;
                    }
                }

                var edgesToDelete = command.EdgeData.Select(d => d.edge).ToList();
                var deletedModels = graphModel.DeleteEdges(edgesToDelete);
                updater.MarkDeleted(deletedModels);

                void ConvertEdgeToPortals((IEdgeModel edgeModel, Vector2 startPos, Vector2 endPos) data)
                {
                    // Only a single portal per output port. Don't recreate if we already created one.
                    var outputPortModel = data.edgeModel.FromPort;
                    IEdgePortalEntryModel portalEntry = null;
                    if (outputPortModel != null && !existingPortalEntries.TryGetValue(data.edgeModel.FromPort, out portalEntry))
                    {
                        portalEntry = graphModel.CreateEntryPortalFromEdge(data.edgeModel);
                        existingPortalEntries[outputPortModel] = portalEntry;
                        updater.MarkNew(portalEntry);
                        createdElements.Add(portalEntry);

                        if (!(outputPortModel.NodeModel is IInputOutputPortsNodeModel nodeModel))
                            return;

                        portalEntry.Position = data.startPos + k_EntryPortalBaseOffset;

                        // y offset based on port order. hurgh.
                        var idx = nodeModel.OutputsByDisplayOrder.IndexOfInternal(outputPortModel);
                        portalEntry.Position += Vector2.down * (k_PortalHeight * idx + 16); // Fudgy.

                        string portalName;
                        if (nodeModel is IConstantNodeModel constantNodeModel)
                            portalName = constantNodeModel.Type.FriendlyName();
                        else
                        {
                            portalName = (nodeModel as IHasTitle)?.Title ?? "";
                            var portName = (outputPortModel as IHasTitle)?.Title ?? "";
                            if (!string.IsNullOrEmpty(portName))
                                portalName += " - " + portName;
                        }

                        portalEntry.DeclarationModel = graphModel.CreateGraphPortalDeclaration(portalName);
                        updater.MarkNew(portalEntry.DeclarationModel);

                        var newEntryEdge = graphModel.CreateEdge(portalEntry.InputPort, outputPortModel);
                        updater.MarkNew(newEntryEdge);
                    }

                    // We can have multiple portals on input ports however
                    if (!existingPortalExits.TryGetValue(data.edgeModel.ToPort, out var portalExits))
                    {
                        portalExits = new List<IEdgePortalExitModel>();
                        existingPortalExits[data.edgeModel.ToPort] = portalExits;
                    }

                    IEdgePortalExitModel portalExit;
                    var inputPortModel = data.edgeModel.ToPort;
                    portalExit = graphModel.CreateExitPortalFromEdge(data.edgeModel);
                    portalExits.Add(portalExit);
                    updater.MarkNew(portalExit);
                    createdElements.Add(portalExit);

                    portalExit.Position = data.endPos + k_ExitPortalBaseOffset;
                    {
                        if (data.edgeModel.ToPort.NodeModel is IInputOutputPortsNodeModel nodeModel)
                        {
                            // y offset based on port order. hurgh.
                            var idx = nodeModel.InputsByDisplayOrder.IndexOfInternal(inputPortModel);
                            portalExit.Position += Vector2.down * (k_PortalHeight * idx + 16); // Fudgy.
                        }
                    }

                    portalExit.DeclarationModel = portalEntry?.DeclarationModel;

                    var newExitEdge = graphModel.CreateEdge(inputPortModel, portalExit.OutputPort);
                    updater.MarkNew(newExitEdge);
                }
            }

            if (createdElements.Any())
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection(graphModelState.GraphModel);
                    selectionUpdaters.MainUpdateScope.SelectElements(createdElements, true);
                }
            }
        }
    }
}
