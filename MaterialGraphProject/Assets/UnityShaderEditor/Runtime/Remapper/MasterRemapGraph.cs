using System;
using System.Collections.Generic;
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
            base.AddNode(node);
        }

        struct DisposeMeh : IDisposable
        {
            private readonly MasterRemapInputNode m_Graph;

            public DisposeMeh(MasterRemapInputNode graph, RemapMasterNode master)
            {
                m_Graph = graph;
                graph.m_RemapTarget = master;
            }

            public void Dispose()
            {
                m_Graph.m_RemapTarget = null;
            }
        }

        public List<string> GetSubShadersFor(RemapMasterNode rmn, GenerationMode mode)
        {
            var subShaders = new List<string>();
            try
            {
                using (new DisposeMeh(inputNode, rmn))
                {
                    foreach (var node in GetNodes<IMasterNode>())
                        subShaders.Add(node.GetSubShader(mode));
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return subShaders;
        }
    }
}
