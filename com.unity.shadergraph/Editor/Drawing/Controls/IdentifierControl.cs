using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class IdentifierControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public IdentifierControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new IdentifierControlView(m_Label, node, propertyInfo);
        }
    }

    class IdentifierControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public IdentifierControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            var style = Resources.Load<StyleSheet>("Styles/Controls/IdentifierControlView");
            if (style) styleSheets.Add(style);

            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(string))
                throw new ArgumentException("Property must be of type string.", "propertyInfo");

            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);
            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            var strField = new IdentifierField() { value = (string)m_PropertyInfo.GetValue(m_Node, null) };
            strField.RegisterValueChangedCallback(OnChange);
            Add(strField);
        }

        void OnChange(ChangeEvent<string> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Identifier Change");
            m_PropertyInfo.SetValue(m_Node, evt.newValue, null);
            this.MarkDirtyRepaint();
        }
    }
}
