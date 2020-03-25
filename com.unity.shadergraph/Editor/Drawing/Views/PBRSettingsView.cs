using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing
{
    class PBRSettingsView : MasterNodeSettingsView
    {
        PBRMasterNode m_Node;
        public PBRSettingsView(AbstractMaterialNode node) : base(node)
        {
            m_Node = node as PBRMasterNode;

            PropertySheet ps = new PropertySheet();

            ps.Add(new PropertyRow(new Label("Workflow")), (row) =>
                {
                    row.Add(new EnumField(PBRMasterNode.Model.Metallic), (field) =>
                    {
                        field.value = m_Node.model;
                        field.RegisterValueChangedCallback(ChangeWorkFlow);
                    });
                });

            ps.Add(new PropertyRow(new Label("Surface")), (row) =>
                {
                    row.Add(new EnumField(SurfaceType.Opaque), (field) =>
                    {
                        field.value = m_Node.surfaceType;
                        field.RegisterValueChangedCallback(ChangeSurface);
                    });
                });

            ps.Add(new PropertyRow(new Label("Blend")), (row) =>
                {
                    row.Add(new EnumField(AlphaMode.Additive), (field) =>
                    {
                        field.value = m_Node.alphaMode;
                        field.RegisterValueChangedCallback(ChangeAlphaMode);
                    });
                });

            ps.Add(new PropertyRow(new Label("Fragment Normal Space")), (row) =>
            {
                row.Add(new EnumField(NormalDropOffSpace.Tangent), (field) =>
                {
                    field.value = m_Node.normalDropOffSpace;
                    field.RegisterValueChangedCallback(ChangeSpaceOfNormalDropOffMode);
                });
            });

            ps.Add(new PropertyRow(new Label("Two Sided")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.twoSided.isOn;
                        toggle.OnToggleChanged(ChangeTwoSided);
                    });
                });

            ps.Add(new PropertyRow(new Label("DOTS instancing")), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.dotsInstancing.isOn;
                    toggle.OnToggleChanged(ChangeDotsInstancing);
                });
            });

            Add(ps);
            Add(GetShaderGUIOverridePropertySheet());
        }

        void ChangeWorkFlow(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.model, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Work Flow Change");
            m_Node.model = (PBRMasterNode.Model)evt.newValue;
        }

        void ChangeSurface(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.surfaceType, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Surface Change");
            m_Node.surfaceType = (SurfaceType)evt.newValue;
        }

        void ChangeAlphaMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.alphaMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Mode Change");
            m_Node.alphaMode = (AlphaMode)evt.newValue;
        }

        void ChangeSpaceOfNormalDropOffMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.normalDropOffSpace, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Normal Space Drop-Off Mode Change");
            m_Node.normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
        }

        void ChangeTwoSided(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Two Sided Change");
            ToggleData td = m_Node.twoSided;
            td.isOn = evt.newValue;
            m_Node.twoSided = td;
        }

        void ChangeDotsInstancing(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("DotsInstancing Change");
            ToggleData td = m_Node.dotsInstancing;
            td.isOn = evt.newValue;
            m_Node.dotsInstancing = td;
        }
    }
}
