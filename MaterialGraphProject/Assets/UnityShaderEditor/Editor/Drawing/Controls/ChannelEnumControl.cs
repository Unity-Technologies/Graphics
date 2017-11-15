using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChannelEnumControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public ChannelEnumControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ChannelEnumControlView(m_Label, node, propertyInfo);
        }
    }

    public class ChannelEnumControlView : VisualElement
    {
        GUIContent m_Label;
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        string[] popupEntries;

        public ChannelEnumControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
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
            var entries = (Enum) m_PropertyInfo.GetValue(m_Node, null);
            var value = (int)m_PropertyInfo.GetValue(m_Node, null);

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                int count = (int)SlotValueHelper.GetChannelCount(m_Node.FindSlot<MaterialSlot>(0).concreteValueType);
                popupEntries = new string[count];

                string[] enumNames = Enum.GetNames(entries.GetType());
                for (int i = 0; i < popupEntries.Length; i++)
                    popupEntries[i] = enumNames[i];
                value = EditorGUILayout.Popup(m_Label, value, popupEntries);

                if (changeCheckScope.changed)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, value, null);
                }
            }
        }
    }
}
