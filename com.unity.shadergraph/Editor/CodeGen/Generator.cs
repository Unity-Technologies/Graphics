using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using Data.Util;

namespace UnityEditor.ShaderGraph
{
    class Generator
    {
        const string kDebugSymbol = "SHADERGRAPH_DEBUG";

        GraphData m_GraphData;
        AbstractMaterialNode m_OutputNode;
        ITargetImplementation[] m_TargetImplementations;
        GenerationMode m_Mode;
        string m_Name;

        ShaderStringBuilder m_Builder;
        List<PropertyCollector.TextureInfo> m_ConfiguredTextures;
        List<string> m_AssetDependencyPaths;

        public string generatedShader => m_Builder.ToCodeBlack();
        public List<PropertyCollector.TextureInfo> configuredTextures => m_ConfiguredTextures;
        public List<string> assetDependencyPaths => m_AssetDependencyPaths;

        public Generator(GraphData graphData, AbstractMaterialNode outputNode, GenerationMode mode, string name)
        {
            m_GraphData = graphData;
            m_OutputNode = outputNode;
            m_Mode = mode;
            m_Name = name;

            m_Builder = new ShaderStringBuilder();
            m_ConfiguredTextures = new List<PropertyCollector.TextureInfo>();
            m_AssetDependencyPaths = new List<string>();

            BuildShader();
        }

        void GetTargetImplementations()
        {
            if(m_OutputNode is IMasterNode masterNode)
            {
                m_TargetImplementations = m_GraphData.activeTargetImplementations.ToArray();
            }
            else
            {
                m_TargetImplementations = new ITargetImplementation[] { new DefaultPreviewTarget() };
            }
        }

        void GetAssetDependencyPaths(TargetSetupContext context)
        {
            foreach(string assetDependency in context.assetDependencyPaths)
            {
                m_AssetDependencyPaths.Add(assetDependency);
            }
        }

        public static List<IField> GatherActiveFieldsFromNode(AbstractMaterialNode outputNode, ShaderPass pass) //TODO: clean up this active field gathering 
        {
            if(outputNode is IMasterNode masterNode)
            {
                return GenerationUtils.GetActiveFieldsFromConditionals(masterNode.GetConditionalFields(pass));
            }
            // Preview shader
            else
            {
                return new List<IField>() { DefaultFields.GraphPixel };
            }
        }

        void BuildShader()
        {
            var activeNodeList = ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, m_OutputNode);

            var shaderProperties = new PropertyCollector();
            var shaderKeywords = new KeywordCollector();
            m_GraphData.CollectShaderProperties(shaderProperties, m_Mode);
            m_GraphData.CollectShaderKeywords(shaderKeywords, m_Mode);

            if(m_GraphData.GetKeywordPermutationCount() > ShaderGraphPreferences.variantLimit)
            {
                m_GraphData.AddValidationError(m_OutputNode.tempId, ShaderKeyword.kVariantLimitWarning, Rendering.ShaderCompilerMessageSeverity.Error);
                
                m_ConfiguredTextures = shaderProperties.GetConfiguredTexutres();
                m_Builder.AppendLines(ShaderGraphImporter.k_ErrorShader);
            }

            GetTargetImplementations();

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                activeNode.CollectShaderProperties(shaderProperties, m_Mode);

            m_Builder.AppendLine(@"Shader ""{0}""", m_Name);
            using (m_Builder.BlockScope())
            {
                GenerationUtils.GeneratePropertiesBlock(m_Builder, shaderProperties, shaderKeywords, m_Mode);
                
                for(int i = 0; i < m_TargetImplementations.Length; i++)
                {
                    TargetSetupContext context = new TargetSetupContext();
                    context.SetMasterNode(m_OutputNode as IMasterNode);
                    m_TargetImplementations[i].SetupTarget(ref context); 
                    GetAssetDependencyPaths(context);
                    GenerateSubShader(i, context.descriptor);
                }

                m_Builder.AppendLine(@"FallBack ""Hidden/InternalErrorShader""");
            }

            m_ConfiguredTextures = shaderProperties.GetConfiguredTexutres();
        }

        void GenerateSubShader(int targetIndex, SubShaderDescriptor descriptor)
        {
            m_Builder.AppendLine("SubShader");
            using(m_Builder.BlockScope())
            {
                GenerationUtils.GenerateSubShaderTags(m_OutputNode as IMasterNode, descriptor, m_Builder);

                foreach(ConditionalShaderPass pass in descriptor.passes)
                {
                    var fields = GatherActiveFieldsFromNode(m_OutputNode, pass.shaderPass);

                    // TODO: cleanup this preview check, needed for HD decal preview pass
                    if(m_Mode == GenerationMode.Preview) 
                        fields.Add(DefaultFields.IsPreview);

                    // Check masternode fields for valid passes
                    if(pass.TestActive(fields)) 
                        GenerateShaderPass(targetIndex, pass.shaderPass, fields);
                }
            }
        }

        void GenerateShaderPass(int targetIndex, ShaderPass pass, List<IField> fields)
        {
            // Early exit if pass is not used in preview
            if(m_Mode == GenerationMode.Preview && !pass.useInPreview)
                return;

            var activeFields = fields.ToActiveFields();

            // --------------------------------------------------
            // Debug

            // Get scripting symbols
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            bool isDebug = defines.Contains(kDebugSymbol);

            // --------------------------------------------------
            // Setup

            // Initiailize Collectors
            var propertyCollector = new PropertyCollector();
            var keywordCollector = new KeywordCollector();
            m_OutputNode.owner.CollectShaderKeywords(keywordCollector, m_Mode);

            // Get upstream nodes from ShaderPass port mask
            List<AbstractMaterialNode> vertexNodes;
            List<AbstractMaterialNode> pixelNodes;
            GenerationUtils.GetUpstreamNodesForShaderPass(m_OutputNode, pass, out vertexNodes, out pixelNodes);

            // Track permutation indices for all nodes
            List<int>[] vertexNodePermutations = new List<int>[vertexNodes.Count];
            List<int>[] pixelNodePermutations = new List<int>[pixelNodes.Count];

            // Get active fields from upstream Node requirements
            ShaderGraphRequirementsPerKeyword graphRequirements;
            GenerationUtils.GetActiveFieldsAndPermutationsForNodes(m_OutputNode, pass, keywordCollector, vertexNodes, pixelNodes,
                vertexNodePermutations, pixelNodePermutations, activeFields, out graphRequirements);

            // GET CUSTOM ACTIVE FIELDS HERE!

            // Get active fields from ShaderPass
            GenerationUtils.AddRequiredFields(pass.requiredFields, activeFields.baseInstance);

            // Get Port references from ShaderPass
            List<MaterialSlot> pixelSlots;
            List<MaterialSlot> vertexSlots;
            if(m_OutputNode is IMasterNode || m_OutputNode is SubGraphOutputNode)
            {
                pixelSlots = GenerationUtils.FindMaterialSlotsOnNode(pass.pixelPorts, m_OutputNode);
                vertexSlots = GenerationUtils.FindMaterialSlotsOnNode(pass.vertexPorts, m_OutputNode);
            }
            else
            {
                pixelSlots = new List<MaterialSlot>()
                {
                    new Vector4MaterialSlot(0, "Out", "Out", SlotType.Output, Vector4.zero) { owner = m_OutputNode },
                };
                vertexSlots = new List<MaterialSlot>();
            }

            // Function Registry
            var functionBuilder = new ShaderStringBuilder();
            var functionRegistry = new FunctionRegistry(functionBuilder);

            // Hash table of named $splice(name) commands
            // Key: splice token
            // Value: string to splice
            Dictionary<string, string> spliceCommands = new Dictionary<string, string>();

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
            if(!string.IsNullOrEmpty(pass.displayName))
            {
                spliceCommands.Add("PassName", $"Name \"{pass.displayName}\"");
            }
            else
            {
                spliceCommands.Add("PassName", "// Name: <None>");
            }

            // Tags
            if(!string.IsNullOrEmpty(pass.lightMode))
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
                foreach(RenderState.Type type in Enum.GetValues(typeof(RenderState.Type)))
                {
                    var renderStates = pass.renderStates?.Where(x => x.renderState.type == type);
                    if(renderStates != null)
                    {
                        foreach(ConditionalRenderState renderState in renderStates)
                        {
                            string value = null;
                            if(renderState.TestActive(fields, out value))
                            {
                                renderStateBuilder.AppendLine(value);
                                break;
                            }
                        }
                    }
                }

                string command = GenerationUtils.GetSpliceCommand(renderStateBuilder.ToCodeBlack(), "RenderState");
                spliceCommands.Add("RenderState", command);
            }

            // Pragmas
            using (var passPragmaBuilder = new ShaderStringBuilder())
            {
                if(pass.pragmas != null)
                {
                    foreach(ConditionalPragma pragma in pass.pragmas)
                    {
                        string value = null;
                        if(pragma.TestActive(fields, out value))
                            passPragmaBuilder.AppendLine(value);
                    }
                }

                string command = GenerationUtils.GetSpliceCommand(passPragmaBuilder.ToCodeBlack(), "PassPragmas");
                spliceCommands.Add("PassPragmas", command);
            }

            // Includes
            using (var passIncludeBuilder = new ShaderStringBuilder())
            {
                if(pass.includes != null)
                {
                    foreach(ConditionalInclude include in pass.includes)
                    {
                        string value = null;
                        if(include.TestActive(fields, out value))
                            passIncludeBuilder.AppendLine(value);
                    }
                }

                string command = GenerationUtils.GetSpliceCommand(passIncludeBuilder.ToCodeBlack(), "PassIncludes");
                spliceCommands.Add("PassIncludes", command);
            }

            // Keywords
            using (var passKeywordBuilder = new ShaderStringBuilder())
            {
                if(pass.keywords != null)
                {
                    foreach(ConditionalKeyword keyword in pass.keywords)
                    {
                        string value = null;
                        if(keyword.TestActive(fields, out value))
                            passKeywordBuilder.AppendLine(value);
                    }
                }

                string command = GenerationUtils.GetSpliceCommand(passKeywordBuilder.ToCodeBlack(), "PassKeywords");
                spliceCommands.Add("PassKeywords", command);
            }

            // -----------------------------
            // Generated structs and Packing code 
            var interpolatorBuilder = new ShaderStringBuilder();
            var passStructs = new List<StructDescriptor>();

            if(pass.structs != null)
            {
                passStructs.AddRange(pass.structs);

                foreach (StructDescriptor shaderStruct in pass.structs)
                {
                    if(shaderStruct.interpolatorPack == false)
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
                            var key = instanceGenerator.ToCodeBlack();
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

                            interpolatorBuilder.Concat(generated.Value.Item1);
                        }
                        if (generatedPackedTypes.Count > 0)
                            interpolatorBuilder.AppendLine("#endif");
                    }
                    else
                    {
                        GenerationUtils.GenerateInterpolatorFunctions(shaderStruct, activeFields.baseInstance, out interpolatorBuilder);
                    }
                    //using interp index from functions, generate packed struct descriptor 
                    GenerationUtils.GeneratePackedStruct(shaderStruct, activeFields, out packStruct);
                    passStructs.Add(packStruct);
                }           
            }
            if(interpolatorBuilder.length != 0) //hard code interpolators to float, TODO: proper handle precision 
                interpolatorBuilder.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Float.ToShaderString());
            else
                interpolatorBuilder.AppendLine("//Interpolator Packs: <None>");
            spliceCommands.Add("InterpolatorPack", interpolatorBuilder.ToCodeBlack());
            
            // Generated String Builders for all struct types 
            var passStructBuilder = new ShaderStringBuilder();
            if(passStructs != null)
            {
                var structBuilder = new ShaderStringBuilder();
                foreach(StructDescriptor shaderStruct in passStructs)
                {
                    GenerationUtils.GenerateShaderStruct(shaderStruct, activeFields, out structBuilder);
                    structBuilder.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Float.ToShaderString()); //hard code structs to float, TODO: proper handle precision 
                    passStructBuilder.Concat(structBuilder);
                }
            }
            if(passStructBuilder.length == 0) 
                passStructBuilder.AppendLine("//Pass Structs: <None>");
            spliceCommands.Add("PassStructs", passStructBuilder.ToCodeBlack());

            // --------------------------------------------------
            // Graph Vertex

            var vertexBuilder = new ShaderStringBuilder();

            // If vertex modification enabled
            if (activeFields.baseInstance.Contains(DefaultFields.GraphVertex))
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
            if(vertexBuilder.length == 0)
                vertexBuilder.AppendLine("// GraphVertex: <None>");
            spliceCommands.Add("GraphVertex", vertexBuilder.ToCodeBlack());

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
            GenerationUtils.GenerateSurfaceDescriptionStruct(pixelGraphOutputBuilder, pixelSlots, pixelGraphOutputName, activeFields.baseInstance);

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
                pixelGraphInputName);

            using (var pixelBuilder = new ShaderStringBuilder())
            {
                // Generate final shader strings
                pixelBuilder.AppendLines(pixelGraphOutputBuilder.ToString());
                pixelBuilder.AppendNewLine();
                pixelBuilder.AppendLines(pixelGraphFunctionBuilder.ToString());
                
                // Add to splice commands
                if(pixelBuilder.length == 0)
                    pixelBuilder.AppendLine("// GraphPixel: <None>");
                spliceCommands.Add("GraphPixel", pixelBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Graph Functions

            if(functionBuilder.length == 0)
                functionBuilder.AppendLine("// GraphFunctions: <None>");
            spliceCommands.Add("GraphFunctions", functionBuilder.ToCodeBlack());

            // --------------------------------------------------
            // Graph Keywords

            using (var keywordBuilder = new ShaderStringBuilder())
            {
                keywordCollector.GetKeywordsDeclaration(keywordBuilder, m_Mode);
                if(keywordBuilder.length == 0)
                    keywordBuilder.AppendLine("// GraphKeywords: <None>");
                spliceCommands.Add("GraphKeywords", keywordBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Graph Properties

            using (var propertyBuilder = new ShaderStringBuilder())
            {
                propertyCollector.GetPropertiesDeclaration(propertyBuilder, m_Mode, m_GraphData.concretePrecision);
                if(propertyBuilder.length == 0)
                    propertyBuilder.AppendLine("// GraphProperties: <None>");
                spliceCommands.Add("GraphProperties", propertyBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Dots Instanced Graph Properties

            int instancedPropCount = propertyCollector.GetDotsInstancingPropertiesCount(m_Mode);
            using (var dotsInstancedPropertyBuilder = new ShaderStringBuilder())
            {
                if (instancedPropCount > 0)
                    dotsInstancedPropertyBuilder.AppendLines(propertyCollector.GetDotsInstancingPropertiesDeclaration(m_Mode));
                else
                    dotsInstancedPropertyBuilder.AppendLine("// DotsInstancedProperties: <None>");
                spliceCommands.Add("DotsInstancedProperties", dotsInstancedPropertyBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Dots Instancing Options

            using (var dotsInstancingOptionsBuilder = new ShaderStringBuilder())
            {
                if (instancedPropCount > 0)
                {
                    dotsInstancingOptionsBuilder.AppendLine("#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL))");
                    dotsInstancingOptionsBuilder.AppendLine("    #define UNITY_SUPPORT_INSTANCING");
                    dotsInstancingOptionsBuilder.AppendLine("#endif");
                    dotsInstancingOptionsBuilder.AppendLine("#if defined(UNITY_SUPPORT_INSTANCING) && defined(INSTANCING_ON)");
                    dotsInstancingOptionsBuilder.AppendLine("    #define UNITY_DOTS_INSTANCING_ENABLED");
                    dotsInstancingOptionsBuilder.AppendLine("#endif");
                    dotsInstancingOptionsBuilder.AppendLine("#pragma instancing_options nolightprobe");
                    dotsInstancingOptionsBuilder.AppendLine("#pragma instancing_options nolodfade");
                }
                else
                {
                    if (pass.defaultDotsInstancingOptions != null)
                    {
                        foreach (var instancingOption in pass.defaultDotsInstancingOptions)
                            dotsInstancingOptionsBuilder.AppendLine(instancingOption);
                    }
                }
                if(dotsInstancingOptionsBuilder.length == 0)
                    dotsInstancingOptionsBuilder.AppendLine("// DotsInstancingOptions: <None>");
                spliceCommands.Add("DotsInstancingOptions", dotsInstancingOptionsBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Graph Defines

            using (var graphDefines = new ShaderStringBuilder())
            {
                // TODO: Solve inconsistency
                // URP: #define PASSNAME
                // HDRP: #define SHADERPASS PASSNAME
                // graphDefines.AppendLine("#define SHADERPASS {0}", pass.referenceName);
                graphDefines.AppendLine("#define {0}", pass.referenceName);
                
                if(pass.defines != null)
                {
                    foreach(ConditionalDefine define in pass.defines)
                    {
                        string value = null;
                        if(define.TestActive(fields, out value))
                            graphDefines.AppendLine(value);
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
                spliceCommands.Add("GraphDefines", graphDefines.ToCodeBlack());
            }

            // --------------------------------------------------
            // Main

            // Main include is expected to contain vert/frag definitions for the pass
            // This must be defined after all graph code
            using (var mainBuilder = new ShaderStringBuilder())
            {
                if(!string.IsNullOrEmpty(pass.varyingsInclude))
                    mainBuilder.AppendLine($"#include \"{pass.varyingsInclude}\"");
                if(!string.IsNullOrEmpty(pass.passInclude))
                    mainBuilder.AppendLine($"#include \"{pass.passInclude}\"");

                // Add to splice commands
                if(mainBuilder.length == 0)
                    mainBuilder.AppendLine("// MainInclude: <None>");
                spliceCommands.Add("MainInclude", mainBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Debug

            // Debug output all active fields
            
            using(var debugBuilder = new ShaderStringBuilder())
            {
                if (isDebug)
                {
                    // Active fields
                    debugBuilder.AppendLine("// ACTIVE FIELDS:");
                    foreach (IField field in activeFields.baseInstance.fields)
                    {
                        debugBuilder.AppendLine($"//{field.tag}.{field.name}");
                    }
                }
                if(debugBuilder.length == 0)
                    debugBuilder.AppendLine("// <None>");
                
                // Add to splice commands
                spliceCommands.Add("Debug", debugBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Finalize

            // Pass Template
            string passTemplatePath;
            if(!string.IsNullOrEmpty(pass.passTemplatePath))
                passTemplatePath = pass.passTemplatePath;
            else
                passTemplatePath = m_TargetImplementations[targetIndex].passTemplatePath;

            // Shared Templates
            string sharedTemplateDirectory;
            if(!string.IsNullOrEmpty(pass.sharedTemplateDirectory))
                sharedTemplateDirectory = pass.sharedTemplateDirectory;
            else
                sharedTemplateDirectory = m_TargetImplementations[targetIndex].sharedTemplateDirectory;

            if (!File.Exists(passTemplatePath))
                return;
            
            // Process Template
            var templatePreprocessor = new ShaderSpliceUtil.TemplatePreprocessor(activeFields, spliceCommands, 
                isDebug, sharedTemplateDirectory, m_AssetDependencyPaths);
            templatePreprocessor.ProcessTemplateFile(passTemplatePath);
            m_Builder.Concat(templatePreprocessor.GetShaderCode());
        }
    }
}
