using System;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
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
            m_Node = (UnlitMasterNode)node;
            var uxml = Resources.Load<VisualTreeAsset>("UXML/UnlitSettings");
            uxml.CloneTree(this, null);

            m_Container = this.Q("container");

            m_AlphaMode = new EnumField(m_Node.alphaMode);
            m_AlphaMode.AddToClassList("enumcontainer");

            m_Container.Add(m_AlphaMode);

            m_AlphaMode.OnValueChanged(ChangeAlphaMode);
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


