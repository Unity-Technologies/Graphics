using System;

using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;
using UnityEngine.UIElements;

namespace UnityEditor.VFX
{
    [CustomEditor(typeof(VFXCustomAttributeDescriptor), true)]
    class VFXCustomAttributeDescriptorEditor : Editor
    {
        private TextField m_NameField;
        private EnumField m_TypeField;
        private TextField m_DescriptionField;

        public override VisualElement CreateInspectorGUI()
        {
            if (this.target is VFXCustomAttributeDescriptor customAttributeDescriptor)
            {
                var container = new VisualElement();
                container.SetEnabled(!customAttributeDescriptor.isReadOnly);

                this.m_NameField = new TextField("Name", 128, false, false, '*') { value = customAttributeDescriptor.attributeName, isDelayed = true };
                this.m_NameField.RegisterCallback<ChangeEvent<string>>(this.OnNameChanged, TrickleDown.TrickleDown);
                this.m_NameField.bindingPath = "m_AttributeName";
                container.Add(this.m_NameField);

                this.m_TypeField = new EnumField("Type", customAttributeDescriptor.type);
                this.m_TypeField.RegisterCallback<ChangeEvent<Enum>>(this.OnTypeChanged);
                this.m_TypeField.bindingPath = "m_Type";
                container.Add(this.m_TypeField);

                this.m_DescriptionField = new TextField("Description", 256, true, false, '*') { value = customAttributeDescriptor.description, isDelayed = true };
                this.m_DescriptionField.style.height = 62;
                this.m_DescriptionField.RegisterCallback<ChangeEvent<string>>(this.OnDescriptionChanged);
                this.m_DescriptionField.bindingPath = "m_Description";
                container.Add(this.m_DescriptionField);

                return container;
            }
            else
            {
                return new Label($"Selected object is not of type {nameof(VFXCustomAttributeDescriptor)}");
            }
        }

        private void OnDisable()
        {
            if (m_NameField != null)
            {
                this.m_NameField.UnregisterCallback<ChangeEvent<string>>(this.OnNameChanged);
                this.m_TypeField.UnregisterCallback<ChangeEvent<Enum>>(this.OnTypeChanged);
                this.m_DescriptionField.UnregisterCallback<ChangeEvent<string>>(this.OnDescriptionChanged);
            }
        }

        private void OnDescriptionChanged(ChangeEvent<string> evt)
        {
            this.Changed(m_NameField.value, m_NameField.value, (CustomAttributeUtility.Signature)m_TypeField.value, evt.newValue);
        }

        private void OnTypeChanged(ChangeEvent<Enum> evt)
        {
            this.Changed(m_NameField.value, m_NameField.value, (CustomAttributeUtility.Signature)evt.newValue, m_DescriptionField.value);
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            evt.StopImmediatePropagation();
            if (!this.Changed(evt.previousValue, evt.newValue, (CustomAttributeUtility.Signature)m_TypeField.value, m_DescriptionField.value))
            {
                m_NameField.SetValueWithoutNotify(evt.previousValue);
            }
        }

        private bool Changed(string oldName, string newName, CustomAttributeUtility.Signature newType, string newDescription)
        {
            var customAttributeDescriptor = (VFXCustomAttributeDescriptor)this.target;
            var hasChanged = customAttributeDescriptor.Changed(oldName, newName, newType, newDescription);
            if (hasChanged && VFXViewWindow.GetWindow(customAttributeDescriptor.graph) is { } view)
            {
                view.graphView.blackboard.UpdateCustomAttribute(oldName, newName);
            }

            return hasChanged;
        }
    }
}
