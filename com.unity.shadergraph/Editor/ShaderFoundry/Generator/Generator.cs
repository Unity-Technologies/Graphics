using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using Pool = UnityEngine.Pool;

namespace UnityEditor.ShaderFoundry
{
    class MyClass
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod()
        {
            //var generator = new Generator();
            ShaderGraph.Generator.Callback = Generator.BuildTemplateShader;
            Debug.Log("Before scene loaded");
        }
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            //var generator = new Generator();
            ShaderGraph.Generator.Callback = Generator.BuildTemplateShader;
        }
    }


    class Generator
    {
        static public string BuildTemplateShader(ShaderGraph.Generator generator)
        {
            var container = new ShaderFoundry.ShaderContainer();
            ShaderBuilder builder = new ShaderBuilder();

            var sandboxGenerator = new ShaderFoundry.ShaderGenerator();
            var sandboxShaderDescBuilder = new ShaderFoundry.ShaderDescriptor.Builder(container, generator.m_Name);
            sandboxShaderDescBuilder.FallbackShader = @"FallBack ""Hidden/Shader Graph/FallbackError""";

            foreach (var target in generator.m_Targets)
            {
                var legacyProvider = new ShaderFoundry.LegacyTemplateProvider(target, generator.m_assetCollection);
                var provider = legacyProvider as ShaderFoundry.ITemplateProvider;

                var templates = provider.GetTemplates(container);


                foreach (var template in templates)
                {
                    var templateDescBuilder = new ShaderFoundry.TemplateDescriptor.Builder(container, template);
                    var linker = template.Linker;
                    var legacyLinker = linker as ShaderFoundry.SandboxLegacyTemplateLinker;

                    foreach (var pass in template.Passes)
                    {
                        UnityEditor.ShaderGraph.PassDescriptor legacyPass = new PassDescriptor();
                        legacyLinker.FindLegacyPass(pass.ReferenceName, ref legacyPass);

                        BuildTemplateGraph(container, generator, templateDescBuilder, template, pass, legacyPass);
                    }
                    sandboxShaderDescBuilder.TemplateDescriptors.Add(templateDescBuilder.Build());
                }
            }
            sandboxGenerator.Generate(builder, container, sandboxShaderDescBuilder.Build());

            return builder.ToString();
        }

        static void BuildTemplateGraph(ShaderFoundry.ShaderContainer container, ShaderGraph.Generator generator, ShaderFoundry.TemplateDescriptor.Builder templateDescBuilder, ShaderFoundry.Template template, ShaderFoundry.TemplatePass passDesc, PassDescriptor legacyPassDescriptor)
        {
            var path = AssetDatabase.GUIDToAssetPath(generator.m_GraphData.assetGuid);
            var sharedDirectory = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "Shared");
            if (!Directory.Exists(sharedDirectory))
                Directory.CreateDirectory(sharedDirectory);
            var sharedFileName = $"Pass_({passDesc.PassIdentifier.m_SubShaderIndex}_{passDesc.PassIdentifier.m_PassIndex})" + "Shared.hlsl";
            var sharedPath = Path.Combine(sharedDirectory, sharedFileName);

            const string vertexGraphInputName = "VSInput";
            const string vertexGraphOutputName = "VSOutput";
            const string vertexGraphFunctionName = "applyVertex";
            const string pixelGraphInputName = "FSInput";
            const string pixelGraphOutputName = "FSOutput";
            const string pixelGraphFunctionName = "applyFragment";
            const string vertexEntryPointName = "VertexDescriptionFunction";
            const string pixelEntryPointName = "SurfaceDescriptionFunction";

            // Function Registry
            var functionBuilder = new ShaderStringBuilder();
            var graphIncludes = new IncludeCollection();
            var functionRegistry = new FunctionRegistry(functionBuilder, graphIncludes, true);

            var pass = legacyPassDescriptor;
            List<AbstractMaterialNode> vertexNodes, pixelNodes;
            List<MaterialSlot> pixelSlots, vertexSlots;

            ActiveFields activeFields = new ActiveFields();
            PropertyCollector propertyCollector = new PropertyCollector();
            KeywordCollector keywordCollector = new KeywordCollector();
            ExtractNodesNew(generator, passDesc, pass, activeFields, propertyCollector, out vertexNodes, out pixelNodes, out pixelSlots, out vertexSlots);

            // Track permutation indices for all nodes
            ShaderGraphRequirementsPerKeyword graphRequirements;
            List<int>[] vertexNodePermutations, pixelNodePermutations;
            vertexNodePermutations = new List<int>[vertexNodes.Count];
            pixelNodePermutations = new List<int>[pixelNodes.Count];
            GenerationUtils.GetActiveFieldsAndPermutationsForNodes(pass, keywordCollector, vertexNodes, pixelNodes,
                vertexNodePermutations, pixelNodePermutations, activeFields, out graphRequirements);

            ShaderFoundry.BlockDescriptor BuildSimpleBlockDescriptor(ShaderFoundry.ShaderContainer container, ShaderFoundry.Block block)
            {
                var builder = new ShaderFoundry.BlockDescriptor.Builder(container, block);
                return builder.Build();
            }

            // --------------------------------------------------
            // Graph Vertex

            var vertexCP = passDesc.FindCustomizationPoint(template, LegacyCustomizationPoints.VertexDescriptionCPName);
            var surfaceCP = passDesc.FindCustomizationPoint(template, LegacyCustomizationPoints.SurfaceDescriptionCPName);

            var vertexCPDesc = new ShaderFoundry.CustomizationPointDescriptor.Builder(container, vertexCP);
            var fragmentCPDesc = new ShaderFoundry.CustomizationPointDescriptor.Builder(container, surfaceCP);
            vertexCPDesc.PassIdentifiers.Add(passDesc.PassIdentifier);
            fragmentCPDesc.PassIdentifiers.Add(passDesc.PassIdentifier);

            // If vertex modification enabled
            if (vertexSlots != null)
            {
                // Build the basic block with it's inputs/outputs
                var vertexBlockBuilder = BuildBlockForStage(generator, vertexNodes, vertexSlots, ShaderStageCapability.Vertex, container, vertexEntryPointName, vertexGraphFunctionName, vertexGraphInputName, vertexGraphOutputName);

                // Build vertex graph functions from ShaderPass vertex port mask
                var vertexDescriptionFunction = GenerateVertexDescriptionFunctionSandbox(
                    generator.m_GraphData,
                    container,
                    vertexBlockBuilder,
                    functionRegistry,
                    propertyCollector,
                    keywordCollector,
                    generator.m_Mode,
                    generator.m_OutputNode,
                    vertexNodes,
                    vertexNodePermutations,
                    vertexSlots,
                    vertexGraphInputName,
                    vertexGraphFunctionName,
                    vertexGraphOutputName);

                vertexBlockBuilder.SetEntryPointFunction(vertexDescriptionFunction);
                vertexCPDesc.BlockDescriptors.Add(BuildSimpleBlockDescriptor(container, vertexBlockBuilder.Build()));
            }

            // --------------------------------------------------
            // Graph Pixel

            // Build the basic block with it's inputs/outputs
            var fragmentBlockBuilder = BuildBlockForStage(generator, pixelNodes, pixelSlots, ShaderStageCapability.Fragment, container, pixelEntryPointName, pixelGraphFunctionName, pixelGraphInputName, pixelGraphOutputName);

            // Build pixel graph functions from ShaderPass pixel port mask
            var surfaceDescriptionFunction = GenerateSurfaceDescriptionFunctionSandbox(
                pixelNodes,
                pixelNodePermutations,
                generator.m_OutputNode,
                generator.m_GraphData,
                container,
                fragmentBlockBuilder,
                functionRegistry,
                propertyCollector,
                keywordCollector,
                generator.m_Mode,
                pixelGraphFunctionName,
                pixelGraphOutputName,
                null,
                pixelSlots,
                pixelGraphInputName,
                pass.virtualTextureFeedback);

            fragmentBlockBuilder.SetEntryPointFunction(surfaceDescriptionFunction);
            var sharedIncludeDesc = new IncludeDescriptor.Builder(container, $"\"{sharedPath}\"");
            fragmentBlockBuilder.AddInclude(sharedIncludeDesc.Build());
            fragmentCPDesc.BlockDescriptors.Add(BuildSimpleBlockDescriptor(container, fragmentBlockBuilder.Build()));

            templateDescBuilder.AddCustomizationPointDescriptor(vertexCPDesc.Build());
            templateDescBuilder.AddCustomizationPointDescriptor(fragmentCPDesc.Build());

            string existingShared = null;
            if (File.Exists(sharedPath))
                existingShared = File.ReadAllText(sharedPath);
            var sharedCode = functionBuilder.ToString();
            if(existingShared != sharedCode)
                File.WriteAllText(sharedPath, sharedCode);
            //if(generator.m_assetCollection != null)
            //    generator.m_assetCollection.AddAssetDependency(AssetDatabase.GUIDFromAssetPath(sharedPath), AssetCollection.Flags.IncludeInExportPackage);
        }

        static ShaderFoundry.Block.Builder BuildBlockForStage(ShaderGraph.Generator generator, List<AbstractMaterialNode> nodes, List<MaterialSlot> slots, ShaderStageCapability stageCapability, ShaderFoundry.ShaderContainer container, string blockName, string customizationPointName, string inputsTypeName, string outputsTypeName)
        {
            var blockBuilder = new ShaderFoundry.Block.Builder(container, blockName);

            var inputsStruct = BuildInputsType(generator, nodes, inputsTypeName, stageCapability, container, blockBuilder);
            var outputsStruct = BuildOutputsType(slots, outputsTypeName, container, blockBuilder);

            // Have to collect properties somehow? This doesn't work but I don't care right now...
            PropertyCollector propCollector = new PropertyCollector();
            foreach (var node in nodes)
            {
                node.CollectShaderProperties(propCollector, GenerationMode.ForReals);
            }
            foreach (var prop in propCollector.properties)
            {
                var concretizedTypeName = prop.concreteShaderValueType.ToShaderString();//.ToConcreteShaderValueType();//.type.Replace("$precision", m_GraphData.graphDefaultConcretePrecision.ToShaderString());
                var fieldType = container.GetType(concretizedTypeName);

                var propBuilder = new ShaderFoundry.BlockVariable.Builder(container);
                propBuilder.ReferenceName = prop.referenceName;
                propBuilder.DisplayName = prop.displayName;
                propBuilder.Type = fieldType;
                blockBuilder.AddProperty(propBuilder.Build());
                //block.AddProperty(new BlockProperty { ReferenceName = prop.referenceName, DisplayName = prop.displayName, Type = fieldType });
            }

            blockBuilder.AddType(inputsStruct);
            blockBuilder.AddType(outputsStruct);

            {
                foreach (var field in inputsStruct.StructFields)
                {
                    var varBuilder = new ShaderFoundry.BlockVariable.Builder(container);
                    varBuilder.DisplayName = field.Name;
                    varBuilder.ReferenceName = field.Name;
                    varBuilder.Type = field.Type;
                    blockBuilder.AddInput(varBuilder.Build());
                }
            }
            {
                foreach (var field in outputsStruct.StructFields)
                {
                    var varBuilder = new ShaderFoundry.BlockVariable.Builder(container);
                    varBuilder.DisplayName = field.Name;
                    varBuilder.ReferenceName = field.Name;
                    varBuilder.Type = field.Type;
                    blockBuilder.AddOutput(varBuilder.Build());
                }
            }
            return blockBuilder;
        }

        static ShaderFoundry.ShaderType BuildInputsType(ShaderGraph.Generator generator, List<AbstractMaterialNode> nodes, string typeName, ShaderStageCapability stageCapability, ShaderFoundry.ShaderContainer container, ShaderFoundry.Block.Builder blockBuilder)
        {
            var builder = new ShaderFoundry.ShaderType.StructBuilder(blockBuilder, typeName);
            // Add active fields
            var activeFields = GetActiveFieldsFor(nodes, stageCapability);
            foreach (var field in activeFields)
            {
                var concretizedTypeName = field.type.Replace("$precision", generator.m_GraphData.graphDefaultConcretePrecision.ToShaderString());
                var fieldType = container.GetType(blockBuilder, concretizedTypeName);
                builder.AddField(fieldType, field.name);
            }
            return builder.Build();
        }

        static ShaderFoundry.ShaderType BuildOutputsType(List<MaterialSlot> slots, string typeName, ShaderFoundry.ShaderContainer container, ShaderFoundry.Block.Builder blockBuilder)
        {
            var builder = new ShaderFoundry.ShaderType.StructBuilder(blockBuilder, typeName);
            foreach (var slot in slots)
            {
                string hlslName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                var fieldType = container.GetType(blockBuilder, slot.concreteValueType.ToShaderString(slot.owner.concretePrecision));
                builder.AddField(fieldType, hlslName);
            }
            return builder.Build();
        }

        static private void ExtractNodesNew(ShaderGraph.Generator generator, ShaderFoundry.TemplatePass passDesc, PassDescriptor pass, ActiveFields activeFields, PropertyCollector propertyCollector, out List<AbstractMaterialNode> vertexNodes, out List<AbstractMaterialNode> pixelNodes, out List<MaterialSlot> pixelSlots, out List<MaterialSlot> vertexSlots)
        {
            // Get Port references from ShaderPass
            pixelSlots = new List<MaterialSlot>();
            vertexSlots = new List<MaterialSlot>();
            if (generator.m_OutputNode == null)
            {
                void ProcessStackForPass(ContextData contextData, IEnumerable<ShaderFoundry.BlockDescriptor> blockDescriptors, BlockFieldDescriptor[] passBlockMask,
                    List<AbstractMaterialNode> nodeList, List<MaterialSlot> slotList)
                {
                    if (passBlockMask == null)
                        return;

                    var preBlock = blockDescriptors.ElementAtOrDefault(0).Block;
                    var postBlock = blockDescriptors.ElementAtOrDefault(2).Block;

                    foreach (var blockFieldDescriptor in passBlockMask)
                    {
                        var matchingOutput = preBlock.Outputs.FirstOrDefault(prop => prop.DisplayName == blockFieldDescriptor.name);
                        var matchingInput = postBlock.Inputs.FirstOrDefault(prop => prop.DisplayName == blockFieldDescriptor.name);
                        if (matchingInput == null && matchingOutput == null)
                            continue;
                        // Hack: Removed this to make life easier
                        //if (!m_ActiveBlocks.Contains(blockFieldDescriptor))
                        //    continue;

                        // Attempt to get BlockNode from the stack
                        var block = contextData.blocks.FirstOrDefault(x => x.value.descriptor == blockFieldDescriptor).value;

                        // If the BlockNode doesn't exist in the stack we need to create one
                        // TODO: Can we do the code gen without a node instance?
                        if (block == null)
                        {
                            block = new BlockNode();
                            block.Init(blockFieldDescriptor);
                            block.owner = generator.m_GraphData;
                        }
                        // Don't collect properties from temp nodes
                        else
                        {
                            block.CollectShaderProperties(propertyCollector, generator.m_Mode);
                        }

                        // Add nodes and slots from supported vertex blocks
                        NodeUtils.DepthFirstCollectNodesFromNode(nodeList, block, NodeUtils.IncludeSelf.Include);
                        slotList.Add(block.FindSlot<MaterialSlot>(0));
                        activeFields.baseInstance.Add(block.descriptor);
                    }
                }

                // Mask blocks per pass
                vertexNodes = Pool.ListPool<AbstractMaterialNode>.Get();
                pixelNodes = Pool.ListPool<AbstractMaterialNode>.Get();

                // Process stack for vertex and fragment
                ProcessStackForPass(generator.m_GraphData.vertexContext, passDesc.VertexBlocks, pass.validVertexBlocks, vertexNodes, vertexSlots);
                ProcessStackForPass(generator.m_GraphData.fragmentContext, passDesc.FragmentBlocks, pass.validPixelBlocks, pixelNodes, pixelSlots);
            }
            else if (generator.m_OutputNode is SubGraphOutputNode)
            {
                GenerationUtils.GetUpstreamNodesForShaderPass(generator.m_OutputNode, pass, out vertexNodes, out pixelNodes);
                var slot = generator.m_OutputNode.GetInputSlots<MaterialSlot>().FirstOrDefault();
                if (slot != null)
                    pixelSlots = new List<MaterialSlot>() { slot };
                else
                    pixelSlots = new List<MaterialSlot>();
                vertexSlots = new List<MaterialSlot>();
            }
            else
            {
                GenerationUtils.GetUpstreamNodesForShaderPass(generator.m_OutputNode, pass, out vertexNodes, out pixelNodes);
                pixelSlots = new List<MaterialSlot>()
                {
                    new Vector4MaterialSlot(0, "Out", "Out", SlotType.Output, Vector4.zero) { owner = generator.m_OutputNode },
                };
                vertexSlots = new List<MaterialSlot>();
            }
        }

        internal static ShaderFoundry.ShaderFunction GenerateVertexDescriptionFunctionSandbox(
            GraphData graph,
            ShaderFoundry.ShaderContainer container,
            ShaderFoundry.Block.Builder blockBuilder,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            KeywordCollector shaderKeywords,
            GenerationMode mode,
            AbstractMaterialNode rootNode,
            List<AbstractMaterialNode> nodes,
            List<int>[] keywordPermutationsPerNode,
            List<MaterialSlot> slots,
            string graphInputStructName = "VertexDescriptionInputs",
            string functionName = "PopulateVertexData",
            string graphOutputStructName = "VertexDescription")
        {
            if (graph == null)
                return ShaderFoundry.ShaderFunction.Invalid;


            graph.CollectShaderProperties(shaderProperties, mode);

            var returnType = container.GetType(blockBuilder, graphOutputStructName);
            var inputType = container.GetType(blockBuilder, graphInputStructName);
            var builder = new ShaderFoundry.ShaderFunction.Builder(blockBuilder, functionName, returnType);
            builder.AddInput(inputType, "IN");
            {
                //builder.AddVariableDeclarationStatement(blockBuilder, returnType, "description", "({0})0");
                builder.AddLine(returnType.Name, $" description = ({returnType.Name})0;");
                var nodesBuilder = new ShaderStringBuilder();
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (!nodes[i].isActive)
                        continue;
                    bool active = true;
                    foreach (var slot in nodes[i].GetInputSlots<MaterialSlot>())
                        active &= slot.isConnected;
                    if (!active)
                        continue;

                    GenerationUtils.GenerateDescriptionForNode(nodes[i], keywordPermutationsPerNode[i], functionRegistry, nodesBuilder,
                        shaderProperties, shaderKeywords,
                        graph, mode);
                }
                builder.AddLine(nodesBuilder.ToString());


                functionRegistry.builder.currentNode = null;
                //builder.currentNode = null;

                if (slots.Count != 0)
                {
                    foreach (var slot in slots)
                    {
                        var isSlotConnected = graph.GetEdges(slot.slotReference).Any();
                        var slotName = NodeUtils.ConvertToValidHLSLIdentifier(slot.shaderOutputName);
                        var slotValue = isSlotConnected ?
                            ((AbstractMaterialNode)slot.owner).GetSlotValue(slot.id, mode, slot.owner.concretePrecision) : slot.GetDefaultValue(mode, slot.owner.concretePrecision);
                        builder.AddLine(string.Format("description.{0} = {1};", slotName, slotValue));
                    }
                }

                builder.AddLine("return description;");
            }
            return builder.Build();
        }

        internal static ShaderFoundry.ShaderFunction GenerateSurfaceDescriptionFunctionSandbox(
            List<AbstractMaterialNode> nodes,
            List<int>[] keywordPermutationsPerNode,
            AbstractMaterialNode rootNode,
            GraphData graph,
            ShaderFoundry.ShaderContainer container,
            ShaderFoundry.Block.Builder blockBuilder,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            KeywordCollector shaderKeywords,
            GenerationMode mode,
            string functionName = "PopulateSurfaceData",
            string surfaceDescriptionName = "SurfaceDescription",
            Vector1ShaderProperty outputIdProperty = null,
            IEnumerable<MaterialSlot> slots = null,
            string graphInputStructName = "SurfaceDescriptionInputs",
            bool virtualTextureFeedback = false)
        {
            if (graph == null)
                return ShaderFoundry.ShaderFunction.Invalid;

            graph.CollectShaderProperties(shaderProperties, mode);

            var returnType = container.GetType(blockBuilder, surfaceDescriptionName);
            var inputType = container.GetType(blockBuilder, graphInputStructName);
            var builder = new ShaderFoundry.ShaderFunction.Builder(blockBuilder, functionName, returnType);
            builder.AddInput(inputType, "IN");
            //surfaceDescriptionFunction.AppendLine(String.Format("{0} {1}(SurfaceDescriptionInputs IN)", surfaceDescriptionName, functionName), false);
            //using (surfaceDescriptionFunction.BlockScope())
            {
                //returnType.AddVariableDeclarationStatement(builder, "surface", "({0})0");
                builder.AddLine(returnType.Name, $" surface = ({returnType.Name})0;");
                var nodesBuilder = new ShaderStringBuilder();
                for (int i = 0; i < nodes.Count; i++)
                {
                    //if (!nodes[i].isActive)
                    //    continue;
                    //bool active = true;
                    //foreach (var slot in nodes[i].GetInputSlots<MaterialSlot>())
                    //    active &= slot.isConnected;
                    //if (!active)
                    //    continue;

                    GenerationUtils.GenerateDescriptionForNode(nodes[i], keywordPermutationsPerNode[i], functionRegistry, nodesBuilder,
                        shaderProperties, shaderKeywords,
                        graph, mode);
                }
                builder.AddLine(nodesBuilder.ToString());

                functionRegistry.builder.currentNode = null;
                //surfaceDescriptionFunction.currentNode = null;

                GenerateSurfaceDescriptionRemap(graph, rootNode, slots, builder, mode);

                if (virtualTextureFeedback)
                {
                    var vtBuilder = new ShaderStringBuilder();
                    VirtualTexturingFeedbackUtils.GenerateVirtualTextureFeedback(
                        nodes,
                        keywordPermutationsPerNode,
                        vtBuilder,
                        shaderKeywords);
                    builder.AddLine(vtBuilder.ToString());
                }

                builder.AddLine("return surface;");
            }
            return builder.Build();
        }

        static void GenerateSurfaceDescriptionRemap(
            GraphData graph,
            AbstractMaterialNode rootNode,
            IEnumerable<MaterialSlot> slots,
            ShaderFoundry.ShaderFunction.Builder builder,
            GenerationMode mode)
        {
            if (rootNode == null)
            {
                foreach (var input in slots)
                {
                    if (input != null)
                    {
                        var node = input.owner;
                        var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                        var hlslName = NodeUtils.GetHLSLSafeName(input.shaderOutputName);
                        if (foundEdges.Any())
                            builder.AddLine($"surface.{hlslName} = {node.GetSlotValue(input.id, mode, node.concretePrecision)};");
                        else
                            builder.AddLine($"surface.{hlslName} = {input.GetDefaultValue(mode, node.concretePrecision)};");
                    }
                }
            }
            else if (rootNode is SubGraphOutputNode)
            {
                var slot = slots.FirstOrDefault();
                if (slot != null)
                {
                    var foundEdges = graph.GetEdges(slot.slotReference).ToArray();
                    var hlslName = $"{NodeUtils.GetHLSLSafeName(slot.shaderOutputName)}_{slot.id}";
                    if (foundEdges.Any())
                        builder.AddLine($"surface.{hlslName} = {rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision)};");
                    else
                        builder.AddLine($"surface.{hlslName} = {slot.GetDefaultValue(mode, rootNode.concretePrecision)};");
                    builder.AddLine($"surface.Out = all(isfinite(surface.{hlslName})) ? {GenerationUtils.AdaptNodeOutputForPreview(rootNode, slot.id, "surface." + hlslName)} : float4(1.0f, 0.0f, 1.0f, 1.0f);");
                }
            }
            else
            {
                var slot = rootNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                if (slot != null)
                {
                    string slotValue;
                    string previewOutput;
                    if (rootNode.isActive)
                    {
                        slotValue = rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision);
                        previewOutput = GenerationUtils.AdaptNodeOutputForPreview(rootNode, slot.id);
                    }
                    else
                    {
                        slotValue = rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision);
                        previewOutput = "float4(0.0f, 0.0f, 0.0f, 0.0f)";
                    }
                    builder.AddLine($"surface.Out = all(isfinite({slotValue})) ? {previewOutput} : float4(1.0f, 0.0f, 1.0f, 1.0f);");
                }
            }
        }

        internal static List<FieldDescriptor> GetActiveFieldsFor(List<AbstractMaterialNode> nodes, ShaderStageCapability stageCapability)
        {
            ShaderGraphRequirementsPerKeyword requirements = new ShaderGraphRequirementsPerKeyword();
            requirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(nodes, stageCapability, false));

            // Add active fields
            ConditionalField[] conditionalFields;
            if (stageCapability == ShaderStageCapability.Vertex)
                conditionalFields = GenerationUtils.GetConditionalFieldsFromVertexRequirements(requirements.baseInstance.requirements);
            else
                conditionalFields = GenerationUtils.GetConditionalFieldsFromPixelRequirements(requirements.baseInstance.requirements);
            return GenerationUtils.GetActiveFieldsFromConditionals(conditionalFields);
        }
    }


    internal static class Extensions
    {
        public static ShaderFoundry.ShaderType GetType(this ShaderFoundry.ShaderContainer container, ShaderFoundry.Block.Builder blockBuilder, string typeName)
        {
            // First search in the block's context, if not found search in the global context
            var type = container.GetType(typeName, blockBuilder);
            if (!type.IsValid)
                type = container.GetType(typeName);
            return type;
        }
    }
}
