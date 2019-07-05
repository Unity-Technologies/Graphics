using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Linq;
using UnityEngine.Profiling;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXBlockUI : VFXNodeUI
    {
        Toggle m_EnableToggle;

        public new VFXBlockController controller
        {
            get { return base.controller as VFXBlockController; }
            set { base.controller = value; }
        }

        public override VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController controller, VFXNodeUI node)
        {
            VFXContextDataAnchorController anchorController = controller as VFXContextDataAnchorController;

            VFXEditableDataAnchor anchor = VFXBlockDataAnchor.Create(anchorController, node);
            return anchor;
        }

        protected override bool HasPosition()
        {
            return false;
        }

        public VFXContextUI context
        {
            get { return this.GetFirstAncestorOfType<VFXContextUI>(); }
        }

        public VFXBlockUI()
        {
            Profiler.BeginSample("VFXBlockUI.VFXBlockUI");
            this.AddStyleSheetPath("VFXBlock");
            pickingMode = PickingMode.Position;
            m_EnableToggle = new Toggle();
            m_EnableToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleEnable);
            titleContainer.Insert(1, m_EnableToggle);

            capabilities &= ~Capabilities.Ascendable;
            capabilities |= Capabilities.Selectable | Capabilities.Droppable;
            this.AddManipulator(new SelectionDropper());

            Profiler.EndSample();
            style.position = PositionType.Relative;
        }

        // On purpose -- until we support Drag&Drop I suppose
        public override void SetPosition(Rect newPos)
        {
            style.position = PositionType.Relative;
        }

        void OnToggleEnable(ChangeEvent<bool> e)
        {
            controller.model.enabled = !controller.model.enabled;
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if (controller.model.enabled)
            {
                titleContainer.RemoveFromClassList("disabled");
            }
            else
            {
                titleContainer.AddToClassList("disabled");
            }

            m_EnableToggle.SetValueWithoutNotify(controller.model.enabled);
            if (inputContainer != null)
                inputContainer.SetEnabled(controller.model.enabled);
            if (settingsContainer != null)
                settingsContainer.SetEnabled(controller.model.enabled);

            if (!controller.model.isValid)
                AddToClassList("invalid");
            else
                RemoveFromClassList("invalid");
        }

        public override bool superCollapsed
        {
            get { return false; }
        }
    }
}
