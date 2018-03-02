using System;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class UnlitSettingsView : VisualElement
    {
        VisualElement m_Container;

        EnumField m_AlphaMode;
        UnlitMasterNode m_Node;
        public UnlitSettingsView(INode node)
        {
            AddStyleSheetPath("Styles/UnlitSettings");
            m_Node = (UnlitMasterNode)node;
            this.Add(new VisualElement{ name="container" }, (container) =>
            {
                container.Add( new VisualElement(), (row) =>
                {
                    row.AddToClassList("row");
                    row.Add(new Label { text = "Alpha Mode"}, (label) =>
                    {
                        label.AddToClassList("label");
                    });

                    row.Add(new EnumField(AlphaMode.Additive), (enumField) =>
                    {
                        enumField.AddToClassList("enumcontainer");
                        enumField.value = m_Node.alphaMode;
                        enumField.OnValueChanged(ChangeAlphaMode);
                    });
                });
            } );
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


