using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColorControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public ColorControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ColorControlView(m_Label, node, propertyInfo);
        }
    }

    public class ColorControlView : VisualElement
    {
        GUIContent m_Label;
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public ColorControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(Color))
                throw new ArgumentException("Property must be of type Color.", "propertyInfo");
            m_Label = new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));
            Add(new IMGUIContainer(OnGUIHandler));
        }

        void OnGUIHandler()
        {
            var value = (Color) m_PropertyInfo.GetValue(m_Node, null);
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                value = EditorGUILayout.ColorField(m_Label, value);
                if (changeCheckScope.changed)
                    m_PropertyInfo.SetValue(m_Node, value, null);
            }
        }
    }
}
