using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to insert blocks into contexts.
    /// </summary>
    public class InsertBlocksInContextCommand : UndoableCommand
    {
        const string k_UndoString = "Insert Block";
        const string k_UndoStringPlural = "Insert Blocks";
        const string k_UndoStringDuplicate = "Duplicate Block";
        const string k_UndoStringDuplicatePlural = "Duplicate Blocks";

        /// <summary>
        /// The data needed per context to insert.
        /// </summary>
        public struct ContextData
        {
            /// <summary>
            /// The target context for the insertion.
            /// </summary>
            public IContextNodeModel Context { get; set; }

            /// <summary>
            /// The target index in the context for the insertion.
            /// </summary>
            public int Index { get; set; }

            /// <summary>
            /// The blocks to be inserted in the context.
            /// </summary>
            public IReadOnlyList<IBlockNodeModel> Blocks { get; set; }
        }

        public ContextData[] Data;
        public bool Duplicate;

        /// <summary>
        /// Initializes a new <see cref="InsertBlocksInContextCommand" />.
        /// </summary>
        /// <param name="context">The context in which to add the block.</param>
        /// <param name="index">The index in the context to which add the block.</param>
        /// <param name="blocks">The blocks to insert.</param>
        /// <param name="duplicate">If true the blocks will be duplicated before being inserted.</param>
        /// <param name="undoText">The undo string.</param>
        public InsertBlocksInContextCommand(IContextNodeModel context, int index, IEnumerable<IBlockNodeModel> blocks, bool duplicate = false, string undoText = null) :
            this(new[] { new ContextData { Context = context, Index = index, Blocks = blocks?.ToList() ?? Enumerable.Empty<IBlockNodeModel>().ToList() }}, duplicate, undoText)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="InsertBlocksInContextCommand" />.
        /// </summary>
        /// <param name="data">The data containing blocks and their target context.</param>
        /// <param name="duplicate">If true the blocks will be duplicated before being inserted.</param>
        /// <param name="undoText">The undo string.</param>
        public InsertBlocksInContextCommand(ContextData[] data, bool duplicate = true, string undoText = null)
        {
            if (data == null)
            {
                UndoString = k_UndoString;
                return;
            }

            if (undoText != null)
                UndoString = undoText;
            else if (data.Length > 1 || data.Length == 1 &&  data[0].Blocks.Count() > 1)
                UndoString = duplicate ? k_UndoStringDuplicatePlural : k_UndoStringPlural;
            else
                UndoString = duplicate ? k_UndoStringDuplicate : k_UndoString;

            Data = data;

            Duplicate = data.Length > 1 || duplicate;
        }

        /// <summary>
        /// Default command handler for InsertBlocksInContextCommand.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState, InsertBlocksInContextCommand command)
        {
            if (command.Data == null)
                return;

            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                var undoableStates = selectionHelper.UndoableSelectionStates.Append(graphModelState);
                undoStateUpdater.SaveStates(undoableStates.ToArray(), command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var selectionUpdater in selectionUpdaters)
                    {
                        selectionUpdater.ClearSelection(command.Data[0].Context.GraphModel);
                    }

                    foreach (var contextData in command.Data)
                    {
                        IEnumerable<IBlockNodeModel> newNodes;
                        if (!command.Duplicate)
                        {
                            foreach (var block in contextData.Blocks)
                            {
                                var context = block.ContextNodeModel;
                                if (context != null)
                                {
                                    graphUpdater.MarkChanged(context, ChangeHint.GraphTopology);
                                    context.RemoveElements(new[] {block});
                                }
                            }

                            newNodes = contextData.Blocks;
                        }
                        else
                        {
                            List<IBlockNodeModel> duplicatedNodes = new List<IBlockNodeModel>();
                            foreach (var block in contextData.Blocks)
                            {
                                var blockNodeModel = block.Clone();

                                blockNodeModel.AssignNewGuid();
                                blockNodeModel.ContextNodeModel = null;

                                duplicatedNodes.Add(blockNodeModel);
                            }

                            newNodes = duplicatedNodes;
                        }

                        int currentIndex = contextData.Index;
                        foreach (var block in newNodes)
                        {
                            if (block.IsCompatibleWith(contextData.Context))
                            {
                                contextData.Context.InsertBlock(block, currentIndex++);
                                graphUpdater.MarkNew(block);
                            }
                        }

                        if (command.Duplicate)
                        {
                            int cpt = 0;
                            foreach (var block in newNodes)
                            {
                                if (block.ContextNodeModel == contextData.Context)
                                    block.OnDuplicateNode(contextData.Blocks[cpt++]);
                                selectionUpdaters.MainUpdateScope.SelectElement(block, true);
                            }
                        }
                        else
                        {
                            //we need to update edges for non duplicated blocks. Duplicated blocks have no edge.
                            foreach (var block in newNodes)
                            {
                                foreach (var port in block.Ports)
                                {
                                    foreach (var edge in port.GetConnectedEdges())
                                    {
                                        graphUpdater.MarkChanged(edge, ChangeHint.GraphTopology);
                                    }
                                }
                            }
                        }

                        graphUpdater.MarkChanged(contextData.Context, ChangeHint.GraphTopology);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Graph block creation data used by the searcher.
    /// </summary>
    ///
    public readonly struct GraphBlockCreationData : IGraphNodeCreationData
    {
        /// <summary>
        /// The interface to the graph where we want the block to be created in.
        /// </summary>
        public IGraphModel GraphModel { get; }

        /// <summary>
        /// Unused for blocks.
        /// </summary>
        public Vector2 Position { get => Vector2.zero; }

        /// <summary>
        /// The flags specifying how the block is to be spawned.
        /// </summary>
        public SpawnFlags SpawnFlags { get; }

        /// <summary>
        /// The SerializableGUID to assign to the newly created item.
        /// </summary>
        public SerializableGUID Guid { get; }

        /// <summary>
        /// The Context in which the Block will be added.
        /// </summary>
        public IContextNodeModel ContextNodeModel { get; }

        /// <summary>
        /// The index of the position at which the Block will be added to the Context.
        /// </summary>
        public int OrderInContext { get; }

        /// <summary>
        /// Initializes a new GraphNodeCreationData.
        /// </summary>
        /// <param name="graphModel">The interface to the graph where we want the node to be created in.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="contextNodeModel">The Context in which the block will be added.</param>
        /// <param name="orderInContext">The index of the position at which the Block is to be added to the Context.</param>
        public GraphBlockCreationData(IGraphModel graphModel,
            SpawnFlags spawnFlags = SpawnFlags.Default,
            SerializableGUID guid = default,
            IContextNodeModel contextNodeModel = null,
            int orderInContext = -1)
        {
            GraphModel = graphModel;
            SpawnFlags = spawnFlags;
            Guid = guid;
            ContextNodeModel = contextNodeModel;
            OrderInContext = orderInContext;
        }
    }

    /// <summary>
    /// Command to create a block from a <see cref="GraphNodeModelSearcherItem"/>.
    /// </summary>
    public class CreateBlockFromSearcherCommand : UndoableCommand
    {
        const string k_UndoString = "Create Block";
        /// <summary>
        /// The searcher item representing the block to create.
        /// </summary>
        public GraphNodeModelSearcherItem SelectedItem;

        /// <summary>
        /// The SerializableGUID to assign to the newly created item.
        /// </summary>
        public SerializableGUID Guid;

        /// <summary>
        /// The Context in which the block will be added.
        /// </summary>
        public IContextNodeModel ContextNodeModel;

        /// <summary>
        /// The index of the position at which the block will be added to the Context.
        /// </summary>
        public int OrderInContext;

        /// <summary>
        /// Initializes a new <see cref="CreateBlockFromSearcherCommand"/>.
        /// </summary>
        CreateBlockFromSearcherCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Initializes a new CreateBlockFromSearcherCommand.
        /// </summary>
        /// <param name="selectedItem">The searcher item representing the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        /// <param name="contextNodeModel">The context in which to add the block.</param>
        /// <param name="orderInContext">The index of the position at which the Block is to be added to the Context.</param>
        public CreateBlockFromSearcherCommand(GraphNodeModelSearcherItem selectedItem,
                                             IContextNodeModel contextNodeModel = null,
                                              int orderInContext = -1,
                                              SerializableGUID guid = default) : this()
        {
            SelectedItem = selectedItem;
            Guid = guid.Valid ? guid : SerializableGUID.Generate();
            ContextNodeModel = contextNodeModel;
            OrderInContext = orderInContext;
        }

        /// <summary>
        /// Default command handler for CreateNodeFromSearcherCommand.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState, CreateBlockFromSearcherCommand command)
        {
            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                var undoableStates = selectionHelper.UndoableSelectionStates.Append(graphModelState);
                undoStateUpdater.SaveStates(undoableStates.ToArray(), command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var selectionUpdaters = selectionHelper.UpdateScopes)
            {
                foreach (var selectionUpdater in selectionUpdaters)
                {
                    selectionUpdater.ClearSelection(graphModelState.GraphModel);
                }

                var newModel = command.SelectedItem.CreateElement.Invoke(
                    new GraphBlockCreationData(graphModelState.GraphModel, guid: command.Guid, contextNodeModel: command.ContextNodeModel, orderInContext: command.OrderInContext));

                graphUpdater.MarkNew(newModel);
                selectionUpdaters.MainUpdateScope.SelectElements(new []{ newModel }, true);
            }
        }
    }
}
