using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public abstract class AbstractMaterialGraph : Graphing.SerializableGraph
    {
        [NonSerialized]
        private PreviewRenderUtility m_PreviewUtility;
        
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
                    EditorUtility.SetCameraAnimateMaterials(m_PreviewUtility.m_Camera, true);
                }

                return m_PreviewUtility;
            }
        }

        public bool requiresRepaint
        {
            get { return nodes.Any(x => x is IRequiresTime); }
        }
         
        public override void AddNode(INode node)
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
