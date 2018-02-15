using System;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PBRSettingsView : VisualElement
    {
        VisualElement m_Container;

        EnumField m_Model;
        EnumField m_AlphaMode;
        PBRMasterNode m_Node;
        public PBRSettingsView(INode node)
        {
            m_Node = (PBRMasterNode)node;
            var uxml = Resources.Load<VisualTreeAsset>("UXML/PBRSettings");
            uxml.CloneTree(this, null);

            m_Container = this.Q("container");

            m_Model = new EnumField(m_Node.model);
            m_AlphaMode = new EnumField(m_Node.alphaMode);

            m_Model.AddToClassList("enumcontainer");
            m_AlphaMode.AddToClassList("enumcontainer");

            m_Container.Add(m_Model);
            m_Container.Add(m_AlphaMode);

            m_Model.OnValueChanged(ChangeModel);
            m_AlphaMode.OnValueChanged(ChangeAlphaMode);
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


