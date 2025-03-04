using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine.Profiling;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXBlockUI : VFXNodeUI
    {
        public new VFXBlockController controller
        {
            get => base.controller as VFXBlockController;
            set => base.controller = value;
        }

        protected override bool HasPosition()
        {
            return false;
        }

        public VFXContextUI context => this.GetFirstAncestorOfType<VFXContextUI>();

        public VFXBlockUI()
        {
            Profiler.BeginSample("VFXBlockUI.VFXBlockUI");
            this.AddStyleSheetPath("VFXBlock");
            pickingMode = PickingMode.Position;

            capabilities &= ~Capabilities.Ascendable;
            capabilities |= Capabilities.Selectable | Capabilities.Droppable;
            this.AddManipulator(new SelectionDropper());

            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            Profiler.EndSample();
            style.position = PositionType.Relative;
        }

        // On purpose -- until we support Drag&Drop I suppose
        public override void SetPosition(Rect newPos)
        {
            style.position = PositionType.Relative;
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if (controller.model.enabled)
                RemoveFromClassList("block-disabled");
            else
                AddToClassList("block-disabled");

            if (!controller.model.isValid)
                AddToClassList("invalid");
            else
                RemoveFromClassList("invalid");
        }

        public override bool superCollapsed => false;

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var view = evt.originPanel.visualTree.Q<VFXView>();
            view?.blackboard?.ClearAllAttributesHighlights();
        }

        private void OnMouseHover(EventBase evt)
        {
            Profiler.BeginSample("VFXNodeUI.OnMouseOver");
            try
            {
                var view = GetFirstAncestorOfType<VFXView>();
                if (view != null)
                {
                    UpdateHover(view, evt.eventTypeId == MouseEnterEvent.TypeId());
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private void UpdateHover(VFXView view, bool isHovered)
        {
            var blackboard = view.blackboard;
            if (blackboard == null || controller.model == null)
                return;

            var attributes = controller.model is IVFXAttributeUsage attributeUsage
                ? attributeUsage.usedAttributes.Select(x => x.name)
                : controller.model.attributes.Select(x => x.attrib.name);

            foreach (var row in blackboard.GetAttributeRowsFromNames(attributes.ToArray()))
            {
                if (isHovered)
                    row.AddToClassList("hovered");
                else
                    row.RemoveFromClassList("hovered");
            }
        }
    }
}
