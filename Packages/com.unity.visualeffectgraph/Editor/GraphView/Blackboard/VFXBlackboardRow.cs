using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXBlackboardRow : BlackboardRow, IControlledElement<VFXParameterController>
    {
        private readonly IParameterItem m_Property;

        private readonly VFXBlackboardField m_Field;
        private readonly VFXBlackboardPropertyView m_Properties;

        private VFXParameterController m_Controller;
        private int m_CurrentOrder;
        private bool m_CurrentExposed;

        public VFXBlackboardRow(PropertyItem property, VFXParameterController controller) : this(new VFXBlackboardField(property) { name = "vfx-field" }, new VFXBlackboardPropertyView { name = "vfx-properties" })
        {
            m_Property = property;
            this.Q<TemplateContainer>().pickingMode = PickingMode.Ignore;
            var button = this.Q<Button>("expandButton");
            if (button != null)
            {
                button.clickable.clicked += OnExpand;
            }

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            this.controller = controller;
        }

        void OnExpand()
        {
            controller.expanded = expanded;
        }

        public VFXBlackboardField field => m_Field;

        private VFXBlackboardRow(VFXBlackboardField field, VFXBlackboardPropertyView property) : base(field, property)
        {
            m_Field = field;
            m_Properties = property;

            m_Field.owner = this;
            m_Properties.owner = this;
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            // if the order or exposed change, let the event be caught by the VFXBlackboard
            if (controller.order == m_CurrentOrder && controller.exposed == m_CurrentExposed)
            {
                e.StopPropagation();
            }
            m_CurrentOrder = controller.order;
            m_CurrentExposed = controller.exposed;

            expanded = controller.expanded;

            m_Properties.SelfChange(e.change);

            m_Field.SelfChange();
            RemoveFromClassList("hovered");
        }

        Controller IControlledElement.controller => m_Controller;

        public VFXParameterController controller
        {
            get => m_Controller;
            private set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                        m_Controller.UnregisterHandler(m_Properties);
                    }
                    m_Controller = value;
                    m_Properties.Clear();

                    if (m_Controller != null)
                    {
                        m_CurrentOrder = m_Controller.order;
                        m_CurrentExposed = m_Controller.exposed;
                        m_Controller.RegisterHandler(this);
                        m_Controller.RegisterHandler(m_Properties);
                    }
                }
            }
        }

        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this)
            {
                evt.menu.AppendAction("Rename", (a) => Rename(), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Duplicate %d", (a) => Duplicate(), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Delete", (a) => Delete(), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Copy Name", (a) => CopyName(), DropdownMenuAction.AlwaysEnabled);

                evt.StopPropagation();
            }
        }

        private void CopyName()
        {
            EditorGUIUtility.systemCopyBuffer = m_Property.title;
        }

        private void Delete()
        {
            this.GetFirstAncestorOfType<VFXView>().Delete();
        }

        private void Duplicate()
        {
            GetFirstAncestorOfType<VFXView>().DuplicateSelectionCallback();
        }

        private void Rename()
        {
            this.Q<VFXBlackboardField>().OpenTextEditor();
        }
    }
}
