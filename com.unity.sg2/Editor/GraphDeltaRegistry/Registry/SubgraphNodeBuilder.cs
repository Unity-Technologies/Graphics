using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;

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
                // temp; context nodes haven't been fixed yet, so filter out these defaults for now.
                bool shouldExclude = port.LocalID.Contains("Emission")
                    || port.LocalID.Contains("NormalTS")
                    || port.LocalID.Contains("BaseColor");

                //, "NormalTS", "BaseColor"
                if (!port.IsInput && !shouldExclude) //temp: Filter context outputs.
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
            Registry registry,
            out INodeDefinitionBuilder.Dependencies outputs)
        {
            outputs = new();
            outputs.includes = new();
            outputs.localFunctions = new();

            subgraph.ReconcretizeAll();

            // TODO: allow for a function name/prefix to get added in the constructor
            var func = new ShaderFunction.Builder(container, $"SomeName");

            foreach (var port in data.GetPorts())
            {
                if (port.IsInput && port.IsHorizontal)
                {
                    var type = port.GetShaderType(registry, container);
                    func.AddInput(type, port.LocalID);
                }
            }

            var outputContextName = Registry.ResolveKey<ShaderGraphContext>().Name;
            var output = subgraph.GetNode(outputContextName);

            Interpreter.GenerateSubgraphBody(output, container, ref func, ref outputs.localFunctions, ref outputs.includes);

            foreach (var port in data.GetPorts())
            {
                if (!port.IsInput && port.IsHorizontal)
                {
                    var name = port.LocalID;
                    // the "out_" prefix is a bad smell and it permeates throughout this, contextBuilder, referenceNodeBuilder, and the Interpreter,
                    // -- Think it means we need an 'inout' port type!
                    var inName = port.LocalID.TrimStart("out_".ToCharArray());
                    var inPort = output.GetPort(inName);
                    var type = port.GetShaderType(registry, container);
                    func.AddOutput(type, name);

                    var connectedPort = inPort.GetConnectedPorts().FirstOrDefault();
                    var connectedNode = connectedPort?.GetNode() ?? null;
                    bool isReference = connectedNode?.GetRegistryKey().Name == ReferenceNodeBuilder.kRegistryKey.Name;

                    if (isReference)
                    {
                        var connectedName = connectedNode.GetPort(ReferenceNodeBuilder.kContextEntry).GetConnectedPorts().First().LocalID.Replace("out__", "_");
                        func.AddLine($"{name} = {connectedName};");
                    }
                    else if (connectedPort != null)
                    {
                        // We can assume that the body code performs the assignment-- oh that's bad.
                        // func.AddLine($"{name} = sg_{connectedNode.ID.LocalPath}_{connectedPort.ID.LocalPath};");
                    }
                    else
                    {
                        var typeBuilder = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey());
                        func.AddLine($"{name} = {typeBuilder.GetInitializerList(port.GetTypeField(), registry)};");
                    }
                }
            }

            return func.Build();
        }

        public RegistryKey GetRegistryKey() => key;

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;
    }
}
