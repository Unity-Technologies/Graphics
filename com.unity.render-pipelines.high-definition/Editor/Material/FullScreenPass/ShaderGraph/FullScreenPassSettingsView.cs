using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline.Drawing
{
    class FullScreenPassSettingsView : VisualElement
    {
        FullScreenPassMasterNode m_Node;

        IntegerField m_SortPriorityField;

        Label CreateLabel(string text, int indentLevel)
        {
            string label = "";
            for (var i = 0; i < indentLevel; i++)
            {
                label += "    ";
            }
            return new Label(label + text);
        }

        public FullScreenPassSettingsView(FullScreenPassMasterNode node)
        {
            m_Node = node;
            PropertySheet ps = new PropertySheet();

            int indentLevel = 0;
            ps.Add(new PropertyRow(CreateLabel("Output Depth", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.modifyDepth.isOn;
                    toggle.OnToggleChanged(ChangeOutputDepth);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Blending Mode", indentLevel)), (row) =>
            {
                row.Add(new EnumField(HDLitMasterNode.AlphaModeLit.Additive), (field) =>
                {
                    field.value = m_Node.alphaMode;
                    field.RegisterValueChangedCallback(ChangeBlendMode);
                });
            });

            Add(ps);
        }

        void ChangeOutputDepth(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Output Depth Change");
            ToggleData td = m_Node.modifyDepth;
            td.isOn = evt.newValue;
            m_Node.modifyDepth = td;
        }

        void ChangeBlendMode(ChangeEvent<Enum> evt)
        {
            // Make sure the mapping is correct by handling each case.

            AlphaMode alphaMode = (AlphaMode)evt.newValue;

            if (Equals(m_Node.alphaMode, alphaMode))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Mode Change");
            m_Node.alphaMode = alphaMode;
        }
    }
}
