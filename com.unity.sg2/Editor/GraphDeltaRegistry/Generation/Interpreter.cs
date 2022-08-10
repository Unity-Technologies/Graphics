using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using static UnityEditor.ShaderGraph.GraphDelta.ContextEntryEnumTags;

namespace UnityEditor.ShaderGraph.Generation
{
// TODO: Interpreter should be refactored to cache processing state that is shared across
// many calls to the interpreter. When Async preview goes live especially, there will be a
// lot of repeated work in sorting and processing of nodes, many of which can be cached and
// stitched on demand at various levels. An interpreter that could accept change notifications,
// eg. when topological changes occur and the halo of those changes, it would be possible to even cache
// the ShaderFoundry objects as well.
    public static class Interpreter
    {
        private class ShaderFunctionRegistry : List<ShaderFunction>
        {
            public void EnsureFunctionPresent(ShaderFunction function)
            {
                bool alreadyContained = false;
                foreach(var f in this)
                {
                    if(FunctionsAreEqual(f, function))
                    {
                        alreadyContained = true;
                        break;
                    }
                }
                if(!alreadyContained)
                {
                    Add(function);
                }
            }

            public void EnsureFunctionsPresent(IEnumerable<ShaderFunction> functions)
            {
                foreach(var f in functions)
                {
                    EnsureFunctionPresent(f);
                }
            }

        }
        /// <summary>
        /// There's a collection of required and potentially useful pieces of data
        /// that are cached as a part of the preview management.
        /// Things like the compiled shader, material, the shader code, etc. This
        /// function caches the code local only to the node, whereas the subsequent
        /// function GetBlockCode provides the code for all upstream nodes.
        public static string GetFunctionCode(NodeHandler node, Registry registry)
        {
            var builder = new ShaderBuilder();
            //List<ShaderFunction> dependencies = new();
            var func = registry.GetNodeBuilder(node.GetRegistryKey()).GetShaderFunction(node, new ShaderContainer(), registry, out var dependencies);
            builder.AddDeclarationString(func);
            if (dependencies.localFunctions != null)
            {
                foreach (var dep in dependencies.localFunctions)
                    builder.AddDeclarationString(dep);
            }
            return builder.ConvertToString();
        }

        public static string GetBlockCode(NodeHandler node, GraphHandler graph, Registry registry, ref List<(string, UnityEngine.Texture)> defaultTextures)
        {
            var builder = new ShaderBuilder();
            var container = new ShaderContainer();
            var scpBuilder = new CustomizationPointInstance.Builder(container, CustomizationPoint.Invalid);
            var vcpBuilder = new CustomizationPointInstance.Builder(container, CustomizationPoint.Invalid);
            EvaluateGraphAndPopulateDescriptors(node, graph, container, registry,ref vcpBuilder, ref scpBuilder, ref defaultTextures, null, null);
            foreach (var b in scpBuilder.BlockInstances)
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
            var vertexDescBuilder = new CustomizationPointInstance.Builder(container, vertexCP);
            EvaluateGraphAndPopulateDescriptors(node, graph, container, registry, ref vertexDescBuilder, ref surfaceDescBuilder, ref defaultTextures, vertexCP.Name, surfaceCP.Name);
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

        static void EvaluateBlockReferrables(
            NodeHandler rootNode,
            Registry registry,
            ShaderContainer container,
            ref List<StructField> outputVariables,
            ref List<StructField> inputVariables,
            ref List<(string, UnityEngine.Texture)> defaultTextures)
        {
            var isContext = rootNode.HasMetadata("_contextDescriptor");
            foreach (var port in rootNode.GetPorts())
            {
                if (port.IsHorizontal && (isContext ? port.IsInput : !port.IsInput))
                {
                    BuildPropertyAttributes(port, registry, container, ref outputVariables, ref inputVariables, ref defaultTextures);
                }
            }
        }

        static void BuildPropertyAttributes(
            PortHandler port,
            Registry registry,
            ShaderContainer container,
            ref List<StructField> outputVariables,
            ref List<StructField> inputVariables,
            ref List<(string, UnityEngine.Texture)> defaultTextures)
        {
            var name = port.ID.LocalPath;
            var portTypeField = port.GetTypeField();
            var shaderType = registry.GetTypeBuilder(portTypeField.GetRegistryKey()).GetShaderType(portTypeField, container, registry);
            var varOutBuilder = new StructField.Builder(container, name, shaderType);

            // TODO: This entire step should be deferred to the Type to determine how to process the Property rules,
            // and also warn on bad property rules.
            if (port.GetTypeField().GetRegistryKey().Name == SamplerStateType.kRegistryKey.Name)
            {
                var inVar = SamplerStateType.UniformPromotion(port.GetTypeField(), container, registry);
                inputVariables.Add(inVar);
                outputVariables.Add(varOutBuilder.Build());
                return;
            }

            var usage = PropertyBlockUsage.Excluded;
            var usageField = port.GetField <PropertyBlockUsage>(kPropertyBlockUsage);
            if(usageField != null)
            {
                usage = usageField.GetData();
            }
            PropertyAttribute propertyData = null;
            switch (usage)
            {
                case PropertyBlockUsage.Included:
                    propertyData = new PropertyAttribute { DefaultValue = port.GetDefaultValueString(registry, container), DisplayName = port.GetDisplayNameString(), Exposed = true };
                    var varInBuilder = new StructField.Builder(container, name, shaderType);

                    if (portTypeField.GetRegistryKey().Name == GraphType.kRegistryKey.Name)
                    {
                        var isColor = GraphTypeHelpers.GetLength(portTypeField) >= GraphType.Length.Three &&
                            (port.GetField<bool>(kIsColor)?.GetData() ?? false);

                        if (isColor)
                        {
                            varInBuilder.AddAttribute(new ShaderAttribute.Builder(container, "Color").Build());
                            if (port.GetField<bool>(kIsHdr)?.GetData() ?? false)
                            {
                                varInBuilder.AddAttribute(new ShaderAttribute.Builder(container, "HDR").Build());
                            }
                        }
                    }

                    SimpleSampleBuilder.MarkAsProperty(container, varInBuilder, propertyData);
                    inputVariables.Add(varInBuilder.Build());
                    break;
                case PropertyBlockUsage.Excluded:
                    var source = DataSource.Constant;
                    var sourceField = port.GetField<DataSource>(kDataSource);
                    if (sourceField != null)
                    {
                        source = sourceField.GetData();
                    }
                    if(source != DataSource.Constant)
                    {
                        propertyData = new PropertyAttribute { DataSource = UniformDataSource.Global, UniformName = port.LocalID, Exposed = false };
                        var varInputBuilder = new StructField.Builder(container, name, shaderType);
                        SimpleSampleBuilder.MarkAsProperty(container, varInputBuilder, propertyData);
                        inputVariables.Add(varInputBuilder.Build());
                    }
                    break;
                default:
                    break;
            }

            if (portTypeField.GetRegistryKey().Name == BaseTextureType.kRegistryKey.Name)
            {
                var fieldHandler = port.GetTypeField();
                var tex = BaseTextureType.GetTextureAsset(fieldHandler);
                if (tex != null && !defaultTextures.Contains((name, tex)))
                    defaultTextures.Add((name, tex));
            }


            outputVariables.Add(varOutBuilder.Build());
        }

        internal static void EvaluateGraphAndPopulateDescriptors(
                NodeHandler rootNode,
                GraphHandler shaderGraph,
                ShaderContainer container,
                Registry registry,
                ref CustomizationPointInstance.Builder vertexDescBuilder,
                ref CustomizationPointInstance.Builder surfaceDescBuilder,
                ref List<(string, UnityEngine.Texture)> defaultTextures,
                string vertexName,
                string surfaceName,
                Dictionary<ElementID, HashSet<ElementID>> depList = null
            )
        {
            depList ??= GraphHandlerUtils.BuildDependencyList(shaderGraph);
            var topoList = GraphHandlerUtils.GetUpstreamNodesTopologically(shaderGraph, rootNode.ID, depList, true).Select(e => new NodeHandler(e, shaderGraph.graphDelta, registry)).ToList();


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
            EvaluateBlockReferrables(rootNode, registry, container, ref outputVariables, ref inputVariables, ref defaultTextures);

            //Create output type from evaluated root node outputs
            var outputType = BuildStructFromVariables(container, $"{BlockName}Output", outputVariables, blockBuilder);


            var mainBodyFunctionBuilder = new ShaderFunction.Builder(container, $"SYNTAX_{rootNode.ID.LocalPath}Main", outputType);

            var shaderFunctions = new ShaderFunctionRegistry();
            var includes = new List<ShaderFoundry.IncludeDescriptor>();
            foreach(var node in topoList)
            {
                //if the node is a context node (and not the root node) we recurse
                if (node.HasMetadata("_contextDescriptor"))
                {
                    if (!node.ID.Equals(rootNode.ID))
                    {
                        //evaluate the upstream context's block
                        EvaluateGraphAndPopulateDescriptors(node, shaderGraph, container, registry, ref vertexDescBuilder, ref surfaceDescBuilder, ref defaultTextures, vertexName, surfaceName, depList);
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
                    ProcessNode(node, ref container, ref inputVariables, ref outputVariables, ref defaultTextures, ref blockBuilder, ref mainBodyFunctionBuilder, ref shaderFunctions, ref includes, registry);
                }
            }

            // Should get us every uniquely defined parameter-- not sure how this handles intrinsics- ehh...
            var shaderTypes = shaderFunctions.SelectMany(e => e.Parameters).Select(p => p.Type).ToHashSet();
            foreach(var type in shaderTypes)
            {
                blockBuilder.AddType(type);
            }
             // TODO: https://github.com/Unity-Technologies/Graphics/pull/7079 and following changes in Graphics repo
            // will allow includes to be added directly to ShaderFunctions instead.
            foreach (var include in includes)
            {
                blockBuilder.AddInclude(include);
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
                                mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = {ApplyConversionAndReturnConvertedVariable(connectedPort, port, registry, container, ref mainBodyFunctionBuilder, port.GetShaderType(registry, container).Name, ref shaderFunctions)};");
                            }
                        }
                        else // not connected.
                        {
                            EvaluateContextOutputInitialization(mainBodyFunctionBuilder, port, outputVariables[varIndex++].Name, registry);
                        }
                    }
                    else if(!port.IsInput && !isContext)
                    {
                        mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = SYNTAX_{rootNode.ID.LocalPath}_{port.ID.LocalPath};");
                    }
                }
            }

            foreach (var func in shaderFunctions)
            {
                blockBuilder.AddFunction(func);
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
            var cpName = rootNode.GetField<string>("_CustomizationPointName");
            if (!isContext || cpName == null || (cpName != null && cpName.GetData().Equals(surfaceName)))
            {
                // Prevent duplicates-- by why are we even getting any?
                if (!surfaceDescBuilder.BlockInstances.Any(e => e.Block.Name == blockDesc.Block.Name))
                    surfaceDescBuilder.BlockInstances.Add(blockDesc);
            }
            else if(isContext && cpName != null && cpName.GetData().Equals(vertexName))
            {
                if (!vertexDescBuilder.BlockInstances.Any(e => e.Block.Name == blockDesc.Block.Name))
                    vertexDescBuilder.BlockInstances.Add(blockDesc);
            }

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

        private static void EvaluateContextOutputInitialization(ShaderFunction.Builder mainBodyFunctionBuilder, PortHandler port, string name, Registry registry)
        {
            var field = port.GetTypeField();

            var source = DataSource.Constant;
            var sourceField = port.GetField<DataSource>(kDataSource);
            if (sourceField != null)
            {
                source = sourceField.GetData();
            }
            // basically, unless this is a completely inlined/constant value, this is just a passthrough from somewhere else.
            if (source == DataSource.Constant)
            {
                mainBodyFunctionBuilder.AddLine($"output.{name} = {registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(field, registry)};");
            }
            else
            {
                mainBodyFunctionBuilder.AddLine($"output.{name} = In.{name};");
            }

        }

        private static ShaderType EvaluateShaderType(ContextEntry entry, ShaderContainer container)
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

        private static string ApplyConversionAndReturnConvertedVariable(PortHandler fromPort,
                                                                        PortHandler toPort,
                                                                        Registry registry,
                                                                        ShaderContainer container,
                                                                        ref ShaderFunction.Builder mainBodyFunctionBuilder,
                                                                        string convertedTypeName,
                                                                        ref ShaderFunctionRegistry shaderFunctions,
                                                                        string toConvertNameOverride = null)
        {
            var connectedNode = fromPort.GetNode();
            var toConvert = $"SYNTAX_{connectedNode.ID.LocalPath}_{fromPort.ID.LocalPath}";
            if(toConvertNameOverride != null)
            {
                toConvert = toConvertNameOverride;
            }
            var converted = $"CONVERT_{toPort.GetNode().ID.LocalPath}_{toPort.ID.LocalPath}";
            var cast = registry.GetCast(fromPort, toPort);
            var castFunction = cast.GetShaderCast(fromPort.GetTypeField(), toPort.GetTypeField(), container, registry);

            mainBodyFunctionBuilder.AddLine($"{convertedTypeName} {converted};");
            mainBodyFunctionBuilder.AddLine($"{castFunction.Name}({toConvert}, {converted});");
            shaderFunctions.EnsureFunctionPresent(castFunction);
            return converted;

        }

        private static void ProcessNode(NodeHandler node,
            ref ShaderContainer container,
            ref List<StructField> inputVariables,
            ref List<StructField> outputVariables,
            ref List<(string, UnityEngine.Texture)> defaultTextures, // replace this with a generalized default properties solution.
            ref Block.Builder blockBuilder,
            ref ShaderFunction.Builder mainBodyFunctionBuilder,
            ref ShaderFunctionRegistry shaderFunctions,
            ref List<ShaderFoundry.IncludeDescriptor> includes,
            Registry registry)
        {
            var nodeBuilder = registry.GetNodeBuilder(node.GetRegistryKey());
            List<ShaderFoundry.IncludeDescriptor> localIncludes = new();
            var func = nodeBuilder.GetShaderFunction(node, container, registry, out var dependencies);


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
                        PortHandler connectedPort = null;
                        using (var enumerator = port.GetConnectedPorts().GetEnumerator())
                        {
                            if (enumerator.MoveNext())
                            {
                                connectedPort = enumerator.Current;
                            }
                        }
                        if (connectedPort != null) // connected input port-
                        {
                            var connectedNode = connectedPort.GetNode();
                            if (connectedNode.HasMetadata("_contextDescriptor"))
                            {
                                argument = ApplyConversionAndReturnConvertedVariable(connectedPort,
                                                                                     port,
                                                                                     registry,
                                                                                     container,
                                                                                     ref mainBodyFunctionBuilder,
                                                                                     param.Type.Name,
                                                                                     ref shaderFunctions,
                                                                                     $"In.{connectedPort.ID.LocalPath.Replace("out_", "")}");
                            }
                            else
                            {
                                argument = ApplyConversionAndReturnConvertedVariable(connectedPort, port, registry, container, ref mainBodyFunctionBuilder, param.Type.Name, ref shaderFunctions);
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
                                var name = ITypeDefinitionBuilder.GetUniqueUniformName(fieldHandler);
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


            // Process functions and prevent from adding duplicates
            if (dependencies.localFunctions != null)
                shaderFunctions.EnsureFunctionsPresent(dependencies.localFunctions);
            shaderFunctions.EnsureFunctionPresent(func);

            // Process includes and prevent from adding duplicates
            if (dependencies.includes != null)
                localIncludes.AddRange(dependencies.includes);
            foreach (var include in localIncludes)
            {
                bool shouldAdd = true;
                foreach (var existing in includes)
                {
                    if (existing.Value == include.Value)
                    {
                        shouldAdd = false;
                        break;
                    }
                }
                if (shouldAdd)
                {
                    includes.Add(include);
                }
            }
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
                        if (port.IsInput)
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


        internal static void GenerateSubgraphBody(
            NodeHandler root,
            ShaderContainer container,
            ref ShaderFunction.Builder builder,
            ref List<ShaderFunction> shaderFunctions,
            ref List<ShaderFoundry.IncludeDescriptor> includes)
        {
            var registry = root.Registry;

            // For each node, we need to add a function call to the body.
            foreach (var node in GatherTreeLeafFirst(root))
            {
                if (node.GetRegistryKey().Name == ContextBuilder.kRegistryKey.Name
                || node.GetRegistryKey().Name == ReferenceNodeBuilder.kRegistryKey.Name)
                    continue;

                var nodeBuilder = registry.GetNodeBuilder(node.GetRegistryKey());
                var func = nodeBuilder.GetShaderFunction(node, container, root.Registry, out var dependencies);

                // build up our dependencies...
                if (dependencies.includes != null)
                    includes.Union(dependencies.includes);
                if (dependencies.localFunctions != null)
                    shaderFunctions.Union(dependencies.localFunctions);
                if (!shaderFunctions.Contains(func))
                    shaderFunctions.Add(func);

                // gather up the arguments for this subnode
                List<string> arguments = new();
                foreach (var parameter in func.Parameters)
                {
                    var port = node.GetPort(parameter.Name);

                    var argument = $"sg_{node.ID.LocalPath}_{port.ID.LocalPath}";
                    string initializer = null;
                    var connectedPort = port.GetConnectedPorts().FirstOrDefault();
                    var connectedNode = connectedPort?.GetNode() ?? null;
                    bool addLocalVariable = true;

                    bool isReference = connectedNode?.GetRegistryKey().Name == ReferenceNodeBuilder.kRegistryKey.Name;
                    bool isContextOut = connectedNode?.GetRegistryKey().Name == ContextBuilder.kRegistryKey.Name;

                    // if it's a reference node, we can use the name of the contextEntry port, which is the name of the input port on the rootnode.
                    if (port.IsInput && isReference)
                    {
                        //root is providing the argument, but still goes through a reference node.
                        argument = connectedNode.GetPort(ReferenceNodeBuilder.kContextEntry).GetConnectedPorts().First().LocalID.Replace("out_", "");
                        addLocalVariable = false;
                    }
                    // a normal connect-- we just use the normally generated argument name, but from the connected node/port.
                    else if (port.IsInput && connectedPort != null)
                    {
                        argument = $"sg_{connectedNode.ID.LocalPath}_{connectedPort.ID.LocalPath}";

                    }
                    // not connected, so we need an initializer to setup the default value.
                    else if (port.IsInput)
                    {
                        initializer = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(port.GetTypeField(), registry);
                    }
                    // we are connected to a subgraph output, which means we can just use the connected ports name directly as the argument.
                    else if (!port.IsInput && isContextOut)
                    {
                        argument = "out_" + connectedPort.LocalID;
                        addLocalVariable = false;
                    }

                    arguments.Add(argument);
                    if (addLocalVariable)
                        builder.AddVariableDeclarationStatement(parameter.Type, argument, initializer);
                }
                builder.CallFunction(func, arguments.ToArray());
            }
        }
    }
}
