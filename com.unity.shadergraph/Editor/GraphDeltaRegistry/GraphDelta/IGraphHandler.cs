using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IGraphHandler
    {
        public IEnumerable<NodeHandler> GetNodes();
        public NodeHandler GetNode(ElementID id);
        internal NodeHandler AddNode<T>(ElementID id, Registry.Registry registry) where T : Registry.Defs.INodeDefinitionBuilder;
        public NodeHandler AddNode(RegistryKey key, ElementID id, Registry.Registry registry);
        public bool ReconcretizeNode(ElementID id, Registry.Registry registry);
        public void RemoveNode(ElementID id);
        public IEdgeHandler AddEdge(ElementID portA, ElementID portB);
        public void RemoveEdge(ElementID portA, ElementID portB);
        public IEnumerable<NodeHandler> GetConnectedNodes(ElementID node);
        public IEnumerable<PortHandler> GetConnectedPorts(ElementID port);
    }
}
