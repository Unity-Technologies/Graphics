using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphDelta : IGraphHandler
    {
        private GraphStorage m_data;

        public GraphDelta()
        {
            m_data = new GraphStorage();
        }

        public NodeRef AddNode(string name)
        {
            return m_data.AddNode(name);
        }

        public NodeRef GetNode(string name)
        {
            return m_data.GetNode(name);
        }

        public IEnumerable<NodeRef> GetNodes()
        {
            foreach(var node in m_data.nodes)
            {
                yield return new NodeRef(node);
            }
        }
    }
}
