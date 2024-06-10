using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorUI : VFXNodeUI
    {
        private const float defaultOperatorLabelWidth = 10f;

        VisualElement m_EditButton;
        VisualElement m_EditContainer;
        float m_LastExpendedWidth;

        public VFXOperatorUI()
        {
            defaultLabelWidth = defaultOperatorLabelWidth;
            this.AddStyleSheetPath("VFXOperator");
            this.AddManipulator(new SuperCollapser());

            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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

            RefreshLayout();
        }

        public new VFXOperatorController controller => base.controller as VFXOperatorController;

        protected override void OnNewController()
        {
            base.OnNewController();
            if (isEditable)
            {
                m_EditButton = new Button(OnEdit) { name = "edit" };
                titleContainer.Insert(1, m_EditButton);

                m_EditContainer = GetControllerEditor();
                if (m_EditContainer != null)
                    m_EditContainer.name = "edit-container";
            }
        }

        private bool isEditable => controller != null && controller.isEditable;

        private VisualElement GetControllerEditor()
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
                evt.menu.AppendAction("Convert to Property", OnConvertToProperty, e => DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void OnConvertToProperty(DropdownMenuAction evt)
        {
            ConvertToProperty(false);
        }

        void OnConvertToExposedProperty(DropdownMenuAction evt)
        {
            ConvertToProperty(true);
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
                if (m_EditButton == null)
                {
                    m_EditButton = new VisualElement { name = "edit" };
                    m_EditButton.Add(new VisualElement { name = "icon" });
                    m_EditButton.AddManipulator(new Clickable(OnEdit));
                }

                if (m_EditButton.parent == null)
                {
                    var index = Math.Max(0, titleContainer.childCount - 1);
                    titleContainer.Insert(index, m_EditButton);
                }

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
                if (m_EditButton?.parent != null)
                    m_EditButton.RemoveFromHierarchy();
            }

            if (!expanded && m_EditContainer != null && m_EditContainer.parent != null)
                m_EditContainer.RemoveFromHierarchy();
        }

        protected override void OnPostLayout(GeometryChangedEvent e)
        {
            if (expanded)
            {
                m_LastExpendedWidth = layout.width;
            }
            base.OnPostLayout(e);
        }

        protected override void RefreshLayout()
        {
            base.RefreshLayout();
            // To prevent width to change between expanded and collapsed state
            // we set the minwidth to actual width before collapse, and reset to zero when expand
            // so that the expand/collapse button does not move
            if (!superCollapsed && !expanded)
            {
                if (resolvedStyle.minWidth.value < m_LastExpendedWidth)
                {
                    style.minWidth = m_LastExpendedWidth;
                }
                return;
            }

            style.minWidth = 0f;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var view = evt.originPanel.visualTree.Q<VFXView>();
            if (view != null)
            {
                UpdateHover(view, false);
            }
        }

        private void OnMouseHover(EventBase evt)
        {
            var view = GetFirstAncestorOfType<VFXView>();
            if (view != null)
            {
                UpdateHover(view, evt.eventTypeId == MouseEnterEvent.TypeId());
            }
        }

        private void UpdateHover(VFXView view, bool isHovered)
        {
            var blackboard = view.blackboard;
            if (blackboard == null)
                return;

            List<string> attributes = null;
            if (controller.model is IVFXAttributeUsage attributeUsage)
            {
                attributes = attributeUsage.usedAttributes.Select(x => x.name).ToList();
            }
            else if (controller.model is VFXSubgraphOperator subgraphOperator && subgraphOperator.subgraph.GetResource() is {} resource)
            {
                var usedSubgraph = resource.GetOrCreateGraph();

                attributes = usedSubgraph.customAttributes.Select(x => x.attributeName).ToList();
            }

            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    var row = blackboard.GetAttributeRowFromName(attribute);
                    if (row == null)
                        return;

                    if (isHovered)
                        row.AddToClassList("hovered");
                    else
                        row.RemoveFromClassList("hovered");
                }
            }
        }

        private void ConvertToProperty(bool exposed)
        {
            controller.ConvertToProperty(exposed);
            this.GetFirstAncestorOfType<VFXView>().blackboard.Update(true);
        }
    }
}
