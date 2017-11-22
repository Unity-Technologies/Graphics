using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    public enum ToggleState
    {
        EnabledOff,
        EnabledOn,
        DisabledOff,
        DisabledOn
    }

    public static class ToggleHelper
    {
        public static bool GetBoolValue(ToggleState value)
        {
            if ((int)value > 1)
                return Convert.ToBoolean((int)value - 2);
            else
                return Convert.ToBoolean((int)value);
        }

        public static ToggleState GetToggleValue(bool value)
        {
            return (ToggleState)Convert.ToInt32(value);
        }

        public static ToggleState GetToggleValue(bool value, bool enableCondition)
        {
            return (ToggleState)(Convert.ToInt32(value) + (enableCondition ? 0 : 2));
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
            if (propertyInfo.PropertyType != typeof(ToggleState))
                throw new ArgumentException("Property must be a ToggleState.", "propertyInfo");
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
            var value = (ToggleState) m_PropertyInfo.GetValue(m_Node, null);

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                bool isEnabled = (int)value < 2;
                m_Container.SetEnabled(isEnabled);

                bool isOn = EditorGUILayout.Toggle(m_Label, (int)value == 1 || (int)value == 3);
                value = (ToggleState)(Convert.ToInt32(!isEnabled) + Convert.ToInt32(isOn));
                if (changeCheckScope.changed)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, value, null);
                }
            }
        }
    }
}
