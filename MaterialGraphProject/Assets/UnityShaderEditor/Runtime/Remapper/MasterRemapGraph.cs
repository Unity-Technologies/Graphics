using System;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class MasterRemapGraph : AbstractMaterialGraph
    {
        [NonSerialized]
        private MasterRemapInputNode m_InputNode;
        
        public MasterRemapInputNode inputNode
        {
            get
            {
                // find existing node
                if (m_InputNode == null)
                    m_InputNode = GetNodes<MasterRemapInputNode>().FirstOrDefault();

                return m_InputNode;
            }
        }
     
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_InputNode = null;
        }

        public override void AddNode(INode node)
        {
            if (inputNode != null && node is MasterRemapInputNode)
            {
                Debug.LogWarning("Attempting to add second SubGraphInputNode to SubGraph. This is not allowed.");
                return;
            }

            if (node is AbstractMaterialNode)
            {
                var amn = (AbstractMaterialNode) node;
                if (!amn.allowedInRemapGraph)
                    Debug.LogWarningFormat("Attempting to add {0} to Remap Graph. This is not allowed.", amn.GetType());
            }

            if (node is IGenerateProperties)
            {
                Debug.LogWarning("Attempting to add second SubGraphInputNode to SubGraph. This is not allowed.");
                return;
            }
            base.AddNode(node);
        }
    }
}
