using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SubGraphNodeBuilder : INodeDefinitionBuilder
    {
        RegistryKey key;
        GraphHandler subgraph;

        internal SubGraphNodeBuilder(RegistryKey key, GraphHandler subgraph)
        {
            this.key = key;
            // Locally copy the subgraph so that we can locally manipulate it's concrete state as needed by our graph.
            this.subgraph = GraphHandler.FromSerializedFormat(subgraph.ToSerializedFormat(), subgraph.registry);
            this.subgraph.ReconcretizeAll();
        }

        public void BuildNode(NodeHandler node, Registry registry)
        {
            // TODO: Replace with Subgraph contexts instead if that's safe to use.
            var inputContextName = Registry.ResolveKey<PropertyContext>().Name;
            var outputContextName = Registry.ResolveKey<ShaderGraphContext>().Name;

            var input = subgraph.GetNode(inputContextName);
            var output = subgraph.GetNode(outputContextName);

            foreach(var port in input.GetPorts())
            {
                if (port.IsInput)
                    INodeDefinitionBuilder.CopyPort(port, node, registry);
            }
            foreach (var port in output.GetPorts())
            {
                if (!port.IsInput && !port.LocalID.StartsWith("out_")) //temp: Filter context outputs.
                    INodeDefinitionBuilder.CopyPort(port, node, registry);
            }

            // Dropdowns will need to be some kind of switch type probably, can easily make an editor/driver for that.
            // What about 'isConnected?' All nodes of that type need to be found and then we need to check against our ports here,
            // that's simple enough to do here really-- "Find all nodes of type-- then push a connected/is connected state.
            // Need to make sure the GraphHandler is _duplicated_ for each node?
        }

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(
            NodeHandler data,
            ShaderContainer container,
            Registry registry)
        {
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, key.Name);


            // TODO: Need a Subgraph -> Function helper.
            // This should maybe live in the interpreter.

            return shaderFunctionBuilder.Build();
        }

        public RegistryKey GetRegistryKey() => key;

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;
    }
}
