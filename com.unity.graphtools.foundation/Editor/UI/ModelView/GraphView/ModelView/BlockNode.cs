using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The class for block nodes UI.
    /// </summary>
    public class BlockNode : CollapsibleInOutNode
    {
        /// <summary>
        /// The USS class name used for blocks
        /// </summary>
        public new static readonly string ussClassName = "ge-block-node";

        static readonly string etchBorderName = "etch-over-border";
        static readonly string etchName = "etch";
        static readonly string etchBorderColorName = "etch-border-color";
        static readonly string etchColorName = "etch-color";

        /// <summary>
        /// Modifier class added to class list when the BlockNode has at least one output.
        /// </summary>
        public static readonly string hasOutputModifierUssClassName = ussClassName.WithUssModifier("has-output");

        /// <summary>
        /// The USS class name used for the etch element.
        /// </summary>
        public static readonly string etchUssClassName = ussClassName.WithUssElement(etchName);

        /// <summary>
        /// The USS class name used for the etch border element.
        /// </summary>
        public static readonly string etchBorderUssClassName = ussClassName.WithUssElement(etchBorderName);

        /// <summary>
        /// The USS class name used for the etch color element.
        /// </summary>
        public static readonly string etchColorUssClassName = ussClassName.WithUssElement(etchColorName);

        /// <summary>
        /// The USS class name used for the etch border color element.
        /// </summary>
        public static readonly string etchBorderColorUssClassName = ussClassName.WithUssElement(etchBorderColorName);

        /// <summary>
        /// The <see cref="IBlockNodeModel"/> this <see cref="BlockNode"/> displays.
        /// </summary>
        public IBlockNodeModel BlockNodeModel => Model as IBlockNodeModel;

        VisualElement m_EtchBorder;
        VisualElement m_Etch;
        VisualElement m_EtchBorderColor;
        VisualElement m_EtchColor;
        BlockDragInfos m_BlockDragInfos;


        internal VisualElement Etch => m_Etch;
        internal VisualElement EtchBorder => m_EtchBorder;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockNode" /> class.
        /// </summary>
        public BlockNode()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (e.button == (int)MouseButton.LeftMouse)
            {
            e.StopPropagation();
                m_BlockDragInfos = new BlockDragInfos(this);
                m_BlockDragInfos.DraggedBlockContext.RegisterCallback<KeyDownEvent>(OnDragKey);
                m_BlockDragInfos.DraggedBlockContext.RegisterCallback<MouseUpEvent>(OnMouseUp);
                m_BlockDragInfos.DraggedBlockContext.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                m_BlockDragInfos.OnMouseDown(e);
            }
        }

        void ClearDragging()
        {
            if (m_BlockDragInfos != null)
            {
                m_BlockDragInfos.DraggedBlockContext.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                m_BlockDragInfos.DraggedBlockContext.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                m_BlockDragInfos.DraggedBlockContext.UnregisterCallback<KeyDownEvent>(OnDragKey);
            }

            m_BlockDragInfos = null;
        }

        void OnDragKey(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape)
            {
                m_BlockDragInfos.ReleaseDragging();
                ClearDragging();
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (m_BlockDragInfos != null && !m_BlockDragInfos.OnMouseMove(e))
            {
                ClearDragging();
            }

            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (m_BlockDragInfos != null)
            {
                m_BlockDragInfos.OnMouseUp(e);
                ClearDragging();
            }
        }

        /// <inheritdoc/>
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            ContextNode context = GetFirstAncestorOfType<ContextNode>();
            if (context == null)
                return;
            evt.menu.AppendAction("Insert Block Before",
                action =>
                {
                    Vector2 mousePosition = action?.eventInfo?.mousePosition ?? evt.mousePosition;
                    context.DisplaySmartSearch(mousePosition, BlockNodeModel.GetIndex());
                });
            evt.menu.AppendAction("Insert Block After",
                action =>
                {
                    Vector2 mousePosition = action?.eventInfo?.mousePosition ?? evt.mousePosition;
                    context.DisplaySmartSearch(mousePosition, BlockNodeModel.GetIndex() + 1);
                });
            evt.menu.AppendSeparator();

            base.BuildContextualMenu(evt);
        }

        /// <inheritdoc/>
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            m_EtchBorder = new VisualElement() { name = etchBorderName };
            m_EtchBorder.AddToClassList(etchBorderUssClassName);
            hierarchy.Add(m_EtchBorder);

            m_Etch = new VisualElement() { name = etchName };
            m_Etch.AddToClassList(etchUssClassName);
            hierarchy.Add(m_Etch);

            m_EtchBorderColor = new VisualElement() { name = etchBorderColorName };
            m_EtchBorderColor.AddToClassList(etchBorderColorUssClassName);
            m_EtchBorder.Add(m_EtchBorderColor);

            m_EtchColor = new VisualElement() { name = etchColorName };
            m_EtchColor.AddToClassList(etchColorUssClassName);
            m_Etch.Add(m_EtchColor);

            Border.BringToFront();
        }

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            this.AddStylesheet("BlockNode.uss");
            AddToClassList(ussClassName);
        }

        /// <inheritdoc/>
        protected override void UpdateColorFromModel()
        {
            bool hasOutput = BlockNodeModel.OutputsByDisplayOrder.Any();

            if (NodeModel.HasUserColor && !BlockNodeModel.InputsByDisplayOrder.Any() &&
                !hasOutput)
            {
                m_EtchColor.style.backgroundColor = NodeModel.Color;
                m_EtchBorderColor.style.backgroundColor = NodeModel.Color;
            }
            else
            {
                if (hasOutput)
                    AddToClassList(hasOutputModifierUssClassName);
                else
                    RemoveFromClassList(hasOutputModifierUssClassName);
                m_EtchColor.style.backgroundColor = StyleKeyword.Null;
                m_EtchBorderColor.style.backgroundColor = StyleKeyword.Null;
            }
        }

        /// <inheritdoc/>
        public override void SetPositionOverride(Vector2 position)
        {
            //Setting the position of a BlockNode does nothing.
        }

        /// <inheritdoc/>
        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (!copyPasteData.nodes.All(t => t is IBlockNodeModel))
                return false;

            if (operation == PasteOperation.Duplicate)
            {
                // If we duplicate we want to duplicate each selected block in its own context.
                // Duplicated blocks are added after the last selected block of each context.

                var selection = GraphView.GetSelection();
                var groupedBlocks = selection.OfType<IBlockNodeModel>().GroupBy(t => t.ContextNodeModel);

                var contextDatas = new InsertBlocksInContextCommand.ContextData[groupedBlocks.Count()];

                int cpt = 0;
                foreach (var contextData in groupedBlocks)
                {
                    contextDatas[cpt++] = new InsertBlocksInContextCommand.ContextData()
                    {
                        Context = contextData.Key,
                        Blocks = contextData.ToList(),
                        Index = contextData.Max(t => t.GetIndex()) + 1
                    };
                }

                GraphView.Dispatch(new InsertBlocksInContextCommand(contextDatas, true, operationName));
            }
            else
            {
                // If we paste, we paste everything below the last selected block in the same context as this block.

                var selection = GraphView.GetSelection();
                var selectedBlocksInSameContext = selection.OfType<IBlockNodeModel>().Where(t => t.ContextNodeModel == BlockNodeModel.ContextNodeModel);
                var index = selectedBlocksInSameContext.Max(t => t.GetIndex()) + 1;

                var nodesToPaste = copyPasteData.nodes.OfType<IBlockNodeModel>();

                GraphView.Dispatch(new InsertBlocksInContextCommand(BlockNodeModel.ContextNodeModel, index, nodesToPaste, true, operationName));
            }

            return true;
        }

        /// <inheritdoc/>
        protected override DynamicBorder CreateDynamicBorder()
        {
            return new DynamicBlockBorder(this);
        }
    }
}
