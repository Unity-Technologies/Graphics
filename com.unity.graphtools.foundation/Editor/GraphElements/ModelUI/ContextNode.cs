using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// VisualElement to display a <see cref="IContextNodeModel"/>.
    /// </summary>
    public class ContextNode : CollapsibleInOutNode, IDisplaySmartSearchUI
    {
        /// <summary>
        /// The <see cref="IContextNodeModel"/> this ContextNode displays.
        /// </summary>
        public IContextNodeModel ContextNodeModel => Model as IContextNodeModel;

        /// <summary>
        /// The USS class name used for context nodes
        /// </summary>
        public new static readonly string ussClassName = "ge-context-node";

        /// <summary>
        /// The name of the part containing the blocks
        /// </summary>
        public static readonly string blocksPartName = "blocks-container";

        static readonly string contextBorderName = "context-border";
        static readonly string contextBorderTitleName = "context-border-title";

        /// <summary>
        /// The USS class name used for the context borders element.
        /// </summary>
        public static readonly string contextBorderUssClassName = ussClassName.WithUssElement(contextBorderName);

        /// <summary>
        /// The USS class name used for the title element in the context border.
        /// </summary>
        public static readonly string contextBorderTitleUssClassName = ussClassName.WithUssElement(contextBorderTitleName);

        /// <summary>
        /// The USS class name used for the context borders element when the drag is refused.
        /// </summary>
        public static readonly string contextBorderRefusedUssClassName = contextBorderUssClassName.WithUssModifier("refused");

        /// <summary>
        /// The USS class name used for the context borders element when the drag is accepted.
        /// </summary>
        public static readonly string contextBorderAcceptedUssClassName = contextBorderUssClassName.WithUssModifier("accepted");

        VisualElement m_ContextBorder;
        VisualElement m_ContextTitleBkgnd;

        VisualElement m_DragBlock;

        /// <summary>
        /// The root element of the context blocks.
        /// </summary>
        public VisualElement ContextBlocksRoot { get; private set; }

        /// <inheritdoc/>
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            var selectionBorder = this.SafeQ(selectionBorderElementName);
            var selectionBorderParent = selectionBorder.parent;

            //Move the selection border from being the entire container for the node to being on top of the context-border
            int cpt = 0;
            while (selectionBorder.childCount > 0)
            {
                var elementAt = selectionBorder.ElementAt(0);
                selectionBorderParent.hierarchy.Insert(cpt++, elementAt); // use hierarchy because selectionBorderParent has a content container defined
            }

            m_ContextBorder = new VisualElement { name = contextBorderName };
            m_ContextBorder.AddToClassList(contextBorderUssClassName);
            contentContainer.Insert(0, m_ContextBorder);
            m_ContextBorder.Add(selectionBorder);

            m_ContextTitleBkgnd = new VisualElement() { name = contextBorderTitleName };
            m_ContextTitleBkgnd.AddToClassList(contextBorderTitleUssClassName);
            m_ContextBorder.Add(m_ContextTitleBkgnd);

            m_DragBlock = new VisualElement() { name = "drag-block" };
        }

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            this.AddStylesheet("ContextNode.uss");
            AddToClassList(ussClassName);

            ContextBlocksRoot = PartList.GetPart(blocksPartName)?.Root;
        }

        /// <summary>
        /// Updates the Block color based its model.
        /// </summary>
        protected override void UpdateColorFromModel()
        {
            if (NodeModel.HasUserColor)
            {
                m_ContextTitleBkgnd.style.backgroundColor = NodeModel.Color;
            }
            else
            {
                m_ContextTitleBkgnd.style.backgroundColor = StyleKeyword.Null;
            }
        }

        /// <inheritdoc/>
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            EnableInClassList(ussClassName.WithUssModifier("no-vertical-input"), ContextNodeModel.InputsById.Values.All(t => t.Orientation != PortOrientation.Vertical));
            EnableInClassList(ussClassName.WithUssModifier("no-vertical-output"), ContextNodeModel.OutputsById.Values.All(t => t.Orientation != PortOrientation.Vertical));
        }

        /// <inheritdoc/>
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.InsertPartBefore(bottomPortContainerPartName, ContextBlocksPart.Create(blocksPartName, NodeModel, this, ussClassName));

            var titleBar = PartList.GetPart(titleIconContainerPartName) as IconTitleProgressPart;
            if (titleBar != null)
                titleBar.HasTitleColor = false;
        }

        internal void StartBlockDragging(float blocksHeight)
        {
            m_DragBlock.style.height = blocksHeight;
        }

        int GetBlockIndex(Vector2 posInContext)
        {
            var blockContainer = ContextBlocksRoot;
            if (blockContainer == null)
                return 0;

            var blocks = blockContainer.Children().OfType<BlockNode>().ToList();

            if (blocks.Count > 0)
            {
                var firstBlock = blocks.Last();

                int i = blocks.Count - 1;
                Rect firstLayout = firstBlock.parent.ChangeCoordinatesTo(this, firstBlock.layout);
                float y = firstLayout.y;
                for (; i >= 0; --i)
                {
                    float blockY = blocks[i].layout.height;
                    if (y + blockY * 0.5f > posInContext.y)
                        break;

                    y += blockY + blocks[i].resolvedStyle.marginTop + blocks[i].resolvedStyle.marginBottom;
                }

                return i + 1;
            }

            return 0;
        }

        internal void BlockDraggingRefused()
        {
            m_ContextBorder.AddToClassList(contextBorderRefusedUssClassName);
            m_ContextBorder.RemoveFromClassList(contextBorderAcceptedUssClassName);
        }

        internal void BlocksDragging(Vector2 posInContext, IEnumerable<IBlockNodeModel> blocks, bool copy)
        {
            var blockContainer = ContextBlocksRoot;
            if (blockContainer == null)
                return;

            m_ContextBorder.AddToClassList(contextBorderAcceptedUssClassName);
            m_ContextBorder.RemoveFromClassList(contextBorderRefusedUssClassName);

            int index = GetBlockIndex(posInContext);

            if (index >= blockContainer.childCount)
                blockContainer.Add(m_DragBlock);
            else
                blockContainer.Insert((index < 0 ? 0 : index) + 1, m_DragBlock);
        }

        internal void BlocksDropped(Vector2 posInContext, IEnumerable<IBlockNodeModel> blocks, bool copy)
        {
            int index = GetBlockIndex(posInContext);

            int realIndex = ContextNodeModel.GraphElementModels.Count() - index - (copy ? 0 : blocks.Count(t => t.ContextNodeModel == ContextNodeModel));

            GraphView.Dispatch(new InsertBlocksInContextCommand(ContextNodeModel, realIndex, blocks, copy));

            StopBlockDragging();
        }

        internal void StopBlockDragging()
        {
            m_ContextBorder.RemoveFromClassList(contextBorderAcceptedUssClassName);
            m_ContextBorder.RemoveFromClassList(contextBorderRefusedUssClassName);
            m_DragBlock.RemoveFromHierarchy();
        }

        /// <inheritdoc/>
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if ((evt.target as VisualElement)?.GetFirstOfType<BlockNode>() == null)
            {
                evt.menu.AppendAction("Create Block",
                    action =>
                    {
                        Vector2 mousePosition = action?.eventInfo?.mousePosition ?? evt.mousePosition;
                        DisplaySmartSearch(mousePosition);
                    });
            }
        }

        /// <inheritdoc/>
        public virtual bool DisplaySmartSearch(Vector2 mousePosition)
        {
            var posInContext = this.WorldToLocal(mousePosition);

            int index = GetBlockIndex(posInContext);

            int realIndex = ContextNodeModel.GraphElementModels.Count() - index;

            return DisplaySmartSearch(mousePosition, realIndex);
        }

        /// <summary>
        /// Display the searcher for insertion of a new block at the given index.
        /// </summary>
        /// <param name="mousePosition">The mouse position in window coordinates.</param>
        /// <param name="index">The index in the context at which the new block will be added.</param>
        /// <returns>True if the searcher could be displayed.</returns>
        internal bool DisplaySmartSearch(Vector2 mousePosition, int index)
        {
            var stencil = (Stencil)GraphView.GraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetContextSearcherFilter(ContextNodeModel);
            var adapter = stencil.GetSearcherAdapter(GraphView.GraphModel, "Add a block", GraphView.GraphTool.Name);
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return false;

            var dbs = dbProvider.GetGraphElementContainerSearcherDatabases(GraphView.GraphModel, ContextNodeModel);
            if (dbs == null)
                return false;

            SearcherService.ShowSearcher(GraphView.GraphTool.Preferences, mousePosition, item =>
            {
                GraphView.Dispatch(new CreateBlockFromSearcherCommand(item, ContextNodeModel, index));
            }, dbs, filter, adapter, "create-blocks");

            return true;
        }

        /// <inheritdoc/>
        public override bool PasteIn(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (!copyPasteData.nodes.All(t => t is IBlockNodeModel))
                return false;

            GraphView.Dispatch(new InsertBlocksInContextCommand(ContextNodeModel,
                -1,
                copyPasteData.nodes.OfType<IBlockNodeModel>().ToList(), true, operationName));

            return true;
        }
    }
}
