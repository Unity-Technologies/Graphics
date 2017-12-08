using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXBlockUI : VFXContextSlotContainerUI, IDropTarget, IEdgeDrawerContainer
    {
        Toggle m_EnableToggle;

        public new VFXBlockPresenter controller
        {
            get { return base.controller as VFXBlockPresenter; }
            set { base.controller = value; }
        }


        void IEdgeDrawerContainer.EdgeDirty()
        {
            VFXContextUI contextUI = GetFirstAncestorOfType<VFXContextUI>();

            if (contextUI != null)
                (contextUI as IEdgeDrawerContainer).EdgeDirty();
        }

        public VFXBlockUI()
        {
            this.AddManipulator(new SelectionDropper(HandleDropEvent));

            pickingMode = PickingMode.Position;
            m_EnableToggle = new Toggle(OnToggleEnable);
            titleContainer.shadow.Insert(0, m_EnableToggle);


            //this.AddManipulator(new Collapser());
            capabilities &= ~Capabilities.Ascendable;

            edgeDrawer.RemoveFromHierarchy();
        }

        void OnToggleEnable()
        {
            var presenter = controller;

            presenter.block.enabled = !presenter.block.enabled;
        }

        // This function is a placeholder for common stuff to do before we delegate the action to the drop target
        private void HandleDropEvent(IMGUIEvent evt, List<ISelectable> selection, IDropTarget dropTarget)
        {
            if (dropTarget == null)
                return;

            switch ((EventType)evt.imguiEvent.type)
            {
                case EventType.DragUpdated:
                {
                    Vector2 savedPos = evt.imguiEvent.mousePosition;
                    evt.imguiEvent.mousePosition = this.ChangeCoordinatesTo(dropTarget as VisualElement, evt.imguiEvent.mousePosition);
                    dropTarget.DragUpdated(evt, selection, dropTarget);
                    evt.imguiEvent.mousePosition = savedPos;
                }
                break;
                case EventType.DragExited:
                    dropTarget.DragExited();
                    break;
                case EventType.DragPerform:
                {
                    Vector2 savedPos = evt.imguiEvent.mousePosition;
                    evt.imguiEvent.mousePosition = this.ChangeCoordinatesTo(dropTarget as VisualElement, evt.imguiEvent.mousePosition);
                    dropTarget.DragPerform(evt, selection, dropTarget);
                    evt.imguiEvent.mousePosition = savedPos;
                }
                break;
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = controller;

            presenter.block.collapsed = !presenter.expanded;
            m_EnableToggle.on = presenter.block.enabled;
            if (inputContainer != null)
                inputContainer.SetEnabled(presenter.block.enabled);
            if (m_SettingsContainer != null)
                m_SettingsContainer.SetEnabled(presenter.block.enabled);
        }

        bool IDropTarget.CanAcceptDrop(List<ISelectable> selection)
        {
            return selection.Any(t => t is VFXBlockUI);
        }

        EventPropagation IDropTarget.DragUpdated(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            Vector2 pos = evt.imguiEvent.mousePosition;

            context.DraggingBlocks(selection.Select(t => t as VFXBlockUI).Where(t => t != null), this, pos.y > layout.height / 2);

            return EventPropagation.Stop;
        }

        EventPropagation IDropTarget.DragPerform(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            context.DragFinished();
            Vector2 pos = evt.imguiEvent.mousePosition;

            IEnumerable<VFXBlockUI> draggedBlocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);

            VFXBlockPresenter blockPresenter = controller;
            VFXContextPresenter contextPresenter = blockPresenter.contextPresenter;

            if (context.CanDrop(draggedBlocksUI, this))
            {
                context.BlocksDropped(blockPresenter, pos.y > layout.height / 2, draggedBlocksUI, evt.imguiEvent.control);
                DragAndDrop.AcceptDrag();
            }
            else
            {
            }

            return EventPropagation.Stop;
        }

        EventPropagation IDropTarget.DragExited()
        {
            //context.DragFinished();
            return EventPropagation.Stop;
        }
    }
}
