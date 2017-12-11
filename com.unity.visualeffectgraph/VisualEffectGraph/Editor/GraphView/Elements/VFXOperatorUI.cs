using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class SuperCollapser : Manipulator
    {
        public SuperCollapser()
        {
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseUp);
        }

        void OnMouseUp(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                VFXStandaloneSlotContainerUI slotContainer = (VFXStandaloneSlotContainerUI)target;

                slotContainer.superCollapsed = !slotContainer.superCollapsed;
            }
        }
    }
    class VFXStandaloneSlotContainerUI : VFXNodeUI
    {
        public VFXStandaloneSlotContainerUI()
        {
            this.AddManipulator(new SuperCollapser());
        }

        protected override void OnStyleResolved(ICustomStyle style)
        {
            float settingsLabelWidth = 30;
            float settingsControlWidth = 110;
            GetPreferedSettingsWidths(ref  settingsLabelWidth, ref settingsControlWidth);

            ApplySettingsWidths(settingsLabelWidth, settingsControlWidth);
            this.style.minWidth = settingsLabelWidth + settingsControlWidth + 20;

            base.OnStyleResolved(style);

            float labelWidth = 30;
            float controlWidth = 110;

            GetPreferedWidths(ref labelWidth, ref controlWidth);

            ApplyWidths(labelWidth, controlWidth);
        }

        public override void ApplyWidths(float labelWidth, float controlWidth)
        {
            base.ApplyWidths(labelWidth, controlWidth);
            inputContainer.style.width = labelWidth + controlWidth + 20;
        }

        public bool superCollapsed
        {
            get { return controller.model.superCollapsed; }

            set
            {
                if (controller.model.superCollapsed != value)
                {
                    controller.model.superCollapsed = value;
                }
            }
        }
        protected override void SelfChange()
        {
            base.SelfChange();

            if (superCollapsed)
            {
                AddToClassList("superCollapsed");
            }
            else
            {
                RemoveFromClassList("superCollapsed");
            }
        }
    }


    class VFXOperatorUI : VFXStandaloneSlotContainerUI
    {
        public VFXOperatorUI()
        {
            VisualElement element = new VisualElement();
            element.name = "middle";
            inputContainer.parent.Insert(1, element);
        }

        public new VFXOperatorPresenter controller
        {
            get { return base.controller as VFXOperatorPresenter; }
        }


        public override void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
            base.GetPreferedWidths(ref labelWidth, ref controlWidth);

            foreach (var port in GetPorts(true, false).Cast<VFXEditableDataAnchor>())
            {
                float portLabelWidth = port.GetPreferredLabelWidth();
                float portControlWidth = port.GetPreferredControlWidth();

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
                if (controlWidth < portControlWidth)
                {
                    controlWidth = portControlWidth;
                }
            }
        }

        public override void ApplyWidths(float labelWidth, float controlWidth)
        {
            base.ApplyWidths(labelWidth, controlWidth);
            foreach (var port in GetPorts(true, false).Cast<VFXEditableDataAnchor>())
            {
                port.SetLabelWidth(labelWidth);
            }
        }
    }
}
