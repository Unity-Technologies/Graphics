using System;
using static UnityEditor.ShaderGraph.GraphDelta.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public sealed class NodeRef : IDisposable
    {
        private WeakReference<Element> m_node;

        public void Dispose()
        {
            m_node = null;
        }

        internal NodeRef(Element elem)
        {
            m_node = new WeakReference<Element>(elem);
        }
    }
}
