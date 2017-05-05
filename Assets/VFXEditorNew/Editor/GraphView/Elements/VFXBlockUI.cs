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
    class VFXBlockUI : VFXNodeUI, IDropTarget
    {
        public GraphViewTypeFactory typeFactory { get; set; }

        public VFXBlockUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Position;

            AddManipulator(new SelectionDropper(HandleDropEvent));
            leftContainer.alignContent = Align.Stretch;
        }

        // This function is a placeholder for common stuff to do before we delegate the action to the drop target
        private EventPropagation HandleDropEvent(Event evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            if (dropTarget == null)
                return EventPropagation.Continue;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    return dropTarget.DragUpdated(evt, selection, dropTarget);
                case EventType.DragExited:
                    return dropTarget.DragExited();
                case EventType.DragPerform:
                    return dropTarget.DragPerform(evt, selection, dropTarget);
            }

            return EventPropagation.Stop;
        }

        public override NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter presenter)
        {
            VFXBlockDataAnchorPresenter anchorPresenter = presenter as VFXBlockDataAnchorPresenter;

            VFXEditableDataAnchor anchor = VFXEditableDataAnchor.Create<VFXDataEdgePresenter>(anchorPresenter);

            anchorPresenter.sourceNode.viewPresenter.onRecompileEvent += anchor.OnRecompile;

            return anchor;
        }

        protected override void OnAnchorRemoved(NodeAnchor anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                GetPresenter<VFXParameterPresenter>().viewPresenter.onRecompileEvent += (anchor as VFXEditableDataAnchor).OnRecompile;
            }
        }

        public override EventPropagation Select(VisualContainer selectionContainer, Event evt)
        {
            BlockContainer blockContainer = selectionContainer as BlockContainer;
            if (blockContainer == null || blockContainer != parent || !IsSelectable())
                return EventPropagation.Continue;

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
                    return EventPropagation.Stop;
                }
                return EventPropagation.Continue;
            }

            if (!evt.control)
                blockContainer.ClearSelection();
            blockContainer.AddToSelection(this);

            // TODO: Reset to EventPropagation.Continue when Drag&Drop is supported
            return EventPropagation.Continue;
        }

        // On purpose -- until we support Drag&Drop I suppose
        public override void SetPosition(Rect newPos)
        {
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXBlockPresenter>();

            if (presenter == null)
                return;

            SetPosition(presenter.position);
            presenter.Model.collapsed = !presenter.expanded;
        }

        public override void DoRepaint(IStylePainter painter)
        {
            base.DoRepaint(painter);
        }

        bool IDropTarget.CanAcceptDrop(List<ISelectable> selection)
        {
            return selection.Any(t => t is VFXBlockUI);
        }

        EventPropagation IDropTarget.DragUpdated(Event evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            Vector2 pos = this.GlobalToBound(evt.mousePosition);

            context.DraggingBlocks(selection.Select(t => t as VFXBlockUI).Where(t => t != null), this, pos.y > position.height / 2);

            return EventPropagation.Stop;
        }

        EventPropagation IDropTarget.DragPerform(Event evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            context.DragFinished();
            Vector2 pos = this.GlobalToBound(evt.mousePosition);

            IEnumerable<VFXBlockUI> draggedBlocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);
            IEnumerable<VFXBlockPresenter> draggedBlocks = draggedBlocksUI.Select(t => t.GetPresenter<VFXBlockPresenter>());

            VFXBlockPresenter blockPresenter = GetPresenter<VFXBlockPresenter>();
            VFXContextPresenter contextPresenter = blockPresenter.ContextPresenter;

            if (context.CanDrop(draggedBlocksUI, this))
            {
                contextPresenter.BlocksDropped(blockPresenter, pos.y > position.height / 2, draggedBlocks);
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

        public VFXContextUI context
        {
            get {return this.GetFirstAncestorOfType<VFXContextUI>(); }
        }
    }
}
