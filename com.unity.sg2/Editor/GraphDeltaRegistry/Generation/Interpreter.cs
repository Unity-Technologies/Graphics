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

        public static string GetBlockCode(NodeHandler node, GraphHandler graph, Registry registry, ref List<(string, UnityEngine.Texture)> defaultTextures)
        {
            var builder = new ShaderBuilder();
            var container = new ShaderContainer();
            var cpBuilder = new CustomizationPointInstance.Builder(container, CustomizationPoint.Invalid);
            EvaluateGraphAndPopulateDescriptors(node, graph, container, registry, ref cpBuilder, ref defaultTextures);
            foreach (var b in cpBuilder.BlockInstances)
                foreach(var func in b.Block.Functions)
                    builder.AddDeclarationString(func);
            return builder.ConvertToString();
        }

        private static void GetBlocks(ShaderContainer    container,
                              CustomizationPoint vertexCP,
                              CustomizationPoint surfaceCP,
                              NodeHandler node,
                              GraphHandler graph,
                              Registry registry,
                              ref List<(string, UnityEngine.Texture)> defaultTextures,
                          out CustomizationPointInstance vertexCPDesc,
                          out CustomizationPointInstance surfaceCPDesc)
        {
            vertexCPDesc = CustomizationPointInstance.Invalid; // we currently do not use the vertex customization point

            var surfaceDescBuilder = new CustomizationPointInstance.Builder(container, surfaceCP);
            EvaluateGraphAndPopulateDescriptors(node, graph, container, registry, ref surfaceDescBuilder, ref defaultTextures);
            surfaceCPDesc = surfaceDescBuilder.Build();
        }


        public static string GetShaderForNode(NodeHandler node, GraphHandler graph, Registry registry, out List<(string, UnityEngine.Texture)> defaultTextures)
        {
            List<(string, UnityEngine.Texture)> defaults = new();
            void lambda(ShaderContainer container, CustomizationPoint vertex, CustomizationPoint fragment, out CustomizationPointInstance vertexCPDesc, out CustomizationPointInstance fragmentCPDesc)
                => GetBlocks(container, vertex, fragment, node, graph, registry, ref defaults, out vertexCPDesc, out fragmentCPDesc);
            var shader = SimpleSampleBuilder.Build(new ShaderContainer(), SimpleSampleBuilder.GetTarget(), "Test", lambda, String.Empty);

            defaultTextures = new();
            defaultTextures.AddRange(defaults);
            return shader.codeString;
        }

        private static ShaderType BuildStructFromVariables(ShaderContainer container, string name, IEnumerable<StructField> variables, Block.Builder blockBuilder)
        {
            var structBuilder = new ShaderType.StructBuilder(blockBuilder, name);

            foreach (var field in variables)
                structBuilder.AddField(field);

            return structBuilder.Build();
        }


        internal static void EvaluateGraphAndPopulateDescriptors(NodeHandler rootNode, GraphHandler shaderGraph, ShaderContainer container, Registry registry, ref CustomizationPointInstance.Builder surfaceDescBuilder, ref List<(string, UnityEngine.Texture)> defaultTextures)
        {

            /* PSEDUOCODE
             * Given a root node and graph, we need to know if the node isn't a context node.
             *      If the node isnt a context node, then we know we will need a remap block from the
             *      output of the node to "BaseColor" for previews
             *
             * In either case, we then need to create the block outputs for the given node based on its ports
             *
             * We want to gather all upstream nodes from the rootnode, and start evaluating the expression tree
             *
             * If we hit a context node upstream, then we need to recurse on that context node and make sure its outputs
             *   match our expected inputs
             */


            string BlockName = $"ShaderGraphBlock_{rootNode.ID.LocalPath}";
            var blockBuilder = new Block.Builder(container, BlockName);

            var inputVariables = new List<StructField>();
            var outputVariables = new List<StructField>();
            bool isContext = rootNode.HasMetadata("_contextDescriptor");
            //Evaluate outputs for this block based on root nodes "outputs/endpoints" (horizontal input ports)
            foreach (var port in rootNode.GetPorts())
            {
                if (port.IsHorizontal && (isContext ? port.IsInput : !port.IsInput))
                {
                    var name = port.ID.LocalPath;
                    var type = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetShaderType(port.GetTypeField(), container, registry);
                    var varOutBuilder = new StructField.Builder(container, name, type);
                    var varOut = varOutBuilder.Build();
                    outputVariables.Add(varOut);
                }
            }
            //Create output type from evaluated root node outputs
            var outputType = BuildStructFromVariables(container, $"{BlockName}Output", outputVariables, blockBuilder);


            var mainBodyFunctionBuilder = new ShaderFunction.Builder(container, $"SYNTAX_{rootNode.ID.LocalPath}Main", outputType);

            var shaderFunctions = new List<ShaderFunction>();
            foreach(var node in GatherTreeLeafFirst(rootNode))
            {
                //if the node is a context node (and not the root node) we recurse
                if (node.HasMetadata("_contextDescriptor"))
                {
                    if (!node.ID.Equals(rootNode.ID))
                    {
                        //evaluate the upstream context's block
                        EvaluateGraphAndPopulateDescriptors(node, shaderGraph, container, registry, ref surfaceDescBuilder, ref defaultTextures);
                        //create inputs to our block based on the upstream context's outputs
                        foreach (var port in node.GetPorts())
                        {
                            if (port.IsHorizontal && port.IsInput)
                            {
                                var name = port.ID.LocalPath;
                                var type = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetShaderType(port.GetTypeField(), container, registry);
                                var varInBuilder = new StructField.Builder(container, name, type);
                                var varIn = varInBuilder.Build();
                                inputVariables.Add(varIn);
                            }
                        }
                    }

                }
                else
                {
                    ProcessNode(node, ref container, ref inputVariables, ref outputVariables, ref defaultTextures, ref blockBuilder, ref mainBodyFunctionBuilder, ref shaderFunctions, registry);
                }
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
            int varIndex = 0;
            foreach(PortHandler port in rootNode.GetPorts())
            {
                if(port.IsHorizontal)
                {
                    if (port.IsInput && isContext)
                    {
                        //var entry = port.GetTypeField().GetSubField<IContextDescriptor.ContextEntry>(Registry.Types.GraphType.kEntry).GetData();
                        var connectedPort = port.GetConnectedPorts().FirstOrDefault();
                        if (connectedPort != null) // connected input port-
                        {
                            var connectedNode = connectedPort.GetNode();
                            if (connectedNode.HasMetadata("_contextDescriptor"))
                            {
                                mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = In.{connectedPort.ID.LocalPath.Replace("out_","")};");
                            }
                            else
                            {
                                mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = SYNTAX_{connectedNode.ID.LocalPath}_{connectedPort.ID.LocalPath};");
                            }
                        }
                        else // not connected.
                        {
                            var field = port.GetTypeField();
                            // get the inlined port value as an initializer from the definition-- since there was no connection).
                            mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = {registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(field, registry)};");
                        }
                    }
                    else if(!port.IsInput && !isContext)
                    {
                        mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = SYNTAX_{rootNode.ID.LocalPath}_{port.ID.LocalPath};");
                    }
                }
            }

            mainBodyFunctionBuilder.AddLine("return output;");

            // Setup the block from the inputs, outputs, types, functions
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            mainBodyFunctionBuilder.AddInput(inputType, "In");
            blockBuilder.SetEntryPointFunction(mainBodyFunctionBuilder.Build());
            var block = blockBuilder.Build();
            var blockDescBuilder = new BlockInstance.Builder(container, block);
            var blockDesc = blockDescBuilder.Build();
            surfaceDescBuilder.BlockInstances.Add(blockDesc);

            //if the root node was not a context node, then we need to remap an output to the expected customization point output
            if (!isContext)
            {

                string remapBlockName = $"ShaderGraphBlock_{rootNode.ID.LocalPath}_REMAP";
                var remapBuilder = new Block.Builder(container, remapBlockName);

                var remapFromVariables = outputVariables;
                var remapToVariables = new List<StructField>();


                var colorOutBuilder = new StructField.Builder(container, "BaseColor", container._float3);
                var colorOut = colorOutBuilder.Build();
                remapToVariables.Add(colorOut);

                var remapInputType  = BuildStructFromVariables(container, $"{remapBlockName}Input",  remapFromVariables, remapBuilder);
                var remapOutputType = BuildStructFromVariables(container, $"{remapBlockName}Output", remapToVariables,   remapBuilder);

                var remapMainBodyFunctionBuilder = new ShaderFunction.Builder(container, $"SYNTAX_{remapBlockName}Main", remapOutputType);
                remapMainBodyFunctionBuilder.AddInput(remapInputType, "inputs");
                remapMainBodyFunctionBuilder.AddLine($"{remapBlockName}Block::{remapOutputType.Name} output;");
                var remap = remapFromVariables.FirstOrDefault();
                remapMainBodyFunctionBuilder.AddLine($"output.BaseColor = {ConvertToFloat3(remap.Type, $"inputs.{remap.Name}")};");
                remapMainBodyFunctionBuilder.AddLine("return output;");

                remapBuilder.AddType(remapInputType);
                remapBuilder.AddType(remapOutputType);
                remapBuilder.SetEntryPointFunction(remapMainBodyFunctionBuilder.Build());

                var remapBlock = remapBuilder.Build();
                var remapBlockDescBuilder = new BlockInstance.Builder(container, remapBlock);
                var remapBlockDesc = remapBlockDescBuilder.Build();
                surfaceDescBuilder.BlockInstances.Add(remapBlockDesc);

            }

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
            ref List<StructField> inputVariables,
            ref List<StructField> outputVariables,
            ref List<(string, UnityEngine.Texture)> defaultTextures, // replace this with a generalized default properties solution.
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
                            if (connectedNode.HasMetadata("_contextDescriptor"))
                            {
                                argument = $"In.{connectedPort.ID.LocalPath.Replace("out_", "")}";
                            }
                            else
                            {
                                argument = $"SYNTAX_{connectedNode.ID.LocalPath}_{connectedPort.ID.LocalPath}";
                            }
                        }
                        else // not connected.
                        {
                            // get the inlined port value as an initializer from the definition-- since there was no connection).
                            argument = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(port.GetTypeField(), registry);

                            // TODO: Property/Uniform promotion should be generalized and also should ensure that all promoted fields/uniforms are unique.
                            if (port.GetTypeField().GetRegistryKey().Name == BaseTextureType.kRegistryKey.Name)
                            {
                                var fieldHandler = port.GetTypeField();
                                var field = BaseTextureType.UniformPromotion(port.GetTypeField(), container, registry);
                                inputVariables.Add(field);

                                var tex = BaseTextureType.GetTextureAsset(fieldHandler);
                                var name = BaseTextureType.GetUniqueUniformName(fieldHandler);
                                if (tex != null && !defaultTextures.Contains((name, tex)))
                                    defaultTextures.Add((name, tex));
                            }

                            if (port.GetTypeField().GetRegistryKey().Name == SamplerStateType.kRegistryKey.Name)
                            {
                                var fieldHandler = port.GetTypeField();
                                var field = SamplerStateType.UniformPromotion(port.GetTypeField(), container, registry);
                                inputVariables.Add(field);
                            }
                        }
                    }
                    else // this is an output port.
                    {
                        argument = $"SYNTAX_{node.ID.LocalPath}_{port.ID.LocalPath}"; // add to the arguments for the function call.
                        // default initialize this before our function call.
                        mainBodyFunctionBuilder.AddLine($"{param.Type.Name} {argument};");
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
                //we treat context nodes as leaves to recurse on in processing. If the node is not a context node, or if its the root node, we process normally.
                if (!check.HasMetadata("_contextDescriptor") || check.ID.Equals(rootNode.ID))
                {
                    foreach (PortHandler port in check.GetPorts())
                    {
                        if (port.IsHorizontal && port.IsInput)
                        {
                            foreach (NodeHandler connected in GetConnectedNodes(port))
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
