using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class ObjectControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public ObjectControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ObjectControlView(m_Label, node, propertyInfo);
        }
    }

    class ObjectControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public ObjectControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!typeof(Object).IsAssignableFrom(propertyInfo.PropertyType))
                throw new ArgumentException("Property must be assignable to UnityEngine.Object.");
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            label = label ?? propertyInfo.Name;

            if (!string.IsNullOrEmpty(label))
                Add(new Label { text = label });

            var value = (Object)m_PropertyInfo.GetValue(m_Node, null);
            var objectField = new ObjectField { objectType = propertyInfo.PropertyType, value = value };
            objectField.RegisterValueChangedCallback(OnValueChanged);
            Add(objectField);
        }

        void OnValueChanged(ChangeEvent<Object> evt)
        {
            var value = (Object)m_PropertyInfo.GetValue(m_Node, null);
            if (evt.newValue != value)
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Change + " + m_Node.name);
                m_PropertyInfo.SetValue(m_Node, evt.newValue, null);
            }
        }
    }
}
