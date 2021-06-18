using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class PopupControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        //string[] m_Entries;

        public PopupControlAttribute(string label = null)
        {
            m_Label = label;
            //m_Entries = entries;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new PopupControlView(m_Label, node, propertyInfo);
        }
    }

    [Serializable]
    struct PopupList
    {
        public int selectedEntry;
        public string[] popupEntries;

        public PopupList(string[] entries, int defaultEntry)
        {
            popupEntries = entries;
            selectedEntry = defaultEntry;
        }
    }

    class PopupControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        PopupField<string> m_PopupField;

        public PopupControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/PopupControlView"));
            m_Node = node;
            m_PropertyInfo = propertyInfo;

            Type type = propertyInfo.PropertyType;
            if (type != typeof(PopupList))
            {
                throw new ArgumentException("Property must be a PopupList.", "propertyInfo");
            }

            Add(new Label(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name)));
            var value = (PopupList)propertyInfo.GetValue(m_Node, null);
            m_PopupField = new PopupField<string>(new List<string>(value.popupEntries), value.selectedEntry);
            m_PopupField.RegisterValueChangedCallback(OnValueChanged);
            Add(m_PopupField);
        }

        void OnValueChanged(ChangeEvent<string> evt)
        {
            var value = (PopupList)m_PropertyInfo.GetValue(m_Node, null);
            value.selectedEntry = m_PopupField.index;
            m_PropertyInfo.SetValue(m_Node, value, null);
            m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
        }
    }
}
