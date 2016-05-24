using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public abstract class AbstractMaterialGraph : SerializableGraph
    {
        [NonSerialized]
        private PreviewRenderUtility m_PreviewUtility;

        [NonSerialized]
        private MaterialGraph m_Owner;

        protected AbstractMaterialGraph(MaterialGraph owner)
        {
            m_Owner = owner;
        }

        public IEnumerable<AbstractMaterialNode> materialNodes
        {
            get { return nodes.OfType<AbstractMaterialNode>(); }
        }

        public PreviewRenderUtility previewUtility
        {
            get
            {
                if (m_PreviewUtility == null)
                {
                    m_PreviewUtility = new PreviewRenderUtility();
                    // EditorUtility.SetCameraAnimateMaterials(m_PreviewUtility.m_Camera, true);
                }

                return m_PreviewUtility;
            }
        }

        public bool requiresRepaint
        {
            get { return nodes.Any(x => x is IRequiresTime); }
        }

        public MaterialGraph owner
        {
            get { return m_Owner; } 
            set { m_Owner = value; }
        }
         
        public override void AddNode(SerializableNode node)
        {
            if (node is AbstractMaterialNode)
            {
                base.AddNode(node);
            }
            else
            {
                Debug.LogWarningFormat("Trying to add node {0} to Material graph, but it is not a {1}", node, typeof(AbstractMaterialNode));
            }
        }

        public AbstractMaterialNode GetMaterialNodeFromGuid(Guid guid)
        {
            var node = GetNodeFromGuid(guid);
            if (node == null)
            {
                Debug.LogWarningFormat("Node with guid {0} either can not be found", guid);
                return null;
            }
            if (node is AbstractMaterialNode)
                return node as AbstractMaterialNode;

            Debug.LogWarningFormat("Node {0} with guid {1} is not a Material node", guid);
            return null;
        }
        
        public override void ValidateGraph()
        {
            base.ValidateGraph();

            foreach (var node in materialNodes)
                node.ValidateNode();
        }
    }
}
