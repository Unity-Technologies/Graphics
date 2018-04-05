using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorUI : VFXStandaloneSlotContainerUI
    {
        VisualElement m_EditButton;

        public VFXOperatorUI()
        {
            AddStyleSheetPath("VFXOperator");

            m_Middle = new VisualElement();
            m_Middle.name = "middle";
            inputContainer.parent.Insert(1, m_Middle);

            m_EditButton = new VisualElement() {name = "edit"};
            m_EditButton.Add(new VisualElement() { name = "icon" });
            m_EditButton.AddManipulator(new Clickable(OnEdit));
        }

        VisualElement m_EditContainer;

        void OnEdit()
        {
            if (m_EditContainer != null)
            {
                if (m_EditContainer.parent != null)
                {
                    m_EditContainer.RemoveFromHierarchy();
                }
                else
                {
                    topContainer.Add(m_EditContainer);
                }
                ForceRefreshLayout();
            }
        }

        public void ForceRefreshLayout()
        {
            (panel as BaseVisualElementPanel).ValidateLayout();
            RefreshLayout();
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

        public bool isEditable
        {
            get
            {
                return controller != null && controller.isEditable;
            }
        }

        protected VisualElement GetControllerEditor()
        {
            if( controller is VFXCascadedOperatorController)
            {
                var edit = new VFXMultiOperatorEdit();
                edit.controller = controller as VFXCascadedOperatorController;
                return edit;
            }
            if( controller is VFXUniformOperatorController)
            {
                var edit = new VFXUniformOperatorEdit();
                edit.controller = controller as VFXUniformOperatorController;
                return edit;
            }
            return null;
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

            if (isEditable)
            {
                VFXCascadedOperatorController cascadedController = controller as VFXCascadedOperatorController;

                if (m_EditButton.parent == null)
                {
                    titleContainer.Insert(1, m_EditButton);
                }
                if (m_EditContainer == null)
                {
                    m_EditContainer = GetControllerEditor();
                    if(m_EditContainer != null)
                        m_EditContainer.name = "edit-container";
                }
            }
            else
            {
                if (m_EditContainer != null && m_EditContainer.parent != null)
                {
                    m_EditContainer.RemoveFromHierarchy();
                }
                m_EditContainer = null;
                if (m_EditButton.parent != null)
                {
                    m_EditButton.RemoveFromHierarchy();
                }
            }
        }
    }
}
