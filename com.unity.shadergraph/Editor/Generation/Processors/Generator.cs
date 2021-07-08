using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using Pool = UnityEngine.Pool;

namespace UnityEditor.ShaderGraph
{
    class Generator
    {
        const string kDebugSymbol = "SHADERGRAPH_DEBUG";

        GraphData m_GraphData;
        AbstractMaterialNode m_OutputNode;
        Target[] m_Targets;
        List<BlockNode> m_ActiveBlocks;
        List<BlockNode> m_TemporaryBlocks;
        GenerationMode m_Mode;
        string m_Name;

        ShaderStringBuilder m_Builder;
        List<PropertyCollector.TextureInfo> m_ConfiguredTextures;
        AssetCollection m_assetCollection;

        public string generatedShader => m_Builder.ToCodeBlock();
        public List<PropertyCollector.TextureInfo> configuredTextures => m_ConfiguredTextures;
        public List<BlockNode> temporaryBlocks => m_TemporaryBlocks;

        public Generator(GraphData graphData, AbstractMaterialNode outputNode, GenerationMode mode, string name, AssetCollection assetCollection)
        {
            m_GraphData  = graphData;
            m_OutputNode = outputNode;
            Generate(mode, name, assetCollection, GetTargetImplementations());
        }

        public Generator(GraphData graphData, AbstractMaterialNode outputNode, GenerationMode mode, string name, AssetCollection assetCollection, Target[] targets)
        {
            m_GraphData  = graphData;
            m_OutputNode = outputNode;
            Generate(mode, name, assetCollection, targets);
        }

        void Generate(GenerationMode mode, string name, AssetCollection assetCollection, Target[] targets)
        {
            m_Mode = mode;
            m_Name = name;

            m_Builder = new ShaderStringBuilder();
            m_ConfiguredTextures = new List<PropertyCollector.TextureInfo>();
            m_assetCollection = assetCollection;

            m_ActiveBlocks = m_GraphData.GetNodes<BlockNode>().ToList();
            m_TemporaryBlocks = new List<BlockNode>();
            m_Targets = targets;
            BuildShader();
        }

        Target[] GetTargetImplementations()
        {
            if (m_OutputNode == null)
            {
                var targets = m_GraphData.activeTargets.ToList();
                // Sort the built-in target to be last. This is currently a requirement otherwise it'll get picked up for other passes incorrectly
                targets.Sort(delegate(Target target0, Target target1)
                {
                    var result = target0.displayName.CompareTo(target1.displayName);
                    // If only one value is built-in, then sort it last
                    if (result != 0)
                    {
                        if (target0.displayName == "Built-In")
                            result = 1;
                        if (target1.displayName == "Built-In")
                            result = -1;
                    }

                    return result;
                });
                return targets.ToArray();
            }
            else
            {
                return new Target[] { new PreviewTarget() };
            }
        }

        public ActiveFields GatherActiveFieldsFromNode(AbstractMaterialNode outputNode, PassDescriptor pass, List<(BlockFieldDescriptor descriptor, bool isDefaultValue)> activeBlocks, List<BlockFieldDescriptor> connectedBlocks, Target target)
        {
            var activeFields = new ActiveFields();
            if (outputNode == null)
            {
                bool hasDotsProperties = false;
                m_GraphData.ForeachHLSLProperty(h =>
                {
                    if (h.declaration == HLSLDeclaration.HybridPerInstance)
                        hasDotsProperties = true;
                });

                var context = new TargetFieldContext(pass, activeBlocks, connectedBlocks, hasDotsProperties);
                target.GetFields(ref context);
                var fields = GenerationUtils.GetActiveFieldsFromConditionals(context.conditionalFields.ToArray());
                foreach (FieldDescriptor field in fields)
                    activeFields.baseInstance.Add(field);
            }
            // Preview shader
            else
            {
                activeFields.baseInstance.Add(Fields.GraphPixel);
            }
            return activeFields;
        }

        void BuildShader()
        {
            var activeNodeList = Pool.ListPool<AbstractMaterialNode>.Get();
            bool ignoreActiveState = (m_Mode == GenerationMode.Preview);  // for previews, we ignore node active state
            if (m_OutputNode == null)
            {
                foreach (var block in m_ActiveBlocks)
                {
                    // IsActive is equal to if any active implementation has set active blocks
                    // This avoids another call to SetActiveBlocks on each TargetImplementation
                    if (!block.isActive)
                        continue;

                    NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, block, NodeUtils.IncludeSelf.Include, ignoreActiveState: ignoreActiveState);
                }
            }
            else
            {
                NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, m_OutputNode, ignoreActiveState: ignoreActiveState);
            }

            var shaderProperties = new PropertyCollector();
            var shaderKeywords = new KeywordCollector();
            m_GraphData.CollectShaderProperties(shaderProperties, m_Mode);
            m_GraphData.CollectShaderKeywords(shaderKeywords, m_Mode);

            var graphInputOrderData = new List<GraphInputData>();
            foreach (var cat in m_GraphData.categories)
            {
                foreach (var input in cat.Children)
                {
                    graphInputOrderData.Add(new GraphInputData()
                    {
                        isKeyword = input is ShaderKeyword,
                        referenceName = input.referenceName
                    });
                }
            }
            string path = AssetDatabase.GUIDToAssetPath(m_GraphData.assetGuid);
            if (shaderKeywords.permutations.Count > ShaderGraphPreferences.variantLimit)
            {
                string graphName = "";
                if (m_GraphData.owner != null)
                {
                    if (path != null)
                    {
                        graphName = Path.GetFileNameWithoutExtension(path);
                    }
                }
                Debug.LogError($"Error in Shader Graph {graphName}:{ShaderKeyword.kVariantLimitWarning}");

                m_ConfiguredTextures = shaderProperties.GetConfiguredTextures();
                m_Builder.AppendLines(ShaderGraphImporter.k_ErrorShader);
            }

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                activeNode.CollectShaderProperties(shaderProperties, m_Mode);

            // Collect excess shader properties from the TargetImplementation
            foreach (var target in m_Targets)
            {
                // TODO: Setup is required to ensure all Targets are initialized
                // TODO: Find a way to only require this once
                TargetSetupContext context = new TargetSetupContext();
                target.Setup(ref context);

                target.CollectShaderProperties(shaderProperties, m_Mode);
            }

            // set the property collector to read only
            // (to ensure no rogue target or pass starts adding more properties later..)
            shaderProperties.SetReadOnly();

            m_Builder.AppendLine(@"Shader ""{0}""", m_Name);
            using (m_Builder.BlockScope())
            {
                GenerationUtils.GeneratePropertiesBlock(m_Builder, shaderProperties, shaderKeywords, m_Mode, graphInputOrderData);

                for (int i = 0; i < m_Targets.Length; i++)
                {
                    TargetSetupContext context = new TargetSetupContext(m_assetCollection);

                    // Instead of setup target, we can also just do get context
                    m_Targets[i].Setup(ref context);

                    var subShaderProperties = GetSubShaderPropertiesForTarget(m_Targets[i], m_GraphData, m_Mode, m_OutputNode, m_TemporaryBlocks);
                    foreach (var subShader in context.subShaders)
                    {
                        GenerateSubShader(i, subShader, subShaderProperties);
                    }

                    var customEditor = context.defaultShaderGUI;
                    if (customEditor != null && m_Targets[i].WorksWithSRP(GraphicsSettings.currentRenderPipeline))
                    {
                        m_Builder.AppendLine("CustomEditor \"" + customEditor + "\"");
                    }

                    foreach (var rpCustomEditor in context.customEditorForRenderPipelines)
                    {
                        m_Builder.AppendLine($"CustomEditorForRenderPipeline \"{rpCustomEditor.shaderGUI}\" \"{rpCustomEditor.renderPipelineAssetType}\"");
                    }

                    m_Builder.AppendLine("CustomEditor \"" + typeof(GenericShaderGraphMaterialGUI).FullName + "\"");
                }

                m_Builder.AppendLine(@"FallBack ""Hidden/Shader Graph/FallbackError""");
            }

            m_ConfiguredTextures = shaderProperties.GetConfiguredTextures();
        }

        void GenerateSubShader(int targetIndex, SubShaderDescriptor descriptor, PropertyCollector subShaderProperties)
        {
            if (descriptor.passes == null)
                return;

            // Early out of preview generation if no passes are used in preview
            if (m_Mode == GenerationMode.Preview && descriptor.generatesPreview == false)
                return;

            m_Builder.AppendLine("SubShader");
            using (m_Builder.BlockScope())
            {
                GenerationUtils.GenerateSubShaderTags(m_Targets[targetIndex], descriptor, m_Builder);

                // Get block descriptor list here (from ALL active blocks)
                List<(BlockFieldDescriptor descriptor, bool isDefaultValue)> activeBlockDescriptors = m_ActiveBlocks.Select(x => (x.descriptor, x.GetInputSlots<MaterialSlot>().FirstOrDefault().IsUsingDefaultValue())).ToList();
                var connectedBlockDescriptors = m_ActiveBlocks.Where(x => x.IsSlotConnected(0)).Select(x => x.descriptor).ToList();

                foreach (PassCollection.Item pass in descriptor.passes)
                {
                    var activeFields = GatherActiveFieldsFromNode(m_OutputNode, pass.descriptor, activeBlockDescriptors, connectedBlockDescriptors, m_Targets[targetIndex]);

                    // TODO: cleanup this preview check, needed for HD decal preview pass
                    if (m_Mode == GenerationMode.Preview)
                        activeFields.baseInstance.Add(Fields.IsPreview);

                    // Check masternode fields for valid passes
                    if (pass.TestActive(activeFields))
                        GenerateShaderPass(targetIndex, pass.descriptor, activeFields, activeBlockDescriptors.Select(x => x.descriptor).ToList(), subShaderProperties);
                }
            }
        }

        // this builds the list of properties for a Target / Graph combination
        static PropertyCollector GetSubShaderPropertiesForTarget(Target target, GraphData graph, GenerationMode generationMode, AbstractMaterialNode outputNode, List<BlockNode> outTemporaryBlockNodes)
        {
            PropertyCollector subshaderProperties = new PropertyCollector();

            // Collect shader properties declared by active nodes
            using (var activeNodes = PooledHashSet<AbstractMaterialNode>.Get())
            {
                if (outputNode == null)
                {
                    // shader graph builds active nodes starting from the set of active blocks
                    var currentBlocks = graph.GetNodes<BlockNode>();
                    var activeBlockContext = new TargetActiveBlockContext(currentBlocks.Select(x => x.descriptor).ToList(), null);
                    target.GetActiveBlocks(ref activeBlockContext);

                    foreach (var blockFieldDesc in activeBlockContext.activeBlocks)
                    {
                        bool foundBlock = false;

                        // attempt to get BlockNode(s) from the stack
                        var vertBlockNode = graph.vertexContext.blocks.FirstOrDefault(x => x.value.descriptor == blockFieldDesc).value;
                        if (vertBlockNode != null)
                        {
                            activeNodes.Add(vertBlockNode);
                            foundBlock = true;
                        }

                        var fragBlockNode = graph.fragmentContext.blocks.FirstOrDefault(x => x.value.descriptor == blockFieldDesc).value;
                        if (fragBlockNode != null)
                        {
                            activeNodes.Add(fragBlockNode);
                            foundBlock = true;
                        }

                        if (!foundBlock)
                        {
                            // block doesn't exist (user deleted it)
                            // create a temporary block -- don't add to graph, but use it to gather properties
                            var block = new BlockNode();
                            block.Init(blockFieldDesc);
                            block.owner = graph;
                            activeNodes.Add(block);

                            // We need to make a list of all of the temporary blocks added
                            // (This is used by the PreviewManager to generate a PreviewProperty)
                            outTemporaryBlockNodes.Add(block);
                        }
                    }
                }
                else
                {
                    // preview and/or subgraphs build their active node set based on the single output node
                    activeNodes.Add(outputNode);
                }

                PreviewManager.PropagateNodes(activeNodes, PreviewManager.PropagationDirection.Upstream, activeNodes);

                // NOTE: this is NOT a deterministic ordering
                foreach (var node in activeNodes)
                    node.CollectShaderProperties(subshaderProperties, generationMode);

                // So we sort the properties after
                subshaderProperties.Sort();
            }

            // Collect graph properties
            {
                graph.CollectShaderProperties(subshaderProperties, generationMode);
            }

            // Collect shader properties declared by the Target
            {
                target.CollectShaderProperties(subshaderProperties, generationMode);
            }

            subshaderProperties.SetReadOnly();

            return subshaderProperties;
        }

        void GenerateShaderPass(int targetIndex, PassDescriptor pass, ActiveFields activeFields, List<BlockFieldDescriptor> currentBlockDescriptors, PropertyCollector subShaderProperties)
        {
            // Early exit if pass is not used in preview
            if (m_Mode == GenerationMode.Preview && !pass.useInPreview)
                return;

            // --------------------------------------------------
            // Debug

            // Get scripting symbols
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            bool isDebug = defines.Contains(kDebugSymbol);

            // --------------------------------------------------
            // Setup

            // Custom Interpolator Global flags (see definition for details).
            CustomInterpolatorUtils.generatorNodeOnly = m_OutputNode != null;
            CustomInterpolatorUtils.generatorSkipFlag = m_Targets[targetIndex].ignoreCustomInterpolators ||
                !CustomInterpolatorUtils.generatorNodeOnly && (pass.customInterpolators == null || pass.customInterpolators.Count() == 0);

            // Initialize custom interpolator sub generator
            CustomInterpSubGen customInterpSubGen = new CustomInterpSubGen(m_OutputNode != null);

            // Initiailize Collectors
            // NOTE: propertyCollector is not really used anymore -- we use the subshader PropertyCollector instead
            var propertyCollector = new PropertyCollector();
            var keywordCollector = new KeywordCollector();
            m_GraphData.CollectShaderKeywords(keywordCollector, m_Mode);

            // Get upstream nodes from ShaderPass port mask
            List<AbstractMaterialNode> vertexNodes;
            List<AbstractMaterialNode> pixelNodes;

            // Get Port references from ShaderPass
            var pixelSlots = new List<MaterialSlot>();
            var vertexSlots = new List<MaterialSlot>();

            if (m_OutputNode == null)
            {
                // Update supported block list for current target implementation
                var activeBlockContext = new TargetActiveBlockContext(currentBlockDescriptors, pass);
                m_Targets[targetIndex].GetActiveBlocks(ref activeBlockContext);

                void ProcessStackForPass(ContextData contextData, BlockFieldDescriptor[] passBlockMask,
                    List<AbstractMaterialNode> nodeList, List<MaterialSlot> slotList)
                {
                    if (passBlockMask == null)
                        return;

                    foreach (var blockFieldDescriptor in passBlockMask)
                    {
                        // Mask blocks on active state
                        // TODO: Can we merge these?
                        if (!activeBlockContext.activeBlocks.Contains(blockFieldDescriptor))
                            continue;

                        // Attempt to get BlockNode from the stack
                        var block = contextData.blocks.FirstOrDefault(x => x.value.descriptor == blockFieldDescriptor).value;

                        // If the BlockNode doesnt exist in the stack we need to create one
                        // TODO: Can we do the code gen without a node instance?
                        if (block == null)
                        {
                            block = new BlockNode();
                            block.Init(blockFieldDescriptor);
                            block.owner = m_GraphData;
                        }
                        // Dont collect properties from temp nodes
                        else
                        {
                            block.CollectShaderProperties(propertyCollector, m_Mode);
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
                ProcessStackForPass(m_GraphData.vertexContext, pass.validVertexBlocks, vertexNodes, vertexSlots);
                ProcessStackForPass(m_GraphData.fragmentContext, pass.validPixelBlocks, pixelNodes, pixelSlots);

                // Collect excess shader properties from the TargetImplementation
                m_Targets[targetIndex].CollectShaderProperties(propertyCollector, m_Mode);
            }
            else if (m_OutputNode is SubGraphOutputNode)
            {
                GenerationUtils.GetUpstreamNodesForShaderPass(m_OutputNode, pass, out vertexNodes, out pixelNodes);
                var slot = m_OutputNode.GetInputSlots<MaterialSlot>().FirstOrDefault();
                if (slot != null)
                    pixelSlots = new List<MaterialSlot>() { slot };
                else
                    pixelSlots = new List<MaterialSlot>();
                vertexSlots = new List<MaterialSlot>();
            }
            else
            {
                GenerationUtils.GetUpstreamNodesForShaderPass(m_OutputNode, pass, out vertexNodes, out pixelNodes);
                pixelSlots = new List<MaterialSlot>()
                {
                    new Vector4MaterialSlot(0, "Out", "Out", SlotType.Output, Vector4.zero) { owner = m_OutputNode },
                };
                vertexSlots = new List<MaterialSlot>();
            }

            // Inject custom interpolator antecedents where appropriate
            customInterpSubGen.ProcessExistingStackData(vertexNodes, vertexSlots, pixelNodes, activeFields.baseInstance);

            // Track permutation indices for all nodes
            List<int>[] vertexNodePermutations = new List<int>[vertexNodes.Count];
            List<int>[] pixelNodePermutations = new List<int>[pixelNodes.Count];

            // Get active fields from upstream Node requirements
            ShaderGraphRequirementsPerKeyword graphRequirements;
            GenerationUtils.GetActiveFieldsAndPermutationsForNodes(pass, keywordCollector, vertexNodes, pixelNodes,
                vertexNodePermutations, pixelNodePermutations, activeFields, out graphRequirements);

            // Moved this up so that we can reuse the information to figure out which struct Descriptors
            // should be populated by custom interpolators.
            var passStructs = new List<StructDescriptor>();
            passStructs.AddRange(pass.structs.Select(x => x.descriptor));

            // GET CUSTOM ACTIVE FIELDS HERE!

            // inject custom interpolator fields into the pass structs
            passStructs = customInterpSubGen.CopyModifyExistingPassStructs(passStructs, activeFields.baseInstance);

            // Get active fields from ShaderPass
            GenerationUtils.AddRequiredFields(pass.requiredFields, activeFields.baseInstance);

            // Function Registry
            var functionBuilder = new ShaderStringBuilder();
            var graphIncludes = new IncludeCollection();
            var functionRegistry = new FunctionRegistry(functionBuilder, graphIncludes, true);

            // Hash table of named $splice(name) commands
            // Key: splice token
            // Value: string to splice
            Dictionary<string, string> spliceCommands = new Dictionary<string, string>();

            // populate splice commands from the pass's customInterpolator descriptors.
            if (pass.customInterpolators != null)
                customInterpSubGen.ProcessDescriptors(pass.customInterpolators.Select(item => item.descriptor));
            customInterpSubGen.AppendToSpliceCommands(spliceCommands);

            // --------------------------------------------------
            // Dependencies

            // Propagate active field requirements using dependencies
            // Must be executed before types are built
            foreach (var instance in activeFields.all.instances)
            {
                GenerationUtils.ApplyFieldDependencies(instance, pass.fieldDependencies);
            }

            // --------------------------------------------------
            // Pass Setup

            // Name
            if (!string.IsNullOrEmpty(pass.displayName))
            {
                spliceCommands.Add("PassName", $"Name \"{pass.displayName}\"");
            }
            else
            {
                spliceCommands.Add("PassName", "// Name: <None>");
            }

            // Tags
            if (!string.IsNullOrEmpty(pass.lightMode))
            {
                spliceCommands.Add("LightMode", $"\"LightMode\" = \"{pass.lightMode}\"");
            }
            else
            {
                spliceCommands.Add("LightMode", "// LightMode: <None>");
            }

            // --------------------------------------------------
            // Pass Code

            // Render State
            using (var renderStateBuilder = new ShaderStringBuilder())
            {
                // Render states need to be separated by RenderState.Type
                // The first passing ConditionalRenderState of each type is inserted
                foreach (RenderStateType type in Enum.GetValues(typeof(RenderStateType)))
                {
                    var renderStates = pass.renderStates?.Where(x => x.descriptor.type == type);
                    if (renderStates != null)
                    {
                        foreach (RenderStateCollection.Item renderState in renderStates)
                        {
                            if (renderState.TestActive(activeFields))
                            {
                                renderStateBuilder.AppendLine(renderState.value);

                                // Cull is the only render state type that causes a compilation error
                                // when there are multiple Cull directive with different values in a pass.
                                if (type == RenderStateType.Cull)
                                    break;
                            }
                        }
                    }
                }

                string command = GenerationUtils.GetSpliceCommand(renderStateBuilder.ToCodeBlock(), "RenderState");
                spliceCommands.Add("RenderState", command);
            }

            // Pragmas
            using (var passPragmaBuilder = new ShaderStringBuilder())
            {
                if (pass.pragmas != null)
                {
                    foreach (PragmaCollection.Item pragma in pass.pragmas)
                    {
                        if (pragma.TestActive(activeFields))
                            passPragmaBuilder.AppendLine(pragma.value);
                    }
                }

                // Enable this to turn on shader debugging
                bool debugShader = false;
                if (debugShader)
                {
                    passPragmaBuilder.AppendLine("#pragma enable_d3d11_debug_symbols");
                }

                string command = GenerationUtils.GetSpliceCommand(passPragmaBuilder.ToCodeBlock(), "PassPragmas");
                spliceCommands.Add("PassPragmas", command);
            }

            // Keywords
            using (var passKeywordBuilder = new ShaderStringBuilder())
            {
                if (pass.keywords != null)
                {
                    List<KeywordShaderStage> stages = new List<KeywordShaderStage>();
                    foreach (KeywordCollection.Item keyword in pass.keywords)
                    {
                        if (keyword.TestActive(activeFields))
                        {
                            keyword.descriptor.AppendKeywordDeclarationStrings(passKeywordBuilder);
                        }
                    }
                }

                string command = GenerationUtils.GetSpliceCommand(passKeywordBuilder.ToCodeBlock(), "PassKeywords");
                spliceCommands.Add("PassKeywords", command);
            }

            // -----------------------------
            // Generated structs and Packing code
            var interpolatorBuilder = new ShaderStringBuilder();

            if (passStructs != null)
            {
                var packedStructs = new List<StructDescriptor>();
                foreach (var shaderStruct in passStructs)
                {
                    if (shaderStruct.packFields == false)
                        continue; //skip structs that do not need interpolator packs

                    List<int> packedCounts = new List<int>();
                    var packStruct = new StructDescriptor();

                    //generate packed functions
                    if (activeFields.permutationCount > 0)
                    {
                        var generatedPackedTypes = new Dictionary<string, (ShaderStringBuilder, List<int>)>();
                        foreach (var instance in activeFields.allPermutations.instances)
                        {
                            var instanceGenerator = new ShaderStringBuilder();
                            GenerationUtils.GenerateInterpolatorFunctions(shaderStruct, instance, out instanceGenerator);
                            var key = instanceGenerator.ToCodeBlock();
                            if (generatedPackedTypes.TryGetValue(key, out var value))
                                value.Item2.Add(instance.permutationIndex);
                            else
                                generatedPackedTypes.Add(key, (instanceGenerator, new List<int> { instance.permutationIndex }));
                        }

                        var isFirst = true;
                        foreach (var generated in generatedPackedTypes)
                        {
                            if (isFirst)
                            {
                                isFirst = false;
                                interpolatorBuilder.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(generated.Value.Item2));
                            }
                            else
                                interpolatorBuilder.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(generated.Value.Item2).Replace("#if", "#elif"));

                            //interpolatorBuilder.Concat(generated.Value.Item1);
                            interpolatorBuilder.AppendLines(generated.Value.Item1.ToString());
                        }
                        if (generatedPackedTypes.Count > 0)
                            interpolatorBuilder.AppendLine("#endif");
                    }
                    else
                    {
                        ShaderStringBuilder localInterpolatorBuilder; // GenerateInterpolatorFunctions do the allocation
                        GenerationUtils.GenerateInterpolatorFunctions(shaderStruct, activeFields.baseInstance, out localInterpolatorBuilder);
                        interpolatorBuilder.Concat(localInterpolatorBuilder);
                    }
                    //using interp index from functions, generate packed struct descriptor
                    GenerationUtils.GeneratePackedStruct(shaderStruct, activeFields, out packStruct);
                    packedStructs.Add(packStruct);
                }
                passStructs.AddRange(packedStructs);
            }
            if (interpolatorBuilder.length != 0) //hard code interpolators to float, TODO: proper handle precision
                interpolatorBuilder.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Single.ToShaderString());
            else
                interpolatorBuilder.AppendLine("//Interpolator Packs: <None>");
            spliceCommands.Add("InterpolatorPack", interpolatorBuilder.ToCodeBlock());

            // Generated String Builders for all struct types
            var passStructBuilder = new ShaderStringBuilder();
            if (passStructs != null)
            {
                var structBuilder = new ShaderStringBuilder();
                foreach (StructDescriptor shaderStruct in passStructs)
                {
                    GenerationUtils.GenerateShaderStruct(shaderStruct, activeFields, out structBuilder);
                    structBuilder.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Single.ToShaderString()); //hard code structs to float, TODO: proper handle precision
                    passStructBuilder.Concat(structBuilder);
                }
            }
            if (passStructBuilder.length == 0)
                passStructBuilder.AppendLine("//Pass Structs: <None>");
            spliceCommands.Add("PassStructs", passStructBuilder.ToCodeBlock());

            // --------------------------------------------------
            // Graph Vertex

            var vertexBuilder = new ShaderStringBuilder();

            // If vertex modification enabled
            if (activeFields.baseInstance.Contains(Fields.GraphVertex) && vertexSlots != null)
            {
                // Setup
                string vertexGraphInputName = "VertexDescriptionInputs";
                string vertexGraphOutputName = "VertexDescription";
                string vertexGraphFunctionName = "VertexDescriptionFunction";
                var vertexGraphFunctionBuilder = new ShaderStringBuilder();
                var vertexGraphOutputBuilder = new ShaderStringBuilder();

                // Build vertex graph outputs
                // Add struct fields to active fields
                GenerationUtils.GenerateVertexDescriptionStruct(vertexGraphOutputBuilder, vertexSlots, vertexGraphOutputName, activeFields.baseInstance);

                // Build vertex graph functions from ShaderPass vertex port mask
                GenerationUtils.GenerateVertexDescriptionFunction(
                    m_GraphData,
                    vertexGraphFunctionBuilder,
                    functionRegistry,
                    propertyCollector,
                    keywordCollector,
                    m_Mode,
                    m_OutputNode,
                    vertexNodes,
                    vertexNodePermutations,
                    vertexSlots,
                    vertexGraphInputName,
                    vertexGraphFunctionName,
                    vertexGraphOutputName);

                // Generate final shader strings
                vertexBuilder.AppendLines(vertexGraphOutputBuilder.ToString());
                vertexBuilder.AppendNewLine();
                vertexBuilder.AppendLines(vertexGraphFunctionBuilder.ToString());
            }

            // Add to splice commands
            if (vertexBuilder.length == 0)
                vertexBuilder.AppendLine("// GraphVertex: <None>");
            spliceCommands.Add("GraphVertex", vertexBuilder.ToCodeBlock());

            // --------------------------------------------------
            // Graph Pixel

            // Setup
            string pixelGraphInputName = "SurfaceDescriptionInputs";
            string pixelGraphOutputName = "SurfaceDescription";
            string pixelGraphFunctionName = "SurfaceDescriptionFunction";
            var pixelGraphOutputBuilder = new ShaderStringBuilder();
            var pixelGraphFunctionBuilder = new ShaderStringBuilder();

            // Build pixel graph outputs
            // Add struct fields to active fields
            GenerationUtils.GenerateSurfaceDescriptionStruct(pixelGraphOutputBuilder, pixelSlots, pixelGraphOutputName, activeFields.baseInstance, m_OutputNode is SubGraphOutputNode, pass.virtualTextureFeedback);

            // Build pixel graph functions from ShaderPass pixel port mask
            GenerationUtils.GenerateSurfaceDescriptionFunction(
                pixelNodes,
                pixelNodePermutations,
                m_OutputNode,
                m_GraphData,
                pixelGraphFunctionBuilder,
                functionRegistry,
                propertyCollector,
                keywordCollector,
                m_Mode,
                pixelGraphFunctionName,
                pixelGraphOutputName,
                null,
                pixelSlots,
                pixelGraphInputName,
                pass.virtualTextureFeedback);

            using (var pixelBuilder = new ShaderStringBuilder())
            {
                // Generate final shader strings
                pixelBuilder.AppendLines(pixelGraphOutputBuilder.ToString());
                pixelBuilder.AppendNewLine();
                pixelBuilder.AppendLines(pixelGraphFunctionBuilder.ToString());

                // Add to splice commands
                if (pixelBuilder.length == 0)
                    pixelBuilder.AppendLine("// GraphPixel: <None>");
                spliceCommands.Add("GraphPixel", pixelBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Graph Functions

            if (functionBuilder.length == 0)
                functionBuilder.AppendLine("// GraphFunctions: <None>");
            spliceCommands.Add("GraphFunctions", functionBuilder.ToCodeBlock());

            // --------------------------------------------------
            // Graph Keywords

            using (var keywordBuilder = new ShaderStringBuilder())
            {
                keywordCollector.GetKeywordsDeclaration(keywordBuilder, m_Mode);
                if (keywordBuilder.length == 0)
                    keywordBuilder.AppendLine("// GraphKeywords: <None>");
                spliceCommands.Add("GraphKeywords", keywordBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Graph Properties

            using (var propertyBuilder = new ShaderStringBuilder())
            {
                subShaderProperties.GetPropertiesDeclaration(propertyBuilder, m_Mode, m_GraphData.graphDefaultConcretePrecision);

                if (m_Mode == GenerationMode.VFX)
                {
                    const string k_GraphPropertiesStruct = "GraphProperties";
                    propertyBuilder.AppendLine($"struct {k_GraphPropertiesStruct}");
                    using (propertyBuilder.BlockSemicolonScope())
                    {
                        m_GraphData.ForeachHLSLProperty(h =>
                        {
                            if (!h.IsObjectType())
                                h.AppendTo(propertyBuilder);
                        });
                    }
                }

                if (propertyBuilder.length == 0)
                    propertyBuilder.AppendLine("// GraphProperties: <None>");
                spliceCommands.Add("GraphProperties", propertyBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Dots Instanced Graph Properties

            bool hasDotsProperties = subShaderProperties.HasDotsProperties();

            using (var dotsInstancedPropertyBuilder = new ShaderStringBuilder())
            {
                if (hasDotsProperties)
                    dotsInstancedPropertyBuilder.AppendLines(subShaderProperties.GetDotsInstancingPropertiesDeclaration(m_Mode));
                else
                    dotsInstancedPropertyBuilder.AppendLine("// HybridV1InjectedBuiltinProperties: <None>");
                spliceCommands.Add("HybridV1InjectedBuiltinProperties", dotsInstancedPropertyBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Dots Instancing Options

            using (var dotsInstancingOptionsBuilder = new ShaderStringBuilder())
            {
                // Hybrid Renderer V1 requires some magic defines to work, which we enable
                // if the shader graph has a nonzero amount of DOTS instanced properties.
                // This can be removed once Hybrid V1 is removed.
                #if !ENABLE_HYBRID_RENDERER_V2
                if (hasDotsProperties)
                {
                    dotsInstancingOptionsBuilder.AppendLine("#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE)  || defined(SHADER_API_GAMECORE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL))");
                    dotsInstancingOptionsBuilder.AppendLine("    #define UNITY_SUPPORT_INSTANCING");
                    dotsInstancingOptionsBuilder.AppendLine("#endif");
                    dotsInstancingOptionsBuilder.AppendLine("#if defined(UNITY_SUPPORT_INSTANCING) && defined(INSTANCING_ON)");
                    dotsInstancingOptionsBuilder.AppendLine("    #define UNITY_HYBRID_V1_INSTANCING_ENABLED");
                    dotsInstancingOptionsBuilder.AppendLine("#endif");
                }
                #endif

                if (dotsInstancingOptionsBuilder.length == 0)
                    dotsInstancingOptionsBuilder.AppendLine("// DotsInstancingOptions: <None>");
                spliceCommands.Add("DotsInstancingOptions", dotsInstancingOptionsBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Graph Defines

            using (var graphDefines = new ShaderStringBuilder())
            {
                graphDefines.AppendLine("#define SHADERPASS {0}", pass.referenceName);

                if (pass.defines != null)
                {
                    foreach (DefineCollection.Item define in pass.defines)
                    {
                        if (define.TestActive(activeFields))
                            graphDefines.AppendLine(define.value);
                    }
                }

                if (graphRequirements.permutationCount > 0)
                {
                    List<int> activePermutationIndices;

                    // Depth Texture
                    activePermutationIndices = graphRequirements.allPermutations.instances
                        .Where(p => p.requirements.requiresDepthTexture)
                        .Select(p => p.permutationIndex)
                        .ToList();
                    if (activePermutationIndices.Count > 0)
                    {
                        graphDefines.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(activePermutationIndices));
                        graphDefines.AppendLine("#define REQUIRE_DEPTH_TEXTURE");
                        graphDefines.AppendLine("#endif");
                    }

                    // Opaque Texture
                    activePermutationIndices = graphRequirements.allPermutations.instances
                        .Where(p => p.requirements.requiresCameraOpaqueTexture)
                        .Select(p => p.permutationIndex)
                        .ToList();
                    if (activePermutationIndices.Count > 0)
                    {
                        graphDefines.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(activePermutationIndices));
                        graphDefines.AppendLine("#define REQUIRE_OPAQUE_TEXTURE");
                        graphDefines.AppendLine("#endif");
                    }
                }
                else
                {
                    // Depth Texture
                    if (graphRequirements.baseInstance.requirements.requiresDepthTexture)
                        graphDefines.AppendLine("#define REQUIRE_DEPTH_TEXTURE");

                    // Opaque Texture
                    if (graphRequirements.baseInstance.requirements.requiresCameraOpaqueTexture)
                        graphDefines.AppendLine("#define REQUIRE_OPAQUE_TEXTURE");
                }

                // Add to splice commands
                spliceCommands.Add("GraphDefines", graphDefines.ToCodeBlock());
            }

            // --------------------------------------------------
            // Includes

            var allIncludes = new IncludeCollection();
            allIncludes.Add(pass.includes);
            allIncludes.Add(graphIncludes);

            using (var preGraphIncludeBuilder = new ShaderStringBuilder())
            {
                foreach (var include in allIncludes.Where(x => x.location == IncludeLocation.Pregraph))
                {
                    if (include.TestActive(activeFields))
                        preGraphIncludeBuilder.AppendLine(include.value);
                }

                string command = GenerationUtils.GetSpliceCommand(preGraphIncludeBuilder.ToCodeBlock(), "PreGraphIncludes");
                spliceCommands.Add("PreGraphIncludes", command);
            }

            using (var graphIncludeBuilder = new ShaderStringBuilder())
            {
                foreach (var include in allIncludes.Where(x => x.location == IncludeLocation.Graph))
                {
                    if (include.TestActive(activeFields))
                        graphIncludeBuilder.AppendLine(include.value);
                }

                string command = GenerationUtils.GetSpliceCommand(graphIncludeBuilder.ToCodeBlock(), "GraphIncludes");
                spliceCommands.Add("GraphIncludes", command);
            }

            using (var postGraphIncludeBuilder = new ShaderStringBuilder())
            {
                foreach (var include in allIncludes.Where(x => x.location == IncludeLocation.Postgraph))
                {
                    if (include.TestActive(activeFields))
                        postGraphIncludeBuilder.AppendLine(include.value);
                }

                string command = GenerationUtils.GetSpliceCommand(postGraphIncludeBuilder.ToCodeBlock(), "PostGraphIncludes");
                spliceCommands.Add("PostGraphIncludes", command);
            }

            // --------------------------------------------------
            // Debug

            // Debug output all active fields

            using (var debugBuilder = new ShaderStringBuilder())
            {
                if (isDebug)
                {
                    // Active fields
                    debugBuilder.AppendLine("// ACTIVE FIELDS:");
                    foreach (FieldDescriptor field in activeFields.baseInstance.fields)
                    {
                        debugBuilder.AppendLine($"//{field.tag}.{field.name}");
                    }
                }
                if (debugBuilder.length == 0)
                    debugBuilder.AppendLine("// <None>");

                // Add to splice commands
                spliceCommands.Add("Debug", debugBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Additional Commands

            if (pass.additionalCommands != null)
            {
                foreach (AdditionalCommandCollection.Item additionalCommand in pass.additionalCommands)
                {
                    spliceCommands.Add(additionalCommand.field.token, additionalCommand.field.content);
                }
            }

            // --------------------------------------------------
            // Finalize

            // Pass Template
            string passTemplatePath = pass.passTemplatePath;

            // Shared Templates
            string[] sharedTemplateDirectories = pass.sharedTemplateDirectories;

            if (!File.Exists(passTemplatePath))
                return;

            // Process Template
            var templatePreprocessor = new ShaderSpliceUtil.TemplatePreprocessor(activeFields, spliceCommands,
                isDebug, sharedTemplateDirectories, m_assetCollection);
            templatePreprocessor.ProcessTemplateFile(passTemplatePath);
            m_Builder.Concat(templatePreprocessor.GetShaderCode());

            // Turn off the skip flag so other passes behave correctly correctly.
            CustomInterpolatorUtils.generatorSkipFlag = false;
            CustomInterpolatorUtils.generatorNodeOnly = false;
        }
    }
}
