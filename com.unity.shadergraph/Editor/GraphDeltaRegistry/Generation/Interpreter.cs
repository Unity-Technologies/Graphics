using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Generation
{
    public static class Interpreter
    {
        public static string GetFunctionCode(NodeHandler node, Registry registry)
        {
            var builder = new ShaderBuilder();
            var func = registry.GetNodeBuilder(node.GetRegistryKey()).GetShaderFunction(node, new ShaderContainer(), registry);
            builder.AddDeclarationString(func);
            return builder.ConvertToString();
        }

        public static string GetBlockCode(NodeHandler node, GraphHandler graph, Registry registry)
        {
            var builder = new ShaderBuilder();
            var block = EvaluateGraphAndPopulateDescriptors(node, graph, new ShaderContainer(), registry);
            foreach (var func in block.Functions)
                builder.AddDeclarationString(func);
            return builder.ConvertToString();
        }

        public static string GetShaderForNode(NodeHandler node, GraphHandler graph, Registry registry)
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

            var shader = SimpleSampleBuilder.Build(new ShaderContainer(), SimpleSampleBuilder.GetTarget(), "Test", GetBlock, String.Empty);
            return shader.codeString;
        }

        private static ShaderType BuildStructFromVariables(ShaderContainer container, string name, IEnumerable<BlockVariable> variables, Block.Builder blockBuilder)
        {
            var structBuilder = new ShaderType.StructBuilder(blockBuilder, name);
            foreach (var variable in variables)
            {
                var fieldBuilder = new StructField.Builder(container, variable.Name, variable.Type);
                var structField = fieldBuilder.Build();
                structBuilder.AddField(structField);
            }
            return structBuilder.Build();
        }


        internal static Block EvaluateGraphAndPopulateDescriptors(NodeHandler rootNode, GraphHandler shaderGraph, ShaderContainer container, Registry registry)
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
                        var entry = port.GetTypeField().GetSubField<IContextDescriptor.ContextEntry>(GraphType.kEntry).GetData();
                        var varOutBuilder = new BlockVariable.Builder(container)
                        {
                            Name = entry.fieldName,
                            Type = EvaluateShaderType(entry, container)
                        };
                        var varOut = varOutBuilder.Build();
                        outputVariables.Add(varOut);
                    }
                }

            }
            else
            {
                var colorOutBuilder = new BlockVariable.Builder(container)
                {
                    Name = "BaseColor",
                    Type = container._float3
                };
                var colorOut = colorOutBuilder.Build();
                outputVariables.Add(colorOut);
            }

            var outputType = BuildStructFromVariables(container, $"{BlockName}Output", outputVariables, blockBuilder);
            var mainBodyFunctionBuilder = new ShaderFunction.Builder(container, $"SYNTAX_{rootNode.ID.LocalPath}Main", outputType);

            var shaderFunctions = new List<ShaderFunction>();
            foreach(var node in GatherTreeLeafFirst(rootNode))
            {
                if(!isContext || (isContext && node != rootNode))
                    ProcessNode(node, ref container, ref inputVariables, ref outputVariables, ref blockBuilder, ref mainBodyFunctionBuilder, ref shaderFunctions, registry);
            }

            // Should get us every uniquely defined parameter-- not sure how this handles intrinsics- ehh...
            var shaderTypes = shaderFunctions.SelectMany(e => e.Parameters).Select(p => p.Type).ToHashSet();
            foreach(var type in shaderTypes)
            {
                blockBuilder.AddType(type);
            }
            foreach(var func in shaderFunctions)
            {
                blockBuilder.AddFunction(func);
            }

            var inputType = BuildStructFromVariables(container, $"{BlockName}Input", inputVariables, blockBuilder);

            mainBodyFunctionBuilder.AddLine($"{BlockName}Block::{outputType.Name} output;");
            if(isContext)
            {
                int varIndex = 0;
                foreach(PortHandler port in rootNode.GetPorts())
                {
                    if(port.IsHorizontal && port.IsInput)
                    {
                        var entry = port.GetTypeField().GetSubField<IContextDescriptor.ContextEntry>(GraphType.kEntry).GetData();
                        var connectedPort = port.GetConnectedPorts().FirstOrDefault();
                        if (connectedPort != null) // connected input port-
                        {
                            var connectedNode = connectedPort.GetNode();
                            mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = SYNTAX_{connectedNode.ID.LocalPath}_{connectedPort.ID.LocalPath};");
                        }
                        else // not connected.
                        {
                            var field = port.GetTypeField();
                            // get the inlined port value as an initializer from the definition-- since there was no connection).
                            mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++]} = {registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(field, registry)};");
                        }
                    }
                }

            }
            else // if node preview
            {
                var port = rootNode.GetPorts().Where(e => !e.IsInput).First(); // get the first output node
                var field = port.GetTypeField();
                var outType = registry.GetTypeBuilder(field.GetRegistryKey()).GetShaderType(field, container, registry);
                string assignment = ConvertToFloat3(outType, $"SYNTAX_{rootNode.ID.LocalPath}_{port.ID.LocalPath}");
                mainBodyFunctionBuilder.AddLine($"output.{outputVariables[0].Name} = {assignment};");
            }
            mainBodyFunctionBuilder.AddLine("return output;");

            // Setup the block from the inputs, outputs, types, functions
            //foreach (var variable in inputVariables)
            //    blockBuilder.AddInput(variable);
            //foreach (var variable in outputVariables)
            //    blockBuilder.AddOutput(variable);
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
            if((int)entry.length > 1 || (int)entry.height > 1)
            {
                lxh += (int)entry.length;
            }
            if((int)entry.height > 1)
            {
                lxh += "x" + (int)entry.height;
            }
            switch (entry.primitive)
            {
                case GraphType.Primitive.Bool:
                    return container.GetType($"bool{lxh}");
                case GraphType.Primitive.Int:
                    return container.GetType($"int{lxh}");
                case GraphType.Primitive.Float:
                    if (entry.precision == GraphType.Precision.Single)
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

        private static void ProcessNode(NodeHandler node,
            ref ShaderContainer container,
            ref List<BlockVariable> inputVariables,
            ref List<BlockVariable> outputVariables,
            ref Block.Builder blockBuilder,
            ref ShaderFunction.Builder mainBodyFunctionBuilder,
            ref List<ShaderFunction> shaderFunctions,
            Registry registry)
        {
            var nodeBuilder = registry.GetNodeBuilder(node.GetRegistryKey());
            var func = nodeBuilder.GetShaderFunction(node, container, registry);
            bool shouldAdd = true;
            foreach(var existing in shaderFunctions)
            {
                if(FunctionsAreEqual(existing, func))
                {
                    shouldAdd = false;
                }
            }
            if(shouldAdd)
            {
                shaderFunctions.Add(func);
            }
            string arguments = "";
            foreach (var param in func.Parameters)
            {
                var port = node.GetPort(param.Name);
                if (port != null)
                {
                    string argument = "";
                    if (!port.IsHorizontal)
                        continue;
                    if (port.IsInput)
                    {
                        var connectedPort = port.GetConnectedPorts().FirstOrDefault();
                        if (connectedPort != null) // connected input port-
                        {
                            var connectedNode = connectedPort.GetNode();
                            argument = $"SYNTAX_{connectedNode.ID.LocalPath}_{connectedPort.ID.LocalPath}";
                        }
                        else // not connected.
                        {
                            // get the inlined port value as an initializer from the definition-- since there was no connection).
                            argument = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(port.GetTypeField(), registry);
                        }
                    }
                    else // this is an output port.
                    {
                        argument = $"SYNTAX_{node.ID.LocalPath}_{port.ID.LocalPath}"; // add to the arguments for the function call.
                        // default initialize this before our function call.
                        var initValue = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(port.GetTypeField(), registry);
                        mainBodyFunctionBuilder.AddLine($"{param.Type.Name} {argument} = {initValue};");
                    }
                    arguments += argument + ", ";
                }
            }
            if (arguments.Length != 0)
                arguments = arguments.Remove(arguments.Length - 2, 2); // trim the trailing ", "
            mainBodyFunctionBuilder.AddLine($"{func.Name}({arguments});"); // add our node's function call to the body we're building out.
            

        }

        private static string ConvertToFloat3(ShaderType type, string name)
        {
            return type.VectorDimension switch
            {
                4 => $"{name}.xyz",
                2 => $"float3({name}.x, {name}.y, 0)",
                1 => $"float3({name}, {name}, {name})",
                _ => name,
            };
        }

        private static IEnumerable<NodeHandler> GatherTreeLeafFirst(NodeHandler rootNode)
        {
            Stack<NodeHandler> stack = new();
            HashSet<string> visited = new();
            stack.Push(rootNode);
            while(stack.Count > 0)
            {
                NodeHandler check = stack.Peek();
                bool isLeaf = true;
                foreach(PortHandler port in check.GetPorts()) //NEDS TO BE INPUT PORTS
                {
                    if(port.IsHorizontal && port.IsInput)
                    {
                        foreach(NodeHandler connected in GetConnectedNodes(port))
                        {
                            if (!visited.Contains(connected.ID.FullPath))
                            {
                                visited.Add(connected.ID.FullPath);
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

        private static IEnumerable<NodeHandler> GetConnectedNodes(PortHandler port)
        {
            foreach(var connected in port.GetConnectedPorts())
            {
                yield return connected.GetNode();
            }
        }
    }
}
