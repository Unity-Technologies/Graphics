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
    class VFXStandaloneSlotContainerUI : VFXNodeUI
    {
        public VFXStandaloneSlotContainerUI()
        {
            this.AddManipulator(new SuperCollapser());

            RegisterCallback<GeometryChangedEvent>(OnPostLayout);
        }

        void OnPostLayout(GeometryChangedEvent e)
        {
            RefreshLayout();
        }

        public override void RefreshLayout()
        {
            base.RefreshLayout();
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
            }

            ApplySettingsWidths(settingsLabelWidth, settingsControlWidth);

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
}
