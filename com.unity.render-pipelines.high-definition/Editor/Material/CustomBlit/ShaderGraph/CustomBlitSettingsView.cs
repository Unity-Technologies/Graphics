using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Drawing
{
    class CustomBlitSettingsView : VisualElement
    {
        CustomBlitMasterNode m_Node;

        Label CreateLabel(string text, int indentLevel)
        {
            string label = "";
            for (var i = 0; i < indentLevel; i++)
            {
                label += "    ";
            }
            return new Label(label + text);
        }

        void ChangeBlendType(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.blendType, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Blend Type Change");
            m_Node.blendType = (CustomBlitMasterNode.BlendType)evt.newValue;
        }

        public CustomBlitSettingsView(CustomBlitMasterNode node)
        {
            m_Node = node;
            int indentLevel = 0;
            PropertySheet ps = new PropertySheet();
            ps.Add(new PropertyRow(CreateLabel("Blend Type", indentLevel)), (row) =>
            {
                row.Add(new EnumField(CustomBlitMasterNode.BlendType.None), (field) =>
                {
                    field.value = m_Node.blendType;
                    field.RegisterValueChangedCallback(ChangeBlendType);
                });
            });
            Add(ps);
        }     
    }
}
