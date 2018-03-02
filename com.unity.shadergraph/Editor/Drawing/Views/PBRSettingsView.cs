using System;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PBRSettingsView : VisualElement
    {
        PBRMasterNode m_Node;
        public PBRSettingsView(INode node)
        {
            AddStyleSheetPath("Styles/PBRSettings");
            m_Node = (PBRMasterNode)node;

            this.Add(new VisualElement { name = "container" }, (container) =>
            {
                container.Add(new VisualElement(), (row) =>
                {
                    row.AddToClassList("row");

                    row.Add(new Label("Model"), (label) =>
                    {
                        label.AddToClassList("label");
                    });

                    row.Add(new EnumField(PBRMasterNode.Model.Metallic), (enumField) =>
                    {
                        enumField.value = m_Node.model;
                        enumField.OnValueChanged(ChangeModel);
                        enumField.AddToClassList("enumcontainer");
                    });
                });

                container.Add(new VisualElement(), (row) =>
                {
                    row.AddToClassList("row");

                    row.Add(new Label("Alpha Mode"), (label) =>
                    {
                        label.AddToClassList("label");
                    });
                    row.Add(new EnumField(AlphaMode.Additive), (enumField) =>
                    {
                        enumField.value = m_Node.alphaMode;
                        enumField.OnValueChanged(ChangeAlphaMode);
                        enumField.AddToClassList("enumcontainer");
                    });
                });
            });
        }

        void ChangeModel(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.model, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Enum Change");
            m_Node.model = (PBRMasterNode.Model)evt.newValue;
        }

        void ChangeAlphaMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.alphaMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Mode Change");
            m_Node.alphaMode = (AlphaMode)evt.newValue;
        }
    }
}


