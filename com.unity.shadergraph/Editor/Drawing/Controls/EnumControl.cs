using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class EnumControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public EnumControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new EnumControlView(m_Label, node, propertyInfo);
        }
    }

    class EnumControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public EnumControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/EnumControlView"));
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (!propertyInfo.PropertyType.IsEnum)
                throw new ArgumentException("Property must be an enum.", "propertyInfo");
            Add(new Label(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name)));
            var enumField = new EnumField((Enum)m_PropertyInfo.GetValue(m_Node, null));
            enumField.RegisterValueChangedCallback(OnValueChanged);
            Add(enumField);
        }

        void OnValueChanged(ChangeEvent<Enum> evt)
        {
            var value = (Enum)m_PropertyInfo.GetValue(m_Node, null);
            if (!evt.newValue.Equals(value))
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                m_PropertyInfo.SetValue(m_Node, evt.newValue, null);
            }
        }
    }
}
