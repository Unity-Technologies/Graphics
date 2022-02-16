using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Defs;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Generation
{
    public static class Interpreter
    {

        public static string GetShaderForNode(NodeHandler node, IGraphHandler graph, Registry.Registry registry)
        {
            void GetBlock(ShaderContainer container, CustomizationPoint vertexCP, CustomizationPoint surfaceCP, out CustomizationPointInstance vertexCPDesc, out CustomizationPointInstance surfaceCPDesc)
            {
                var block = EvaluateGraphAndPopulateDescriptors(node, graph, container, registry);
                vertexCPDesc = CustomizationPointInstance.Invalid;

                var surfaceDescBuilder = new CustomizationPointInstance.Builder(container, surfaceCP);
                var blockDescBuilder = new BlockInstance.Builder(container, block);
                var blockDesc = blockDescBuilder.Build();
                surfaceDescBuilder.BlockInstances.Add(blockDesc);
                surfaceCPDesc = surfaceDescBuilder.Build();
            }

            var builder = new ShaderBuilder();
            SimpleSampleBuilder.Build(new ShaderContainer(), SimpleSampleBuilder.GetTarget(), "Test", GetBlock, builder);
            return builder.ToString();
        }

        internal static Block EvaluateGraphAndPopulateDescriptors(NodeHandler rootNode, IGraphHandler shaderGraph, ShaderContainer container, Registry.Registry registry)
        {
            const string BlockName = "ShaderGraphBlock";
            var blockBuilder = new Block.Builder(container, BlockName);

            var inputVariables = new List<BlockVariable>();
            var outputVariables = new List<BlockVariable>();

            bool isContext = rootNode.HasMetadata("_contextDescriptor");

            if (isContext)
            {
                foreach (var port in rootNode.GetPorts())
                {
                    if (port.IsHorizontal && port.IsInput)
                    {
                        var entry = port.GetField<IContextDescriptor.ContextEntry>(ShaderGraph.Registry.Types.GraphType.kEntry).GetData();
                        var varOutBuilder = new BlockVariable.Builder(container);
                        varOutBuilder.ReferenceName = entry.fieldName;
                        varOutBuilder.Type = EvaluateShaderType(entry, container);
                        var varOut = varOutBuilder.Build();
                        outputVariables.Add(varOut);
                    }
                }

            }
            else
            {
                var colorOutBuilder = new BlockVariable.Builder(container);
                colorOutBuilder.ReferenceName = "BaseColor";
                colorOutBuilder.Type = container._float3;
                var colorOut = colorOutBuilder.Build();
                outputVariables.Add(colorOut);
            }


            var outputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Output", outputVariables);
            var mainBodyFunctionBuilder = new ShaderFunction.Builder(container, $"SYNTAX_{rootNode.GetName()}Main", outputType);

            var shaderFunctions = new List<ShaderFunction>();
            foreach(var node in GatherTreeLeafFirst(rootNode))
            {
                ProcessNode(node, ref container, ref inputVariables, ref outputVariables, ref blockBuilder, ref mainBodyFunctionBuilder, ref shaderFunctions, registry);
            }

            foreach(var func in shaderFunctions)
            {
                blockBuilder.AddFunction(func);
            }


            var inputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Input", inputVariables);


            mainBodyFunctionBuilder.AddLine($"{outputType.Name} output;");
            if(isContext)
            {
                int varIndex = 0;
                foreach(PortReader port in rootNode.GetPorts())
                {
                    if(port.IsHorizontal() && port.IsInput())
                    {
                        port.GetField(ShaderGraph.Registry.Types.GraphType.kEntry, out Registry.Defs.IContextDescriptor.ContextEntry entry);
                        var connectedPort = port.GetConnectedPorts().FirstOrDefault();
                        if (connectedPort != null) // connected input port-
                        {
                            var connectedNode = connectedPort.GetNode();
                            mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].ReferenceName} = SYNTAX_{connectedNode.GetName()}_{connectedPort.GetName()}");
                        }
                        else // not connected.
                        {
                            // get the inlined port value as an initializer from the definition-- since there was no connection).
                            mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++]} = {registry.GetTypeBuilder(port.GetRegistryKey()).GetInitializerList((GraphDelta.IFieldReader)port, registry)}");
                        }
                    }
                }

            }
            else
            {
                mainBodyFunctionBuilder.AddLine($"output.{outputVariables[0].ReferenceName} = SYNTAX_{rootNode.GetName()}_{rootNode.GetOutputPorts().First().GetName()};");
            }
            mainBodyFunctionBuilder.AddLine("return output;");

            // Setup the block from the inputs, outputs, types, functions
            foreach (var variable in inputVariables)
                blockBuilder.AddInput(variable);
            foreach (var variable in outputVariables)
                blockBuilder.AddOutput(variable);
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            mainBodyFunctionBuilder.AddInput(inputType, "In");
            blockBuilder.SetEntryPointFunction(mainBodyFunctionBuilder.Build());
            return blockBuilder.Build();
        }

        private static ShaderType EvaluateShaderType(IContextDescriptor.ContextEntry entry, ShaderContainer container)
        {
            //length by height
            string lxh = "";
            if(entry.length > 1 || entry.height > 1)
            {
                lxh += entry.length;
            }
            if(entry.height > 1)
            {
                lxh += "x" + entry.height;
            }
            switch (entry.primitive)
            {
                case Registry.Types.GraphType.Primitive.Bool:
                    return container.GetType($"bool{lxh}");
                case Registry.Types.GraphType.Primitive.Int:
                    return container.GetType($"int{lxh}");
                case Registry.Types.GraphType.Primitive.Float:
                    if (entry.precision == Registry.Types.GraphType.Precision.Full)
                    {
                        return container.GetType($"double{lxh}");
                    }
                    else
                    {
                        return container.GetType($"float{lxh}");
                    }
                default:
                    throw new ArgumentException("unsupported type");
            }
        }

        private static bool FunctionsAreEqual(ShaderFunction a, ShaderFunction b)
        {
            if(a.Name.CompareTo(b.Name) != 0)
            {
                return false;
            }

            var aParams = a.Parameters.ToList();
            var bParams = b.Parameters.ToList();

            if(aParams.Count != bParams.Count)
            {
                return false;
            }

            for(int i = 0; i < aParams.Count(); ++i)
            {
                if(aParams[i].IsInput != bParams[i].IsInput
                || aParams[i].Type    != bParams[i].Type
                || aParams[i].IsValid != bParams[i].IsValid)//does this one need to be checked?
                {
                    return false;
                }
            }
            return true;
        }

        private static void ProcessNode(NodeReader node, ref ShaderContainer container, ref List<BlockVariable> inputVariables, ref List<BlockVariable> outputVariables, ref Block.Builder blockBuilder, ref ShaderFunction.Builder mainBodyFunctionBuilder, ref List<ShaderFunction> shaderFuncitons, Registry.Registry registry)
        {
            var func = registry.GetNodeBuilder(node.GetRegistryKey()).GetShaderFunction(node, container, registry);
            bool shouldAdd = true;
            foreach(var existing in shaderFuncitons)
            {
                if(FunctionsAreEqual(existing, func))
                {
                    shouldAdd = false;
                }
            }
            if(shouldAdd)
            {
                shaderFuncitons.Add(func);
            }
            string arguments = "";
            foreach (var param in func.Parameters)
            {
                if (node.TryGetPort(param.Name, out var port))
                {
                    string argument = "";
                    if (!port.IsHorizontal())
                        continue;
                    if (port.IsInput())
                    {
                        var connectedPort = port.GetConnectedPorts().FirstOrDefault();
                        if (connectedPort != null) // connected input port-
                        {
                            var connectedNode = connectedPort.GetNode();
                            argument = $"SYNTAX_{connectedNode.GetName()}_{connectedPort.GetName()}";
                        }
                        else // not connected.
                        {
                            // get the inlined port value as an initializer from the definition-- since there was no connection).
                            argument = registry.GetTypeBuilder(port.GetRegistryKey()).GetInitializerList((GraphDelta.FieldReader)port, registry);
                        }
                    }
                    else // this is an output port.
                    {
                        argument = $"SYNTAX_{node.GetName()}_{port.GetName()}"; // add to the arguments for the function call.
                        // default initialize this before our function call.
                        var initValue = registry.GetTypeBuilder(port.GetRegistryKey()).GetInitializerList((GraphDelta.FieldReader)port, registry);
                        mainBodyFunctionBuilder.AddLine($"{param.Type.Name} {argument} = {initValue};");
                    }
                    arguments += argument + ", ";
                }
            }
            if (arguments.Length != 0)
                arguments = arguments.Remove(arguments.Length - 2, 2); // trim the trailing ", "
            mainBodyFunctionBuilder.AddLine($"{func.Name}({arguments});"); // add our node's function call to the body we're building out.

        }

        private static IEnumerable<NodeHandler> GatherTreeLeafFirst(INodeHandler rootNode)
        {
            Stack<INodeHandler> stack = new Stack<INodeHandler>();
            HashSet<INodeHandler> visited = new HashSet<INodeHandler>();
            stack.Push(rootNode);
            while(stack.Count > 0)
            {
                INodeHandler check = stack.Peek();
                bool isLeaf = true;
                foreach(IPortHandler port in check.GetInputPorts())
                {
                    if(port.IsHorizontal)
                    {
                        foreach(INodeHandler connected in GetConnectedNodes(port))
                        {
                            if (!visited.Contains(connected))
                            {
                                visited.Add(connected);
                                isLeaf = false;
                                stack.Push(connected);
                            }
                        }
                    }
                }
                if(isLeaf)
                {
                    yield return stack.Pop();
                }
            }
        }

        private static IEnumerable<NodeHandler> GetConnectedNodes(IPortHandler port)
        {
            foreach(var connected in port.GetConnectedPorts())
            {
                yield return connected.GetNode();
            }
        }
    }
}
