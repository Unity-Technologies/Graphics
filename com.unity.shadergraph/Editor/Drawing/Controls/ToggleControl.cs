using System;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEngine;
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
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        UnityEngine.Experimental.UIElements.Toggle m_Toggle;

        public ToggleControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            AddStyleSheetPath("Styles/Controls/ToggleControlView");

            if (propertyInfo.PropertyType != typeof(Toggle))
                throw new ArgumentException("Property must be a Toggle.", "propertyInfo");

            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            var value = (Toggle)m_PropertyInfo.GetValue(m_Node, null);
            var panel = new VisualElement { name = "togglePanel" };
            if (!string.IsNullOrEmpty(label))
                panel.Add(new Label(label));
            Action changedToggle = () => { OnChangeToggle(); };
            m_Toggle = new UnityEngine.Experimental.UIElements.Toggle(changedToggle);
            m_Toggle.SetEnabled(value.isEnabled);
            m_Toggle.SetValue(value.isOn);
            panel.Add(m_Toggle);
            Add(panel);
        }

        public void OnNodeModified(ModificationScope scope)
        {
            var value = (Toggle)m_PropertyInfo.GetValue(m_Node, null);
            m_Toggle.SetEnabled(value.isEnabled);

            if (scope == ModificationScope.Graph)
            {
                Dirty(ChangeType.Repaint);
            }
        }

        void OnChangeToggle()
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
            var value = (Toggle)m_PropertyInfo.GetValue(m_Node, null);
            value.isOn = !value.isOn;
            m_PropertyInfo.SetValue(m_Node, value, null);
            Dirty(ChangeType.Repaint);
        }
    }
}
