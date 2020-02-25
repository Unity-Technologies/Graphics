using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class StringControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public StringControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new StringControlView(m_Label, node, propertyInfo);
        }
    }

    class StringControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public StringControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/StringControlView"));
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(string))
                throw new ArgumentException("Property must be of type string.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            var textField = new TextField { value = (string)m_PropertyInfo.GetValue(m_Node, null) };
            textField.RegisterValueChangedCallback(OnChange);

            Add(textField);
        }

        void OnChange(ChangeEvent<string> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("String Change");
            m_PropertyInfo.SetValue(m_Node, evt.newValue, null);
            this.MarkDirtyRepaint();
        }
    }
}
