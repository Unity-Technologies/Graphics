using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline.Drawing
{
    class DecalSettingsView : VisualElement
    {
        DecalMasterNode m_Node;

        Label CreateLabel(string text, int indentLevel)
        {
            string label = "";
            for (var i = 0; i < indentLevel; i++)
            {
                label += "    ";
            }
            return new Label(label + text);
        }

        public DecalSettingsView(DecalMasterNode node)
        {
            m_Node = node;
            PropertySheet ps = new PropertySheet();
            
            int indentLevel = 0;
            ps.Add(new PropertyRow(CreateLabel("Affect Metal", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.affectsMetal.isOn;
                    toggle.RegisterValueChangedCallback(ChangeAffectsMetal);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Affect AO", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.affectsAO.isOn;
                    toggle.RegisterValueChangedCallback(ChangeAffectsAO);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Affect Smoothness", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.affectsSmoothness.isOn;
                    toggle.RegisterValueChangedCallback(ChangeAffectsSmoothness);
                });
            });
            Add(ps);
        }

        void ChangeAffectsMetal(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Affects Metal Change");
            ToggleData td = m_Node.affectsMetal;
            td.isOn = evt.newValue;
            m_Node.affectsMetal = td;            
        }

        void ChangeAffectsAO(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Affects AO Change");
            ToggleData td = m_Node.affectsAO;
            td.isOn = evt.newValue;
            m_Node.affectsAO = td;
        }

        void ChangeAffectsSmoothness(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Affects Smoothness Change");
            ToggleData td = m_Node.affectsSmoothness;
            td.isOn = evt.newValue;
            m_Node.affectsSmoothness = td;
        }
    }
}
