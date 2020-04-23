using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.Rendering.HighDefinition.Drawing
{
    class DecalSettingsView : MasterNodeSettingsView
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

        public DecalSettingsView(DecalMasterNode node) : base(node)
        {
            m_Node = node;
            PropertySheet ps = new PropertySheet();

            int indentLevel = 0;

            ps.Add(new PropertyRow(CreateLabel("Affect BaseColor", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.affectsAlbedo.isOn;
                    toggle.RegisterValueChangedCallback(ChangeAffectsAlbedo);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Affects Normal", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.affectsNormal.isOn;
                    toggle.RegisterValueChangedCallback(ChangeAffectsNormal);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Affects Metal", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.affectsMetal.isOn;
                    toggle.RegisterValueChangedCallback(ChangeAffectsMetal);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Affects AO", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.affectsAO.isOn;
                    toggle.RegisterValueChangedCallback(ChangeAffectsAO);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Affects Smoothness", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.affectsSmoothness.isOn;
                    toggle.RegisterValueChangedCallback(ChangeAffectsSmoothness);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Affects Emission", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.affectsEmission.isOn;
                    toggle.RegisterValueChangedCallback(ChangeAffectsEmission);
                });
            });

            Add(ps);
            Add(GetShaderGUIOverridePropertySheet());
        }

        void ChangeAffectsAlbedo(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Affects Albedo Change");
            ToggleData td = m_Node.affectsAlbedo;
            td.isOn = evt.newValue;
            m_Node.affectsAlbedo = td;
        }

        void ChangeAffectsNormal(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Affects Normal Change");
            ToggleData td = m_Node.affectsNormal;
            td.isOn = evt.newValue;
            m_Node.affectsNormal = td;
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

        void ChangeAffectsEmission(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Affects Emission Change");
            ToggleData td = m_Node.affectsEmission;
            td.isOn = evt.newValue;
            m_Node.affectsEmission = td;
        }

    }
}
