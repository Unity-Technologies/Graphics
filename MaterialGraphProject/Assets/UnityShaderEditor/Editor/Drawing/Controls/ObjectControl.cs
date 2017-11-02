using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ObjectControlAttribute : Attribute, IControlAttribute
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

    public class ObjectControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        GUIContent m_Label;

        public ObjectControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!typeof(Object).IsAssignableFrom(propertyInfo.PropertyType))
                throw new ArgumentException("Property must be assignable to UnityEngine.Object.");
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            m_Label = new GUIContent(label ?? propertyInfo.Name);
            Add(new IMGUIContainer(OnGUIHandler));
        }

        void OnGUIHandler()
        {
            var value = (Object) m_PropertyInfo.GetValue(m_Node, null);
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                value = EditorGUILayout.MiniThumbnailObjectField(m_Label, value, m_PropertyInfo.PropertyType);
                if (changeCheckScope.changed)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, value, null);
                }
            }
        }
    }
}
