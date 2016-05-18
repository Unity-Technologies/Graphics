using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public abstract class PropertyNode : BaseMaterialNode
    {
        protected class NodeSpecificData : BaseMaterialNode.NodeSpecificData
        {
            [SerializeField]
            public string m_PropertyName;

            [SerializeField]
            public string m_Description;

            [SerializeField]
            public bool m_Exposed;
        }

        protected void ApplyNodeSpecificData(NodeSpecificData data)
        {
            m_NodeSpecificData.m_PropertyName = data.m_PropertyName;
            m_NodeSpecificData.m_Description = data.m_Description;
            m_NodeSpecificData.m_Exposed = data.m_Exposed;
        }

        private NodeSpecificData m_NodeSpecificData = new NodeSpecificData();

        public PropertyNode(BaseMaterialGraph owner) : base(owner)
        {}

        public bool exposed
        {
            get { return m_NodeSpecificData.m_Exposed; }
        }

        public string description
        {
            get
            {
                if (string.IsNullOrEmpty(m_NodeSpecificData.m_Description))
                    return propertyName;

                return m_NodeSpecificData.m_Description;
            }
            set { m_NodeSpecificData.m_Description = value; }
        }

        public virtual string propertyName
        {
            get
            {
                if (!exposed || string.IsNullOrEmpty(m_NodeSpecificData.m_PropertyName))
                    return string.Format("{0}_{1}_Uniform", name, guid);

                return m_NodeSpecificData.m_PropertyName + "_Uniform";
            }
            set { m_NodeSpecificData.m_PropertyName = value; }
        }

        public abstract PropertyType propertyType { get; }

        public abstract PreviewProperty GetPreviewProperty();
        
        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            return propertyName;
        }
        
        public override float GetNodeUIHeight(float width)
        {
            return 2 * EditorGUIUtility.singleLineHeight;
        }

        protected override void CollectPreviewMaterialProperties (List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            properties.Add(GetPreviewProperty());
        }
        
        protected override bool CalculateNodeHasError()
        {
            if (!exposed)
                return false;

            var allNodes = owner.nodes;
            foreach (var n in allNodes.OfType<PropertyNode>())
            {
                if (n == this)
                    continue;;

                if (n.propertyName == propertyName)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            m_NodeSpecificData.m_Exposed = EditorGUILayout.Toggle("Exposed Property", m_NodeSpecificData.m_Exposed);
            if (m_NodeSpecificData.m_Exposed)
                m_NodeSpecificData.m_PropertyName = EditorGUILayout.DelayedTextField("Property Name", m_NodeSpecificData.m_PropertyName);

            var modified = EditorGUI.EndChangeCheck();
            if (modified)
            {
                owner.RevalidateGraph();
            }

            if (m_NodeSpecificData.m_Exposed)
                m_NodeSpecificData.m_Description = EditorGUILayout.TextField("Description", m_NodeSpecificData.m_Description);
            
            modified |= base.OnGUI();
            return modified;
        }
    }
}
