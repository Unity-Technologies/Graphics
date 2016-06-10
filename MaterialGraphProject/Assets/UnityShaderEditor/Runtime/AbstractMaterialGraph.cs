using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class AbstractMaterialGraph : SerializableGraph
    {
        
        public IEnumerable<AbstractMaterialNode> materialNodes
        {
            get { return nodes.OfType<AbstractMaterialNode>(); }
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

        public override bool RequiresConstantRepaint()
        {
            return nodes.OfType<IRequiresTime>().Any();
        }

        /*public PreviewRenderUtility previewUtility
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

        [NonSerialized]
        private PreviewRenderUtility m_PreviewUtility;*/
    }
}
