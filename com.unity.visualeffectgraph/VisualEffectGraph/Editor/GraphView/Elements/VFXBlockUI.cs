using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;
using UnityEngine.Profiling;

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
            Profiler.BeginSample("VFXBlockUI.VFXBlockUI");
            AddStyleSheetPath("VFXBlock");
            pickingMode = PickingMode.Position;
            m_EnableToggle = new Toggle();
            m_EnableToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleEnable);
            titleContainer.Insert(1, m_EnableToggle);

            capabilities &= ~Capabilities.Ascendable;
            capabilities |= Capabilities.Selectable;

            RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            Profiler.EndSample();
            style.positionType = PositionType.Relative;
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            style.positionType = PositionType.Relative;
        }

        void OnMouseDown(MouseDownEvent e)
        {
            VFXView view = this.GetFirstAncestorOfType<VFXView>();

            if (view != null)
            {
                bool combine = e.shiftKey || e.ctrlKey;
                if (view.selection.Contains(this))
                {
                    if (combine)
                    {
                        view.RemoveFromSelection(this);
                    }
                }
                else
                {
                    if (!combine)
                    {
                        view.ClearSelection();
                    }
                    view.AddToSelection(this);
                }
            }
        }

        void OnToggleEnable(ChangeEvent<bool> e)
        {
            controller.block.enabled = !controller.block.enabled;
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

            m_EnableToggle.SetValueWithoutNotify(controller.block.enabled);
            if (inputContainer != null)
                inputContainer.SetEnabled(controller.block.enabled);
            if (settingsContainer != null)
                settingsContainer.SetEnabled(controller.block.enabled);
        }
        public override bool superCollapsed
        {
            get { return false; }
        }
    }
}
