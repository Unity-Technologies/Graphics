using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class BlockDragInfos
    {
        static readonly List<VisualElement> k_PickedElements = new List<VisualElement>();

        const float k_DragThresholdSquare = 6 * 6;
        Vector2 StartDrag { get; set; }
        Vector2 DragPosition { get; set; }
        bool Dragging { get; set; }
        public GraphView GraphView { get; }
        ContextNode HoveredContext { get; set; }
        ModelUI DraggedBlock { get; set; }
        internal ModelUI DraggedBlockContext { get; set; }
        IEnumerable<IBlockNodeModel> SelectedBlockModels { get; set; }
        List<ModelUI> SelectedBlocks { get; set; }
        public int DraggedBlockIndex { get; set; }

        bool m_Duplicate;
        float m_DraggedHeight;

        public BlockDragInfos(BlockNode draggedBlock)
        {
            GraphView = draggedBlock.GraphView;
            DraggedBlock = draggedBlock;
            DraggedBlockContext = draggedBlock.BlockNodeModel.ContextNodeModel.GetUI<ContextNode>(GraphView);
        }

        public void OnMouseDown(MouseDownEvent e)
        {
            StartDrag = e.mousePosition;
            DragPosition = e.localMousePosition;
            DraggedBlockContext.CaptureMouse();
        }

        public bool OnMouseMove(MouseMoveEvent e)
        {
            //If the LeftButton is no longer pressed.
            if ((e.pressedButtons & 1 << ((int)MouseButton.LeftMouse)) == 0)
            {
                ReleaseDragging();
                return false;
            }

            Vector2 graphPosition = GraphView.ContentViewContainer.WorldToLocal(e.mousePosition);
            if (!Dragging)
            {
                if ((e.mousePosition - StartDrag).sqrMagnitude > k_DragThresholdSquare)
                {
                    Dragging = true;

                    IEnumerable<IBlockNodeModel> selectedBlockModels = GraphView.GetSelection().OfType<IBlockNodeModel>();

                    if (selectedBlockModels.Contains(DraggedBlock.Model))
                        SelectedBlockModels = selectedBlockModels;
                    else
                        SelectedBlockModels = Enumerable.Repeat((IBlockNodeModel)DraggedBlock.Model, 1);

                    SelectedBlocks = new List<ModelUI>();

                    m_Duplicate = (e.modifiers & EventModifiers.Alt) != 0;

                    IGraphElementModel mainNodeModel = DraggedBlock.Model;

                    m_DraggedHeight = 0;
                    if (m_Duplicate)
                    {
                        foreach (var block in SelectedBlockModels)
                        {
                            var blockUI = block.GetUI<ModelUI>(GraphView);
                            if (blockUI == null)
                                continue;

                            INodeModel duplicateNodeModel = block.Clone();

                            duplicateNodeModel.AssetModel = GraphView.GraphModel.AssetModel;
                            duplicateNodeModel.AssignNewGuid();
                            duplicateNodeModel.OnDuplicateNode(block);

                            m_DraggedHeight += blockUI.layout.height;
                            ModelUI newBlockUI = GraphElementFactory.CreateUI<ModelUI>(GraphView, duplicateNodeModel);

                            GraphView.ContentViewContainer.Add(newBlockUI);
                            SelectedBlocks.Add(newBlockUI);
                            if (block == DraggedBlock.Model)
                                mainNodeModel = duplicateNodeModel;

                            newBlockUI.style.width = blockUI.layout.width;
                            newBlockUI.style.position = Position.Absolute;
                        }
                    }
                    else
                    {
                        mainNodeModel = DraggedBlock.Model;
                        foreach (var block in SelectedBlockModels)
                        {
                            var blockUI = block.GetUI<ModelUI>(GraphView);
                            if (blockUI == null)
                                continue;
                            GraphView.ContentViewContainer.Add(blockUI);
                            SelectedBlocks.Add(blockUI);

                            m_DraggedHeight += blockUI.layout.height;

                            blockUI.style.width = blockUI.layout.width;
                            blockUI.style.position = Position.Absolute;
                        }
                    }

                    SelectedBlocks.Sort((a, b) =>
                    {
                        IBlockNodeModel aBlockModel = (IBlockNodeModel)a.Model;
                        IBlockNodeModel bBlockModel = (IBlockNodeModel)b.Model;
                        IBlockNodeModel draggedBlockModel = (IBlockNodeModel)DraggedBlock.Model;
                        // block in the same context as the one dragged go first
                        if (aBlockModel.ContextNodeModel == bBlockModel.ContextNodeModel)
                            return aBlockModel.GetIndex() - bBlockModel.GetIndex();
                        if (aBlockModel.ContextNodeModel == draggedBlockModel.ContextNodeModel)
                            return -1;
                        if (bBlockModel.ContextNodeModel == draggedBlockModel.ContextNodeModel)
                            return 1;
                        return (int)Mathf.Sign(a.worldBound.y - b.worldBound.y);
                    });

                    DraggedBlockIndex = SelectedBlocks.FindIndex(t => t.Model == mainNodeModel);
                    if (m_Duplicate)
                    {
                        DraggedBlock = SelectedBlocks[DraggedBlockIndex];
                    }
                }
            }
            if (Dragging)
            {
                Vector2 myPos = new Vector2(graphPosition.x - DragPosition.x, graphPosition.y - DragPosition.y);
                DraggedBlock.style.left = myPos.x;
                DraggedBlock.style.top = myPos.y;

                int myIndex = DraggedBlockIndex;

                float positionY = myPos.y;
                for (int i = myIndex - 1; i >= 0; --i)
                {
                    SelectedBlocks[i].style.left = myPos.x;
                    float height = SelectedBlocks[i].layout.height;
                    positionY -= height;
                    SelectedBlocks[i].style.top = positionY;
                }

                positionY = myPos.y + DraggedBlock.layout.height;
                for (int i = myIndex + 1; i < SelectedBlocks.Count; ++i)
                {
                    SelectedBlocks[i].style.left = myPos.x;
                    SelectedBlocks[i].style.top = positionY;
                    float height = SelectedBlocks[i].layout.height;
                    positionY += height;
                }

                k_PickedElements.Clear();
                DraggedBlock.panel.PickAll(e.mousePosition, k_PickedElements);

                bool found = false;
                foreach (var element in k_PickedElements)
                {
                    if (element is ContextNode context)
                    {
                        found = true;
                        if (HoveredContext != context)
                        {
                            HoveredContext?.StopBlockDragging();
                            HoveredContext = context;
                            HoveredContext.StartBlockDragging(m_DraggedHeight);
                        }
                        Vector2 posInContext = context.WorldToLocal(e.mousePosition);

                        if (SelectedBlockModels.All(t => t.IsCompatibleWith(HoveredContext.ContextNodeModel)))
                            HoveredContext.BlocksDragging(posInContext, SelectedBlockModels, m_Duplicate);
                        else
                            HoveredContext.BlockDraggingRefused();
                        break;
                    }
                }

                if (!found && HoveredContext != null)
                {
                    HoveredContext.StopBlockDragging();
                    HoveredContext = null;
                }
            }
            e.StopPropagation();
            return true;
        }

        public void OnMouseUp(MouseUpEvent e)
        {
            if (Dragging)
            {
                k_PickedElements.Clear();
                DraggedBlock.panel.PickAll(e.mousePosition, k_PickedElements);

                foreach (var element in k_PickedElements)
                {
                    if (element is ContextNode context)
                    {
                        Vector2 posInContext = context.WorldToLocal(e.mousePosition);
                        if (SelectedBlockModels.All(t => t.IsCompatibleWith(HoveredContext.ContextNodeModel)))
                        {
                            foreach (var block in SelectedBlocks)
                            {
                                block.RemoveFromHierarchy();
                                block.RemoveFromView();
                            }
                            HoveredContext.BlocksDropped(posInContext, SelectedBlockModels, m_Duplicate);
                            HoveredContext = null;
                            SelectedBlocks.Clear(); //Clean so that ReleaseDragging do not put them back in the original context
                        }
                        break;
                    }
                }
            }
            ReleaseDragging();
        }

        public void ReleaseDragging()
        {
            if (Dragging)
            {
                HoveredContext?.StopBlockDragging();

                var contextsToUpdate = new HashSet<ContextNode>();

                using (var updater = GraphView.GraphViewState.UpdateScope)
                {
                    if (SelectedBlocks != null)
                    {
                        if (m_Duplicate)
                        {
                            foreach (var block in SelectedBlocks)
                            {
                                block.RemoveFromHierarchy();
                                ((INodeModel)block.Model).Destroy();
                            }
                        }
                        else
                        {
                            foreach (var block in SelectedBlocks)
                            {
                                var context =
                                    ((IBlockNodeModel)block.Model).ContextNodeModel.GetUI<ContextNode>(GraphView);
                                if (context == null)
                                    continue;
                                if (context.ContextBlocksRoot == null)
                                    continue;
                                context.ContextBlocksRoot.Add(block);
                                updater.MarkChanged(context.Model);

                                block.style.position = new StyleEnum<Position>(StyleKeyword.Null);
                                block.style.width = new StyleLength(StyleKeyword.Null);
                            }
                        }
                    }
                }

                Dragging = false;
            }

            if (HoveredContext != null)
            {
                HoveredContext.StopBlockDragging();
                HoveredContext = null;
            }
            DraggedBlockContext.ReleaseMouse();
        }
    }
}
