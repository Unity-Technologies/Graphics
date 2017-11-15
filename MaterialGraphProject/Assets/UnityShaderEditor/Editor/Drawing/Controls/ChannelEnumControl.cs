using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChannelEnumControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        int m_SlotId;

        public ChannelEnumControlAttribute(string label = null, int slotId = 0)
        {
            m_Label = label;
            m_SlotId = slotId;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ChannelEnumControlView(m_Label, m_SlotId, node, propertyInfo);
        }
    }

    public class ChannelEnumControlView : VisualElement, INodeModificationListener
    {
        GUIContent m_Label;
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        int m_SlotId;

        public ChannelEnumControlView(string label, int slotId, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            m_SlotId = slotId;
            if (!propertyInfo.PropertyType.IsEnum)
                throw new ArgumentException("Property must be an enum.", "propertyInfo");
            m_Label = new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));
            Add(new IMGUIContainer(OnGUIHandler));
        }

        void OnGUIHandler()
        {
            UpdatePopup();
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if(scope == ModificationScope.Graph)
                UpdatePopup();
        }

        private void UpdatePopup()
        {
            var value = (int)m_PropertyInfo.GetValue(m_Node, null);
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                int count = (int)SlotValueHelper.GetChannelCount(m_Node.FindSlot<MaterialSlot>(m_SlotId).concreteValueType);
                if (value >= count)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, 0, null);
                    return;
                }
                var entries = (Enum)m_PropertyInfo.GetValue(m_Node, null);
                string[] enumNames = Enum.GetNames(entries.GetType());
                string[] popupEntries = new string[count];
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
