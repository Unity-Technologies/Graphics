using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public abstract class AbstractMaterialGraph : SerializableGraph
    {
        private PreviewRenderUtility m_PreviewUtility;
        private MaterialGraph m_Owner;

        protected AbstractMaterialGraph(MaterialGraph owner)
        {
            m_Owner = owner;
        }

        public MaterialGraph owner
        {
            get { return m_Owner; }
        }

        public IEnumerable<AbstractMaterialNode> materialNodes
        {
            get { return nodes.Where(x => x is AbstractMaterialNode).Cast<AbstractMaterialNode>(); }
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


        public override void ValidateGraph()
        {
            base.ValidateGraph();

            foreach (var node in materialNodes)
                node.ValidateNode();
        }
    }
}
