using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphDelta : IGraphHandler
    {
        public const string k_concrete = "Concrete";
        public const string k_user = "User";

        internal readonly GraphStorage m_data;
        public IEnumerable<NodeHandler> ContextNodes
        {
            get
            {
                foreach(var id in contextNodes)
                {
                    yield return m_data.GetHandler(id) as NodeHandler;
                }
            }
        }

        private const string kRegistryKeyName = "_RegistryKey";
        public GraphDelta()
        {
            m_data = new GraphStorage();
        }

        private List<ElementID> contextNodes = new List<ElementID>();

        NodeHandler IGraphHandler.AddNode<T>(ElementID id, Registry.Registry registry)//  where T : Registry.Defs.INodeDefinitionBuilder
        {
           var key = Registry.Registry.ResolveKey<T>();
           return AddNode(key, id, registry);
        }

        public NodeHandler AddNode(RegistryKey key, ElementID id, Registry.Registry registry)
        {
            var builder = registry.GetNodeBuilder(key);
            if (builder is ContextBuilder cb)
            {
                return AddContextNode(key, registry);
            }

            var nodeHandler = m_data.AddNodeHandler(k_user, id);
            nodeHandler.SetMetadata(kRegistryKeyName, key);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.Type)
            {
                var portHandler = nodeHandler.AddPort("Out", false, true);
                portHandler.SetMetadata(kRegistryKeyName, key);
            }

            builder.BuildNode(nodeHandler, registry);

            return nodeHandler;
        }

        public NodeHandler AddContextNode(RegistryKey contextDescriptorKey, Registry.Registry registry)
        {
            var nodeHandler = m_data.AddNodeHandler(k_user, contextDescriptorKey.Name);
            var contextKey = Registry.Registry.ResolveKey<ContextBuilder>();
            var builder = registry.GetNodeBuilder(contextKey);

            nodeHandler.SetMetadata("_contextDescriptor", contextDescriptorKey);

            nodeHandler.SetMetadata(kRegistryKeyName, contextKey);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.Type)
            {
                nodeHandler.AddPort("Out", false, true).SetMetadata(kRegistryKeyName, contextKey);
            }

            builder.BuildNode(nodeHandler, registry);

            return nodeHandler;

        }

        public void SetupContextNodes(IEnumerable<IContextDescriptor> contextDescriptors, Registry.Registry registry)
        {
            foreach(var descriptor in contextDescriptors)
            {
                AppendContextBlockToStage(descriptor, registry);
            }
        }

        public void AppendContextBlockToStage(IContextDescriptor contextDescriptor, Registry.Registry registry)
        {
            var contextNodeHandler = AddContextNode(contextDescriptor.GetRegistryKey(), registry);

            HookupToContextList(contextNodeHandler);
            ReconcretizeNode(contextNodeHandler.ID, registry);
        }

        private void HookupToContextList(NodeHandler newContextNode)
        {
            if(contextNodes.Count == 0)
            {
                contextNodes.Add(newContextNode.ID);
            }
            else
            {
                var last = contextNodes[contextNodes.Count - 1];
                var tailHandler = m_data.GetHandler(last) as NodeHandler;
                tailHandler.AddPort("Out", false, false);
                newContextNode.AddPort("In", true, false);
                
            }
        }

        public bool ReconcretizeNode(ElementID id, Registry.Registry registry)
        {
            var nodeHandler = m_data.GetHandler(id) as NodeHandler;
            var key = nodeHandler.GetMetadata<RegistryKey>(kRegistryKeyName);
            var builder = registry.GetNodeBuilder(key);
            nodeHandler.ClearLayerData(k_concrete);
            builder.BuildNode(nodeHandler, registry);
            return builder != null;
        }

        public IEnumerable<NodeHandler> GetNodes()
        {
            throw new System.NotImplementedException();
        }

        public NodeHandler GetNode(ElementID id)
        {
            return m_data.GetHandler(id) as NodeHandler;
        }

        public void RemoveNode(ElementID id)
        {
            m_data.RemoveHandler(k_user, id);
        }

        public IEdgeHandler AddEdge(ElementID portA, ElementID portB)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveEdge(ElementID portA, ElementID portB)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<NodeHandler> GetConnectedNodes(ElementID node)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<PortHandler> GetConnectedPorts(ElementID port)
        {
            throw new System.NotImplementedException();
        }

    }
}
