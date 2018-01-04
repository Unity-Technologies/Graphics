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
    class VFXBlockUI : VFXContextSlotContainerUI
    {
        Toggle m_EnableToggle;

        public new VFXBlockController controller
        {
            get { return base.controller as VFXBlockController; }
            set { base.controller = value; }
        }

        public VFXBlockUI()
        {
            pickingMode = PickingMode.Position;
            m_EnableToggle = new Toggle(OnToggleEnable);
            titleContainer.shadow.Insert(1, m_EnableToggle);

            capabilities &= ~Capabilities.Ascendable;
            capabilities |= Capabilities.Selectable;

            RegisterCallback<MouseDownEvent>(OnMouseDown, Capture.Capture);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            VFXView view = this.GetFirstAncestorOfType<VFXView>();

            if (view != null)
            {
                if (!e.shiftKey && !e.ctrlKey)
                {
                    view.ClearSelection();
                }
                if (IsSelected(view))
                {
                    view.RemoveFromSelection(this);
                }
                else
                    view.AddToSelection(this);
            }
        }

        void OnToggleEnable()
        {
            controller.block.enabled = !controller.block.enabled;
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
                    evt.imguiEvent.mousePosition = (dropTarget as VisualElement).WorldToLocal(evt.originalMousePosition);
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
                    evt.imguiEvent.mousePosition = (dropTarget as VisualElement).WorldToLocal(evt.originalMousePosition);
                    dropTarget.DragPerform(evt, selection, dropTarget);
                    evt.imguiEvent.mousePosition = savedPos;
                }
                break;
            }
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if (controller.block.enabled)
            {
                titleContainer.RemoveFromClassList("disabled");
            }
            else
            {
                titleContainer.AddToClassList("disabled");
            }

            m_EnableToggle.on = controller.block.enabled;
            if (inputContainer != null)
                inputContainer.SetEnabled(controller.block.enabled);
            if (m_SettingsContainer != null)
                m_SettingsContainer.SetEnabled(controller.block.enabled);
        }
    }
}
