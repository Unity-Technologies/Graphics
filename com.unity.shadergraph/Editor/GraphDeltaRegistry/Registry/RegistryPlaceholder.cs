using UnityEngine;

namespace UnityEditor.ShaderGraph.Registry
{
    public class RegistryPlaceholder
    {
        public int data;

        public RegistryPlaceholder(int d) { data = d; }
    }

    namespace Exploration
    {
        public class GraphTypeDefinition : INodeDefinitionBuilder
        {
            public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GraphType", Version = 1 };
            public RegistryFlags GetRegistryFlags() => RegistryFlags.IsType;

            enum Precision { Fixed, Half, Full }



            public void BuildNode(GraphDelta.INodeReader userData, GraphDelta.INodeWriter concreteData, IRegistry registry)
            {
                // TODO: Promote to ports-- but also, reconsider a TypeDefinition interface-- yes the data is the same,
                // but conceptually we're working with fields instead of ports-- even though each field is promoted to a port,
                // it could also be a serialized value directly. Need to investigate this further with liz. I could see Type definitions
                // instead using PortWriter- since Types are specifically port associative. That means that node definitions that work with types,
                // such as a constructor node- can work with _any_ type and just promote fields to ports (or ultimately inline values).

                // TODO: Some type local extensions for interacting with this node/port type would be powerful- some sort of extension cast for writers,
                // but it can't ultimately be type strong in the storage, since the typing information is just storing the RegistryKey.
                // (That's important because it allows for indirection by the registry, which gives us overrides and versioning)
                concreteData.SetField("Precision", Precision.Full);
                concreteData.SetField("Length", 4);
                concreteData.SetField("x", 0f);
                concreteData.SetField("y", 0f);
                concreteData.SetField("z", 0f);
                concreteData.SetField("w", 0f);
            }

            public bool CanAcceptConnection(GraphDelta.INodeReader thisNode, GraphDelta.IPortReader thisPort, GraphDelta.IPortReader candidatePort)
            {
                // For now, we can only have outgoing connections, since this isn't a CTOR style node.
                return false;
            }
        }

        public class AddDefinition : INodeDefinitionBuilder
        {
            public RegistryKey GetRegistryKey() => new RegistryKey { Name = "AddNode", Version = 1 };
            public RegistryFlags GetRegistryFlags() => RegistryFlags.isFunc;

            public void BuildNode(GraphDelta.INodeReader userData, GraphDelta.INodeWriter concreteData, IRegistry registry)
            {
                concreteData.AddPort<GraphTypeDefinition>("A", true, true, registry);
                concreteData.AddPort<GraphTypeDefinition>("B", true, true, registry);
                concreteData.AddPort<GraphTypeDefinition>("Out", true, true, registry);
                // If we wanted to inline some defaults here for the ports, they would realistically need to come from a builder interface that works with ports.
                // Data-wise ports/types/nodes are the same thing, but for API purposes, separating node builder (Nodes) and port builder (Types) seems necessary.
                // -- Then a Node definition that works off of ITypeDefinitions can just walk the fields and promote them to ports for a CTOR or a Break style accessor.
            }

            public bool CanAcceptConnection(GraphDelta.INodeReader thisNode, GraphDelta.IPortReader thisPort, GraphDelta.IPortReader candidatePort)
            {
                // any graph type is acceptable for connection purposes in this case-- but this is not how these functions should be written usually.
                // Also-- type system should have an option to automagic just based on the RegistryKey typing alone.
                return thisPort.GetRegistryKey().ToString() == candidatePort.GetRegistryKey().ToString();
            }
        }
    }
}
