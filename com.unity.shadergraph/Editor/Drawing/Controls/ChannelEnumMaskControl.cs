using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;

using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class ChannelEnumMaskControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        int m_SlotId;

        public ChannelEnumMaskControlAttribute(string label = null, int slotId = 0)
        {
            m_Label = label;
            m_SlotId = slotId;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ChannelEnumMaskControlView(m_Label, m_SlotId, node, propertyInfo);
        }
    }

    class ChannelEnumMaskControlView : VisualElement, AbstractMaterialNodeModificationListener
    {
        string m_Label;
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        IMGUIContainer m_Container;
        int m_SlotId;

        public ChannelEnumMaskControlView(string label, int slotId, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/ChannelEnumMaskControlView"));
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            m_SlotId = slotId;
            //if (!propertyInfo.PropertyType.IsEnum)
            //throw new ArgumentException("Property must be an enum.", "propertyInfo");
            m_Label = label;
            m_Container = new IMGUIContainer(OnGUIHandler);
            Add(m_Container);
        }

        void OnGUIHandler()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(m_Label);
            UpdatePopup();
            GUILayout.EndHorizontal();
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Graph)
                m_Container.MarkDirtyRepaint();
        }

        private void UpdatePopup()
        {
            var value = (int)m_PropertyInfo.GetValue(m_Node, null);
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                int channelCount = SlotValueHelper.GetChannelCount(m_Node.FindSlot<MaterialSlot>(m_SlotId).concreteValueType);
                string[] enumEntryNames = Enum.GetNames(typeof(TextureChannel));
                string[] popupEntries = new string[channelCount];
                for (int i = 0; i < popupEntries.Length; i++)
                    popupEntries[i] = enumEntryNames[i];
                value = EditorGUILayout.MaskField("", value, popupEntries, GUILayout.Width(80f));

                if (changeCheckScope.changed)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, value, null);
                }
            }
        }
    }
}
