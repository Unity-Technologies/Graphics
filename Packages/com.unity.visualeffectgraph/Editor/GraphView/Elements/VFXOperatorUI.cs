using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorUI : VFXNodeUI
    {
        VisualElement m_EditButton;
        VisualElement m_EditContainer;
        float m_LastExpendedWidth;

        public VFXOperatorUI()
        {
            this.AddStyleSheetPath("VFXOperator");

            m_EditButton = new VisualElement() { name = "edit" };
            m_EditButton.Add(new VisualElement() { name = "icon" });
            m_EditButton.AddManipulator(new Clickable(OnEdit));
            this.AddManipulator(new SuperCollapser());

            RegisterCallback<GeometryChangedEvent>(OnPostLayout);
        }

        void OnEdit()
        {
            if (m_EditContainer != null)
            {
                if (m_EditContainer.parent != null)
                {
                    m_EditContainer.RemoveFromHierarchy();
                    style.minWidth = 0;
                }
                else
                {
                    expanded = true;
                    RefreshPorts(); // refresh port to make sure outputContainer is added before the editcontainer.
                    topContainer.Add(m_EditContainer);
                }

                UpdateCollapse();
            }
        }

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
            inputContainer.style.width = labelWidth + controlWidth + 20;
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
            if (controller is VFXCascadedOperatorController)
            {
                var edit = new VFXCascadedOperatorEdit();
                edit.controller = controller as VFXCascadedOperatorController;
                return edit;
            }
            if (controller is VFXNumericUniformOperatorController)
            {
                var edit = new VFXUniformOperatorEdit<VFXNumericUniformOperatorController, VFXOperatorNumericUniform>();
                edit.controller = controller as VFXNumericUniformOperatorController;
                return edit;
            }
            if (controller is VFXDynamicTypeOperatorController)
            {
                var edit = new VFXUniformOperatorEdit<VFXDynamicTypeOperatorController, VFXOperatorDynamicType>();
                edit.controller = controller as VFXDynamicTypeOperatorController;
                return edit;
            }
            if (controller is VFXUnifiedOperatorController)
            {
                var edit = new VFXUnifiedOperatorEdit();
                edit.controller = controller as VFXUnifiedOperatorController;
                return edit;
            }
            return null;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this && controller != null && controller.model is VFXInlineOperator)
            {
                evt.menu.AppendAction("Convert to Exposed Property", OnConvertToExposedProperty, e => DropdownMenuAction.Status.Normal);
                evt.menu.AppendAction("Convert to Property", OnConvertToProperty, e => DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void OnConvertToProperty(DropdownMenuAction evt)
        {
            controller.ConvertToProperty(false);
        }

        void OnConvertToExposedProperty(DropdownMenuAction evt)
        {
            controller.ConvertToProperty(true);
        }

        public override bool superCollapsed
        {
            get { return base.superCollapsed && (m_EditContainer == null || m_EditContainer.parent == null); }
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if (isEditable)
            {
                if (m_EditButton.parent == null)
                    titleContainer.Insert(1, m_EditButton);

                if (m_EditContainer == null)
                {
                    m_EditContainer = GetControllerEditor();
                    if (m_EditContainer != null)
                        m_EditContainer.name = "edit-container";
                }
            }
            else
            {
                if (m_EditContainer != null && m_EditContainer.parent != null)
                    m_EditContainer.RemoveFromHierarchy();

                m_EditContainer = null;
                if (m_EditButton.parent != null)
                    m_EditButton.RemoveFromHierarchy();
            }

            if (!expanded && m_EditContainer != null && m_EditContainer.parent != null)
                m_EditContainer.RemoveFromHierarchy();
        }

        void OnPostLayout(GeometryChangedEvent e)
        {
            if (expanded)
            {
                m_LastExpendedWidth = layout.width;
            }
            RefreshLayout();
        }

        public override void RefreshLayout()
        {
            base.RefreshLayout();
            if (!superCollapsed)
            {
                var settingsLabelWidth = 30f;
                var settingsControlWidth = 50f;
                GetPreferedSettingsWidths(ref settingsLabelWidth, ref settingsControlWidth);

                var labelWidth = 30f;
                var controlWidth = 50f;
                GetPreferedWidths(ref labelWidth, ref controlWidth);

                ApplySettingsWidths(settingsLabelWidth, settingsControlWidth);

                ApplyWidths(labelWidth, controlWidth);

                // To prevent width to change between expanded and collapsed state
                // we set the minwidth to actual width before collapse, and reset to zero when expand
                // so that the expand/collapse button does not move
                if (!expanded)
                {
                    if (resolvedStyle.minWidth.value < m_LastExpendedWidth)
                    {
                        style.minWidth = m_LastExpendedWidth;
                    }

                    return;
                }
            }

            style.minWidth = 0f;
        }
    }
}
