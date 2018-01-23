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

                slotContainer.controller.superCollapsed = !slotContainer.superCollapsed;
            }
        }
    }
    class VFXStandaloneSlotContainerUI : VFXNodeUI
    {
        public VFXStandaloneSlotContainerUI()
        {
            this.AddManipulator(new SuperCollapser());

            RegisterCallback<GeometryChangedEvent>(OnPostLayout);
        }

        void OnPostLayout(GeometryChangedEvent e)
        {
            float settingsLabelWidth = 30;
            float settingsControlWidth = 50;
            GetPreferedSettingsWidths(ref  settingsLabelWidth, ref settingsControlWidth);

            float labelWidth = 30;
            float controlWidth = 50;
            GetPreferedWidths(ref labelWidth, ref controlWidth);

            float newMinWidth = Mathf.Max(settingsLabelWidth + settingsControlWidth, labelWidth + controlWidth) + 20;

            if (this.style.minWidth != newMinWidth)
            {
                this.style.minWidth = newMinWidth;

                ApplySettingsWidths(settingsLabelWidth, settingsControlWidth);

                ApplyWidths(labelWidth, controlWidth);
            }
        }

        public override void ApplyWidths(float labelWidth, float controlWidth)
        {
            base.ApplyWidths(labelWidth, controlWidth);
            inputContainer.style.width = labelWidth + controlWidth + 20;
        }

        public bool superCollapsed
        {
            get { return controller.model.superCollapsed; }
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
            m_Middle = new VisualElement();
            m_Middle.name = "middle";
            inputContainer.parent.Insert(1, m_Middle);
        }

        VisualElement m_Middle;

        public new VFXOperatorController controller
        {
            get { return base.controller as VFXOperatorController; }
        }


        public override void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
            base.GetPreferedWidths(ref labelWidth, ref controlWidth);

            foreach (var port in GetPorts(true, false).Cast<VFXEditableDataAnchor>())
            {
                float portLabelWidth = port.GetPreferredLabelWidth() + 1;
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

        protected override void SelfChange()
        {
            base.SelfChange();

            bool hasMiddle = inputContainer.childCount != 0;
            if (hasMiddle)
            {
                if (m_Middle.parent == null)
                {
                    inputContainer.parent.Insert(1, m_Middle);
                }
            }
            else if (m_Middle.parent != null)
            {
                m_Middle.RemoveFromHierarchy();
            }
        }
    }
}
