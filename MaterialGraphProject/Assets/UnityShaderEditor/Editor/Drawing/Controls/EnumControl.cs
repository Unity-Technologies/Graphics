using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EnumControlAttribute : Attribute, IControlAttribute
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

    public class EnumControlView : VisualElement
    {
        GUIContent m_Label;
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public EnumControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (!propertyInfo.PropertyType.IsEnum)
                throw new ArgumentException("Property must be an enum.", "propertyInfo");
            m_Label = new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));
            Add(new IMGUIContainer(OnGUIHandler));
        }

        void OnGUIHandler()
        {
            var value = (Enum) m_PropertyInfo.GetValue(m_Node, null);
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                value = EditorGUILayout.EnumPopup(m_Label, value);
                if (changeCheckScope.changed)
                    m_PropertyInfo.SetValue(m_Node, value, null);
            }
        }
    }
}
