using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    public static class GraphDeltaExtensions
    {
        private const string kRegistryKeyName = "_RegistryKey";

        public static IEnumerable<PortHandler> GetConnectedPorts(this PortHandler port)
        {
            // TODO (Liz) Implement
            throw new System.NotImplementedException();
        }

        public static RegistryKey GetRegistryKey(this NodeHandler node)
        {
            return node.GetMetadata<RegistryKey>(kRegistryKeyName);
        }

        public static RegistryKey GetRegistryKey(this PortHandler port)
        {
            return port.GetTypeField().GetMetadata<RegistryKey>(kRegistryKeyName);
        }

        public static RegistryKey GetRegistryKey(this FieldHandler field)
        {
            return field.GetMetadata<RegistryKey>(kRegistryKeyName);
        }

        public static void AddReferenceNode(this GraphHandler handler, string nodeName, string contextName, string contextEntryName, Registry registry)
        {
            var node = handler.AddNode<ReferenceNodeBuilder>(nodeName, registry);
            var inPort = node.GetPort(ReferenceNodeBuilder.kContextEntry);
            var outPort = handler.GetNode(contextName).GetPort($"out_{contextEntryName}"); // TODO: Not this.
            handler.AddEdge(outPort.ID, inPort.ID);

            handler.ReconcretizeNode(node.ID.FullPath, registry);

            // node.SetMetadata("_referenceName", contextEntryName);


            // reference nodes have some weird rules, in that they can't really fetch or achieve any sort of identity until they are connected downstream to a context node.
            // We need stronger rules around references-- namely that a reference type must be consistent across all instances of that reference within however many context nodes.

            // This is funny though-- if you change a reference's type, that means _all_ reference handles that are represented by a context input port and all reference nodes and...
            // all of their downstream nodes get propogated-- and then upstream node connections can be disrupted if the type every changes.
        }

        internal static void SetupContext(this GraphHandler handler, IEnumerable<IContextDescriptor> contexts, Registry registry)
        {
            // only safe to call right now.
            NodeHandler previousContextNode = null;
            foreach(var context in contexts)
            {
                // Initialize the Context Node with information from the ContextDescriptor
                var name = context.GetRegistryKey().Name + "_Context"; // Not like this...
                var currentContextNode = handler.AddNode<ContextBuilder>(name, registry);
                currentContextNode.SetMetadata("_contextDescriptor", context.GetRegistryKey()); // initialize the node w/a reference to the context descriptor (so it can build itself).
                if(previousContextNode != null)
                {
                    // Create the monadic connection if it should exist.
                    var outPort = previousContextNode.AddPort("Out", false, false);
                    var inPort  = currentContextNode.AddPort("In", true, false);
                    handler.AddEdge(outPort.ID, inPort.ID);
                }
                handler.ReconcretizeNode(name, registry);
                previousContextNode = currentContextNode;
            }

            var entryPoint = handler.AddNode<ContextBuilder>("EntryPoint", registry);
            var toEntry = previousContextNode.AddPort("Out", false, false);
            var inEntry = entryPoint.AddPort("In", true, false);
            handler.AddEdge(toEntry.ID, inEntry.ID);
            handler.ReconcretizeNode("EntryPoint", registry);
        }


        public static bool TestConnection(this GraphHandler handler, string srcNode, string srcPort, string dstNode, string dstPort, Registry registry)
        {
            var dstNodeHandler = handler.GetNode(dstNode);
            var dstPortHandler = dstNodeHandler.GetPort(dstPort);
            var srcPortHandler = handler.GetNode(srcNode).GetPort(srcPort);
            return registry.CastExists(dstPortHandler.GetTypeField().GetRegistryKey(), srcPortHandler.GetTypeField().GetRegistryKey());
        }

        public static bool TryConnect(this GraphHandler handler, string srcNode, string srcPort, string dstNode, string dstPort, Registry registry)
        {
            var dstNodeHandler = handler.GetNode(dstNode);
            var dstPortHandler = dstNodeHandler.GetPort(dstPort);
            var srcPortHandler = handler.GetNode(srcNode).GetPort(srcPort);
            return handler.AddEdge(srcPortHandler.ID, dstPortHandler.ID) != null;
        }

        public static void Disconnect(this GraphHandler handler, string srcNode, string srcPort, string dstNode, string dstPort, Registry registry)
        {
            var dstNodeHandler = handler.GetNode(dstNode);
            var dstPortHandler = dstNodeHandler.GetPort(dstPort);
            var srcPortHandler = handler.GetNode(srcNode).GetPort(srcPort);
            handler.RemoveEdge(srcPortHandler.ID, dstPortHandler.ID);
        }

        public static T GetField<T>(this NodeHandler node, string fieldName)
        {
            return node.GetField<T>(fieldName).GetData();
        }
        public static T GetField<T>(this PortHandler port, string fieldName)
        {
            return port.GetField<T>(fieldName).GetData();
        }
        public static T GetField<T>(this FieldHandler field, string fieldName)
        {
            return field.GetSubField<T>(fieldName).GetData();
        }

        public static bool TryGetField<T>(this NodeHandler node, string fieldName, out T data)
        {
            var field = node.GetField<T>(fieldName);
            if(field != null)
            {
                data = field.GetData();
                return true;
            }
            data = default;
            return false;
        }

        public static void GetField<T>(this PortHandler port, string fieldName, out T data)
        {
            data = port.GetField<T>(fieldName).GetData();
        }
        public static void GetField<T>(this FieldHandler field, string fieldName, out T data)
        {
            data = field.GetSubField<T>(fieldName).GetData();
        }

        public static void TryGetPort(this NodeHandler node, string portName, out PortHandler port)
        {
            port = node.GetPort(portName);
        }

        internal static PortHandler AddPort<T>(
            this NodeHandler node,
            string name,
            bool isInput,
            Registry registry) where T : ITypeDefinitionBuilder
        {
            return AddPort(node, name, isInput, Registry.ResolveKey<T>(), registry);
        }

        public static PortHandler AddPort(
            this NodeHandler node,
            string name,
            bool isInput,
            RegistryKey key,
            Registry registry)
        {
            var port = node.AddPort(name, isInput, true);
            port.AddTypeField().SetMetadata(kRegistryKeyName, key);

            var builder = registry.GetTypeBuilder(key);

            builder.BuildType(port.GetTypeField(), registry);
            return port;
        }
    }
}
