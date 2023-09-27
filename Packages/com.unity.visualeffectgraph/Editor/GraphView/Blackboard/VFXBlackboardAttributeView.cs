using System;
using UnityEditor.VFX.Block;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXBlackboardAttributeView : VisualElement
    {
        private readonly AttributeItem m_AttributeItem;
        private readonly TextField m_DescriptionField;

        public VFXBlackboardAttributeView(AttributeItem attributeItem)
        {
            this.m_AttributeItem = attributeItem;
            VisualElement typeField = this.m_AttributeItem.isEditable
                ? new EnumField("Type", this.m_AttributeItem.type) { name = "type" }
                : new TextField("Type") { name = "typeLabel", value = this.m_AttributeItem.type.ToString(), focusable = false };
            Add(typeField);

            if (attributeItem.isBuiltIn)
            {
                TextField readOnlyField = new TextField("Read-Only") { name = "readonly", isReadOnly = true, focusable = false };
                readOnlyField.value = this.m_AttributeItem.isReadOnly ? "Yes" : "No";
                Add(readOnlyField);
            }

            if (attributeItem.subgraphUse?.Length > 0)
            {
                var subgraphUseText = new TextField("Used by sub-graphs") { value = string.Join(", ", attributeItem.subgraphUse), isReadOnly = true, name = "UsedBySubgraph"};
                subgraphUseText.AddToClassList("read-only");
                Add(subgraphUseText);
            }

            this.m_DescriptionField = new TextField("Description") { name = "description", isDelayed = true };
            this.m_DescriptionField.value = this.m_AttributeItem.description;
            this.m_DescriptionField.maxLength = 256;
            this.m_DescriptionField.multiline = true;
            this.m_DescriptionField.isReadOnly = !this.m_AttributeItem.isEditable;
            if (this.m_AttributeItem.isEditable)
            {
                typeField.RegisterCallback<ChangeEvent<Enum>>(OnTypeChange);
                this.m_DescriptionField.RegisterCallback<ChangeEvent<string>>(OnDescriptionChanged);
            }
            else
            {
                this.m_DescriptionField.AddToClassList("read-only");
                typeField.AddToClassList("read-only");
                ((TextField)typeField).isReadOnly = true;
            }
            Add(this.m_DescriptionField);
        }

        public void Update(CustomAttributeUtility.Signature newType, string newDescription)
        {
            this.Q<EnumField>("type").value = newType;
            m_DescriptionField.value = newDescription;
        }

        private void OnDescriptionChanged(ChangeEvent<string> evt)
        {
            m_AttributeItem.description = evt.newValue;
            this.GetFirstAncestorOfType<VFXView>().controller.graph.TryUpdateCustomAttribute(this.m_AttributeItem.title, m_AttributeItem.type, m_AttributeItem.description);
        }

        private void OnTypeChange(ChangeEvent<Enum> evt)
        {
            m_AttributeItem.type = (CustomAttributeUtility.Signature)evt.newValue;
            this.GetFirstAncestorOfType<VFXView>().controller.graph.TryUpdateCustomAttribute(this.m_AttributeItem.title, m_AttributeItem.type, m_AttributeItem.description);
            this.GetFirstAncestorOfType<VFXBlackboardAttributeRow>().field.UpdateType(m_AttributeItem.type);
        }
    }
}
