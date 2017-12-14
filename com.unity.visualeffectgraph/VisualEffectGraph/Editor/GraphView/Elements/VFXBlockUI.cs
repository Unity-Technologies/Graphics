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
    class VFXBlockUI : VFXContextSlotContainerUI, IDropTarget
    {
        Toggle m_EnableToggle;

        public VFXBlockUI()
        {
            this.AddManipulator(new SelectionDropper(HandleDropEvent));

            pickingMode = PickingMode.Position;
            m_EnableToggle = new Toggle(OnToggleEnable);
            titleContainer.shadow.Insert(0, m_EnableToggle);

            capabilities &= ~Capabilities.Ascendable;
        }

        void OnToggleEnable()
        {
            var presenter = GetPresenter<VFXBlockPresenter>();

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
            var presenter = GetPresenter<VFXBlockPresenter>();

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

            VFXBlockPresenter blockPresenter = GetPresenter<VFXBlockPresenter>();
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
