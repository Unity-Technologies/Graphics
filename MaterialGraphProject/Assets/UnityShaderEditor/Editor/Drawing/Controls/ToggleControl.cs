using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [Serializable]
    public struct Toggle
    {
        public bool isOn;
        public bool isEnabled;

        public Toggle(bool on, bool enabled)
        {
            isOn = on;
            isEnabled = enabled;
        }

        public Toggle(bool on)
        {
            isOn = on;
            isEnabled = true;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ToggleControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public ToggleControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ToggleControlView(m_Label, node, propertyInfo);
        }
    }

    public class ToggleControlView : VisualElement, INodeModificationListener
    {
        GUIContent m_Label;
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        IMGUIContainer m_Container;

        public ToggleControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(Toggle))
                throw new ArgumentException("Property must be a Toggle.", "propertyInfo");
            m_Label = new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));
            m_Container = new IMGUIContainer(OnGUIHandler);
            Add(m_Container);
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Graph)
                m_Container.Dirty(ChangeType.Repaint);
        }

        void OnGUIHandler()
        {
            var value = (Toggle)m_PropertyInfo.GetValue(m_Node, null);

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                m_Container.SetEnabled(value.isEnabled);

                bool isOn = EditorGUILayout.Toggle(m_Label, value.isOn);
                value = new Toggle(isOn, value.isEnabled);
                if (changeCheckScope.changed)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, value, null);
                }
            }
        }
    }
}
