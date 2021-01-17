using System;
using System.Reflection;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class CustomInterpolatorControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        public CustomInterpolatorControlAttribute(string label = null) { m_Label = label; }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new CustomInterpolatorControlView(m_Label, node as CustomInterpolatorSelectorNode, propertyInfo);
        }
    }

    [Serializable]
    struct CustomInterpolatorList
    {
        public string selectedEntry;
        public BlockNode.CustomBlockType selectedType;

        public CustomInterpolatorList(string defaultEntry, BlockNode.CustomBlockType defaultType)
        {
            selectedEntry = defaultEntry;
            selectedType = defaultType;
        }
    }


    class CustomInterpolatorControlView : VisualElement
    {
        CustomInterpolatorSelectorNode m_Node;
        PropertyInfo m_PropertyInfo;
        List<string> m_validNames = new List<string>() { "" };
        PopupField<string> m_PopupField;

        public CustomInterpolatorControlView(string label, CustomInterpolatorSelectorNode node, PropertyInfo propertyInfo)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/PopupControlView"));
            m_Node = node;
            m_PropertyInfo = propertyInfo;

            Type type = propertyInfo.PropertyType;
            if (type != typeof(CustomInterpolatorList))
            {
                throw new ArgumentException("Property must be a PopupList.", "propertyInfo");
            }

            OnNodeRevalidation();
            BuildNames();

            Add(new Label(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name)));
            var value = (CustomInterpolatorList)propertyInfo.GetValue(m_Node, null);
            m_PopupField = new PopupField<string>(m_validNames, value.selectedEntry);
            m_PopupField.RegisterValueChangedCallback(OnValueChanged);
            m_Node.revalidationCallback += OnNodeRevalidation;
            Add(m_PopupField);
        }

        void BuildNames()
        {
            m_validNames.Clear();
            m_validNames.Add("");
            if (m_Node.owner != null)
                foreach (var bnode in m_Node.owner.vertexContext.blocks.Where(block => block.value.isCustomBlock))
                    m_validNames.Add(bnode.value.descriptor.displayName);

            // our values could be invalid now-
            if (m_PopupField != null && !m_validNames.Contains(m_PopupField.value))
            {
                e_oldFriend = null;
                var value = (CustomInterpolatorList)m_PropertyInfo.GetValue(m_Node, null);
                m_PopupField.value = "";
                value.selectedEntry = m_PopupField.value = "";
                value.selectedType = BlockNode.CustomBlockType.Vector4;
                m_PropertyInfo.SetValue(m_Node, value, null);
            }
        }

        void OnNodeRevalidation()
        {
            // when our target node revalidates, it means a new node was added or removed.
            // we need to reg. to that block node in case it's modified by the custom block property drawer.
            foreach (var bnode in m_Node.owner.vertexContext.blocks.Where(bn => bn.value.isCustomBlock).Select(node => node.value))
            {
                bnode.RegisterCallback(BlockNodeModified);
            }

            BuildNames();
        }

        void BlockNodeModified(AbstractMaterialNode node, Graphing.ModificationScope scope)
        {
            var value = (CustomInterpolatorList)m_PropertyInfo.GetValue(m_Node, null);
            if (node is BlockNode bnode)
            {
                if (bnode.isCustomBlock)
                {
                    if (bnode == e_oldFriend)
                    {
                        // Our currently selected node's name was changed.
                        m_validNames.Remove(value.selectedEntry);
                        m_validNames.Add(bnode.customName);
                        value.selectedEntry = m_PopupField.value = bnode.customName;
                        value.selectedType = e_oldFriend.customWidth;
                        m_PropertyInfo.SetValue(m_Node, value, null);
                    }                    
                }
            }
            BuildNames();
        }

        BlockNode e_oldFriend;

        void OnValueChanged(ChangeEvent<string> evt)
        {
            e_oldFriend = m_Node.owner.vertexContext.blocks.Find(bnr => bnr.value.customName == m_PopupField.value);

            var value = (CustomInterpolatorList)m_PropertyInfo.GetValue(m_Node, null);
            value.selectedType = e_oldFriend?.customWidth ?? BlockNode.CustomBlockType.Vector4;
            value.selectedEntry = m_PopupField.value;
            m_PropertyInfo.SetValue(m_Node, value, null);

            m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
        }
    }
}
