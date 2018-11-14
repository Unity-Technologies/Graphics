using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    // The purpose of this class is the hide the fact that it's backed by a list, so that we're allowed to change that
    // in the future. The issue with returning a IEnumerable<NodeRef> is that it will box the enumerable from
    // List<NodeRef> and thus cause a GC allocation.
    public struct NodeRefEnumerable : IEnumerable<NodeRef>
    {
        AbstractMaterialGraph m_Graph;
        int m_ContextId;
        List<ProxyShaderNode> m_Nodes;

        internal NodeRefEnumerable(AbstractMaterialGraph graph, int contextId, List<ProxyShaderNode> nodes)
        {
            m_Graph = graph;
            m_ContextId = contextId;
            m_Nodes = nodes;
        }

        public NodeRefEnumerator GetEnumerator()
        {
            Validate();
            return new NodeRefEnumerator(m_Graph, m_ContextId, m_Nodes.GetEnumerator());
        }

        IEnumerator<NodeRef> IEnumerable<NodeRef>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void Validate()
        {
            if (m_ContextId != m_Graph.currentContextId)
            {
                throw new InvalidOperationException($"{nameof(NodeRefEnumerable)} is only valid during the call to {nameof(IShaderNodeType)}.{nameof(IShaderNodeType.OnChange)} it was created in.");
            }
        }
    }

    public struct NodeRefEnumerator : IEnumerator<NodeRef>
    {
        AbstractMaterialGraph m_Graph;
        int m_ContextId;
        List<ProxyShaderNode>.Enumerator m_Enumerator;

        internal NodeRefEnumerator(AbstractMaterialGraph graph, int contextId, List<ProxyShaderNode>.Enumerator enumerator)
        {
            m_Graph = graph;
            m_ContextId = contextId;
            m_Enumerator = enumerator;
        }

        public bool MoveNext()
        {
            return m_Enumerator.MoveNext();
        }

        public void Reset()
        {
            m_Enumerator.Dispose();
        }

        public NodeRef Current => new NodeRef(m_Graph, m_ContextId, m_Enumerator.Current);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            m_Enumerator.Dispose();
        }
    }
}
