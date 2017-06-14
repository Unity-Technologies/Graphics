using System.Collections.Generic;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXBlockUI : VFXContextSlotContainerUI, IDropTarget
    {
        Toggle m_EnableToggle;

        public VFXBlockUI()
        {
            AddManipulator(new SelectionDropper(HandleDropEvent));

            pickingMode = PickingMode.Position;
            m_EnableToggle = new Toggle(OnToggleEnable);
            titleContainer.InsertChild(0, m_EnableToggle);
        }

        void OnToggleEnable()
        {
            var presenter = GetPresenter<VFXBlockPresenter>();

            presenter.block.enabled = !presenter.block.enabled;
        }

        public override void OnSelected()
        {
        }

        // This function is a placeholder for common stuff to do before we delegate the action to the drop target
        private void HandleDropEvent(IMGUIEvent evt, List<ISelectable> selection, IDropTarget dropTarget)
        {
            if (dropTarget == null)
                return;

            switch ((EventType)evt.imguiEvent.type)
            {
                case EventType.DragUpdated:
                    dropTarget.DragUpdated(evt, selection, dropTarget);
                    break;
                case EventType.DragExited:
                    dropTarget.DragExited();
                    break;
                case EventType.DragPerform:
                    dropTarget.DragPerform(evt, selection, dropTarget);
                    break;
            }
        }

        public override void Select(GraphView selectionContainer, bool additive)
        {
            /*
            BlockContainer blockContainer = selectionContainer as BlockContainer;
            if (blockContainer == null || blockContainer != parent || !IsSelectable())
                return;

            // TODO: Get rid of this hack (parent.parent) to reach contextUI
            // Make sure we select the container context node
            var contextUI = blockContainer.parent.parent as VFXContextUI;
            if (contextUI != null)
            {
                var gView = this.GetFirstAncestorOfType<GraphView>();
                if (gView != null && !gView.selection.Contains(contextUI))
                {
                    gView.ClearSelection();
                    gView.AddToSelection(contextUI);
                }
            }

            if (blockContainer.selection.Contains(this))
            {
                if (evt.control)
                {
                    blockContainer.RemoveFromSelection(this);
                }
            }

            if (!evt.control)
                blockContainer.ClearSelection();
            blockContainer.AddToSelection(this);*/
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXBlockPresenter>();

            presenter.block.collapsed = !presenter.expanded;
            this.enabled = m_EnableToggle.on = presenter.block.enabled;
        }

        bool IDropTarget.CanAcceptDrop(List<ISelectable> selection)
        {
            return selection.Any(t => t is VFXBlockUI);
        }

        EventPropagation IDropTarget.DragUpdated(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            Vector2 pos = this.GlobalToBound(evt.imguiEvent.mousePosition);

            context.DraggingBlocks(selection.Select(t => t as VFXBlockUI).Where(t => t != null), this, pos.y > layout.height / 2);

            return EventPropagation.Stop;
        }

        EventPropagation IDropTarget.DragPerform(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            context.DragFinished();
            Vector2 pos = this.GlobalToBound(evt.imguiEvent.mousePosition);

            IEnumerable<VFXBlockUI> draggedBlocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);
            IEnumerable<VFXBlockPresenter> draggedBlocks = draggedBlocksUI.Select(t => t.GetPresenter<VFXBlockPresenter>());

            VFXBlockPresenter blockPresenter = GetPresenter<VFXBlockPresenter>();
            VFXContextPresenter contextPresenter = blockPresenter.contextPresenter;

            if (context.CanDrop(draggedBlocksUI, this))
            {
                contextPresenter.BlocksDropped(blockPresenter, pos.y > layout.height / 2, draggedBlocks);
            }
            else
            {
            }

            return EventPropagation.Stop;
        }

        EventPropagation IDropTarget.DragExited()
        {
            context.DragFinished();
            return EventPropagation.Stop;
        }
    }
}
