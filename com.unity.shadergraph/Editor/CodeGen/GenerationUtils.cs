using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using Data.Util;

namespace UnityEditor.ShaderGraph
{
    static class GenerationUtils
    {
        const string kDebugSymbol = "SHADERGRAPH_DEBUG";

        public static List<IField> GetActiveFieldsFromConditionals(ConditionalField[] conditionalFields)
        {
            var fields = new List<IField>();
            foreach(ConditionalField conditionalField in conditionalFields)
            {
                if(conditionalField.condition == true)
                {
                    fields.Add(conditionalField.field);
                }
            }

            return fields;
        }

        static ActiveFields ToActiveFields(this List<IField> fields)
        {
            var activeFields = new ActiveFields();
            var baseFields = activeFields.baseInstance;

            foreach(IField field in fields)
                baseFields.Add(field);
            
            return activeFields;
        }

        public static bool GenerateShaderPass(AbstractMaterialNode outputNode, ITarget target, ShaderPass pass, GenerationMode mode, 
            ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            // Early exit if pass is not used in preview
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;

            // Get base active fields from MasterNode
            // TODO: ActiveFields should be refactored to work on IFields and convert to string as late as possible
            // After this change we can read List<IField> for conditionals directly from ActiveFields.baseInstance
            List<IField> fields;
            if(outputNode is IMasterNode masterNode)
            {
                fields = GenerationUtils.GetActiveFieldsFromConditionals(masterNode.GetConditionalFields(pass));
            }
            // Peeview shader
            else
            {
                fields = new List<IField>() { DefaultFields.GraphPixel };
            }
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
            outputNode.owner.CollectShaderKeywords(keywordCollector, mode);

            // Get upstream nodes from ShaderPass port mask
            List<AbstractMaterialNode> vertexNodes;
            List<AbstractMaterialNode> pixelNodes;
            GetUpstreamNodesForShaderPass(outputNode, pass, out vertexNodes, out pixelNodes);

            // Track permutation indices for all nodes
            List<int>[] vertexNodePermutations = new List<int>[vertexNodes.Count];
            List<int>[] pixelNodePermutations = new List<int>[pixelNodes.Count];

            // Get active fields from upstream Node requirements
            ShaderGraphRequirementsPerKeyword graphRequirements;
            GetActiveFieldsAndPermutationsForNodes(outputNode, pass, keywordCollector, vertexNodes, pixelNodes,
                vertexNodePermutations, pixelNodePermutations, activeFields, out graphRequirements);

            // GET CUSTOM ACTIVE FIELDS HERE!

            // Get active fields from ShaderPass
            AddRequiredFields(pass.requiredFields, activeFields.baseInstance);

            // Get Port references from ShaderPass
            List<MaterialSlot> pixelSlots;
            List<MaterialSlot> vertexSlots;
            if(outputNode is IMasterNode || outputNode is SubGraphOutputNode)
            {
                pixelSlots = FindMaterialSlotsOnNode(pass.pixelPorts, outputNode);
                vertexSlots = FindMaterialSlotsOnNode(pass.vertexPorts, outputNode);
            }
            else
            {
                pixelSlots = new List<MaterialSlot>()
                {
                    new Vector4MaterialSlot(0, "Out", "Out", SlotType.Output, Vector4.zero) { owner = outputNode },
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
                ApplyFieldDependencies(instance, pass.fieldDependencies);
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

                string command = GetSpliceCommand(renderStateBuilder.ToCodeBlack(), "RenderState");
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

                string command = GetSpliceCommand(passPragmaBuilder.ToCodeBlack(), "PassPragmas");
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

                string command = GetSpliceCommand(passIncludeBuilder.ToCodeBlack(), "PassIncludes");
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

                string command = GetSpliceCommand(passKeywordBuilder.ToCodeBlack(), "PassKeywords");
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
                            GenerateInterpolatorFunctions(shaderStruct, instance, out instanceGenerator);
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
                        GenerateInterpolatorFunctions(shaderStruct, activeFields.baseInstance, out interpolatorBuilder);
                    }
                    //using interp index from functions, generate packed struct descriptor 
                    GeneratePackedStruct(shaderStruct, activeFields, out packStruct);
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
                    GenerateShaderStruct(shaderStruct, activeFields, out structBuilder);
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
                var vertexGraphInputGenerator = new ShaderGenerator();
                var vertexGraphFunctionBuilder = new ShaderStringBuilder();
                var vertexGraphOutputBuilder = new ShaderStringBuilder();

                // Build vertex graph outputs
                // Add struct fields to active fields
                SubShaderGenerator.GenerateVertexDescriptionStruct(vertexGraphOutputBuilder, vertexSlots, vertexGraphOutputName, activeFields.baseInstance);

                // Build vertex graph functions from ShaderPass vertex port mask
                SubShaderGenerator.GenerateVertexDescriptionFunction(
                    outputNode.owner as GraphData,
                    vertexGraphFunctionBuilder,
                    functionRegistry,
                    propertyCollector,
                    keywordCollector,
                    mode,
                    outputNode,
                    vertexNodes,
                    vertexNodePermutations,
                    vertexSlots,
                    vertexGraphInputName,
                    vertexGraphFunctionName,
                    vertexGraphOutputName);

                // Generate final shader strings
                vertexBuilder.AppendLines(vertexGraphInputGenerator.GetShaderString(0, false));
                vertexBuilder.AppendNewLine();
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
            var pixelGraphInputGenerator = new ShaderGenerator();
            var pixelGraphOutputBuilder = new ShaderStringBuilder();
            var pixelGraphFunctionBuilder = new ShaderStringBuilder();

            // Build pixel graph outputs
            // Add struct fields to active fields
            SubShaderGenerator.GenerateSurfaceDescriptionStruct(pixelGraphOutputBuilder, pixelSlots, pixelGraphOutputName, activeFields.baseInstance);

            // Build pixel graph functions from ShaderPass pixel port mask
            SubShaderGenerator.GenerateSurfaceDescriptionFunction(
                pixelNodes,
                pixelNodePermutations,
                outputNode,
                outputNode.owner as GraphData,
                pixelGraphFunctionBuilder,
                functionRegistry,
                propertyCollector,
                keywordCollector,
                mode,
                pixelGraphFunctionName,
                pixelGraphOutputName,
                null,
                pixelSlots,
                pixelGraphInputName);

            using (var pixelBuilder = new ShaderStringBuilder())
            {
                // Generate final shader strings
                pixelBuilder.AppendLines(pixelGraphInputGenerator.GetShaderString(0, false));
                pixelBuilder.AppendNewLine();
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
                keywordCollector.GetKeywordsDeclaration(keywordBuilder, mode);
                if(keywordBuilder.length == 0)
                    keywordBuilder.AppendLine("// GraphKeywords: <None>");
                spliceCommands.Add("GraphKeywords", keywordBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Graph Properties

            using (var propertyBuilder = new ShaderStringBuilder())
            {
                propertyCollector.GetPropertiesDeclaration(propertyBuilder, mode, outputNode.owner.concretePrecision);
                if(propertyBuilder.length == 0)
                    propertyBuilder.AppendLine("// GraphProperties: <None>");
                spliceCommands.Add("GraphProperties", propertyBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Dots Instanced Graph Properties

            int instancedPropCount = propertyCollector.GetDotsInstancingPropertiesCount(mode);
            using (var dotsInstancedPropertyBuilder = new ShaderStringBuilder())
            {
                if (instancedPropCount > 0)
                    dotsInstancedPropertyBuilder.AppendLines(propertyCollector.GetDotsInstancingPropertiesDeclaration(mode));
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
                graphDefines.AppendLine("#define SHADERPASS {0}", pass.referenceName);
                // graphDefines.AppendLine("#define {0}", pass.referenceName);
                
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
                passTemplatePath = target.passTemplatePath;

            // Shared Templates
            string sharedTemplateDirectory;
            if(!string.IsNullOrEmpty(pass.sharedTemplateDirectory))
                sharedTemplateDirectory = pass.sharedTemplateDirectory;
            else
                sharedTemplateDirectory = target.sharedTemplateDirectory;

            if (!File.Exists(passTemplatePath))
                return false;
            
            // Process Template
            var templatePreprocessor = new ShaderSpliceUtil.TemplatePreprocessor(activeFields, spliceCommands, 
                isDebug, sharedTemplateDirectory, sourceAssetDependencyPaths);
            templatePreprocessor.ProcessTemplateFile(passTemplatePath);
            result.AddShaderChunk(templatePreprocessor.GetShaderCode().ToString(), false);
            return true;
        }

        public static Type GetTypeForStruct(string structName, string resourceClassName, string assemblyName)
        {
            // 'C# qualified assembly type names' for $buildType() commands
            string assemblyQualifiedTypeName = $"{resourceClassName}+{structName}, {assemblyName}";
            return Type.GetType(assemblyQualifiedTypeName);
        }

        static bool IsFieldActive(IField field, IActiveFields activeFields, bool isOptional)
        {
            bool fieldActive = true;
            if (!activeFields.Contains(field) && isOptional)
                fieldActive = false; //if the field is optional and not inside of active fields
            return fieldActive;
        }

        static  void GenerateShaderStruct(StructDescriptor shaderStruct, ActiveFields activeFields, out ShaderStringBuilder structBuilder)
        {
            structBuilder = new ShaderStringBuilder();
            structBuilder.AppendLine($"struct {shaderStruct.name}");
            using(structBuilder.BlockSemicolonScope())
            {
                foreach(SubscriptDescriptor subscript in shaderStruct.subscripts)
                {
                    bool fieldIsActive;
                    var keywordIfDefs = string.Empty;

                    if (activeFields.permutationCount > 0)
                    {
                        //find all active fields per permutation
                        var instances = activeFields.allPermutations.instances
                            .Where(i => IsFieldActive(subscript, i, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional))).ToList();
                        fieldIsActive = instances.Count > 0;
                        if (fieldIsActive)
                            keywordIfDefs = KeywordUtil.GetKeywordPermutationSetConditional(instances.Select(i => i.permutationIndex).ToList());
                    }
                    else
                        fieldIsActive = IsFieldActive(subscript, activeFields.baseInstance, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional));
                        //else just find active fields

                    if (fieldIsActive)
                    {
                        //if field is active:
                        if(subscript.hasPreprocessor())
                            structBuilder.AppendLine($"#if {subscript.preprocessor}");

                        //if in permutation, add permutation ifdef
                        if(!string.IsNullOrEmpty(keywordIfDefs))
                            structBuilder.AppendLine(keywordIfDefs);
                        
                        //check for a semantic, build string if valid
                        string semantic = subscript.hasSemantic() ? $" : {subscript.semantic}" : string.Empty;
                        structBuilder.AppendLine($"{subscript.type} {subscript.name}{semantic};");

                        //if in permutation, add permutation endif
                        if (!string.IsNullOrEmpty(keywordIfDefs))
                            structBuilder.AppendLine("#endif"); //TODO: add debug collector 

                        if(subscript.hasPreprocessor())
                            structBuilder.AppendLine("#endif");                        
                    }            
                }
            }
        }

        static void GeneratePackedStruct(StructDescriptor shaderStruct, ActiveFields activeFields, out StructDescriptor packStruct)
        {
            packStruct = new StructDescriptor() { name = "Packed" + shaderStruct.name, interpolatorPack = true,
                subscripts = new SubscriptDescriptor[]{} };
            List<SubscriptDescriptor> packedSubscripts = new List<SubscriptDescriptor>();
            List<int> packedCounts = new List<int>();

            foreach(SubscriptDescriptor subscript in shaderStruct.subscripts)
            {
                var fieldIsActive = false;
                var keywordIfDefs = string.Empty;

                if (activeFields.permutationCount > 0)
                {
                    //find all active fields per permutation
                    var instances = activeFields.allPermutations.instances
                        .Where(i => IsFieldActive(subscript, i, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional))).ToList();
                    fieldIsActive = instances.Count > 0;
                    if (fieldIsActive)
                        keywordIfDefs = KeywordUtil.GetKeywordPermutationSetConditional(instances.Select(i => i.permutationIndex).ToList());
                }
                else
                    fieldIsActive = IsFieldActive(subscript, activeFields.baseInstance, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional));
                    //else just find active fields

                if (fieldIsActive)
                {
                    //if field is active:
                    if(subscript.hasSemantic() || subscript.vectorCount == 0)  
                        packedSubscripts.Add(subscript);
                    else
                    {
                        // pack float field
                        int vectorCount = subscript.vectorCount;
                        // super simple packing: use the first interpolator that has room for the whole value
                        int interpIndex = packedCounts.FindIndex(x => (x + vectorCount <= 4));
                        int firstChannel;
                        if (interpIndex < 0)
                        {
                            // allocate a new interpolator
                            interpIndex = packedCounts.Count;
                            firstChannel = 0;
                            packedCounts.Add(vectorCount);
                        }
                        else
                        {
                            // pack into existing interpolator
                            firstChannel = packedCounts[interpIndex];
                            packedCounts[interpIndex] += vectorCount;
                        }
                        var packedSubscript = new SubscriptDescriptor(packStruct.name, "interp" + interpIndex, "", subscript.type,
                            "TEXCOORD" + interpIndex, subscript.preprocessor, SubscriptOptions.Static);
                        packedSubscripts.Add(packedSubscript);                        
                    }
                }            
            }
            packStruct.subscripts = packedSubscripts.ToArray();
        }

        static void GenerateInterpolatorFunctions(StructDescriptor shaderStruct, IActiveFields activeFields, out ShaderStringBuilder interpolatorBuilder)
        {
            //set up function string builders and struct builder 
            List<int> packedCounts = new List<int>();
            var packBuilder = new ShaderStringBuilder();
            var unpackBuilder = new ShaderStringBuilder();
            interpolatorBuilder = new ShaderStringBuilder();
            string packedStruct = "Packed" + shaderStruct.name;
            
            //declare function headers
            packBuilder.AppendLine($"{packedStruct} Pack{shaderStruct.name} ({shaderStruct.name} input)");
            packBuilder.AppendLine("{");
            packBuilder.IncreaseIndent();
            packBuilder.AppendLine($"{packedStruct} output;");

            unpackBuilder.AppendLine($"{shaderStruct.name} Unpack{shaderStruct.name} ({packedStruct} input)");
            unpackBuilder.AppendLine("{");
            unpackBuilder.IncreaseIndent();
            unpackBuilder.AppendLine($"{shaderStruct.name} output;");

            foreach(SubscriptDescriptor subscript in shaderStruct.subscripts)
            {
                if(IsFieldActive(subscript, activeFields, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional)))
                {
                    int vectorCount = subscript.vectorCount;
                    if(subscript.hasPreprocessor())
                    {
                        packBuilder.AppendLine($"#if {subscript.preprocessor}");
                        unpackBuilder.AppendLine($"#if {subscript.preprocessor}");
                    }
                    if(subscript.hasSemantic() || vectorCount == 0)
                    {
                        packBuilder.AppendLine($"output.{subscript.name} = input.{subscript.name};");
                        unpackBuilder.AppendLine($"output.{subscript.name} = input.{subscript.name};");
                    }
                    else
                    {
                        // pack float field
                        // super simple packing: use the first interpolator that has room for the whole value
                        int interpIndex = packedCounts.FindIndex(x => (x + vectorCount <= 4));
                        int firstChannel;
                        if (interpIndex < 0)
                        {
                            // allocate a new interpolator
                            interpIndex = packedCounts.Count;
                            firstChannel = 0;
                            packedCounts.Add(vectorCount);
                        }
                        else
                        {
                            // pack into existing interpolator
                            firstChannel = packedCounts[interpIndex];
                            packedCounts[interpIndex] += vectorCount;
                        }
                        // add code to packer and unpacker -- add subscript to packedstruct
                        string packedChannels = ShaderSpliceUtil.GetChannelSwizzle(firstChannel, vectorCount);
                        packBuilder.AppendLine($"output.interp{interpIndex}.{packedChannels} =  input.{subscript.name};");
                        unpackBuilder.AppendLine($"output.{subscript.name} = input.interp{interpIndex}.{packedChannels};");
                    }
                    
                    if(subscript.hasPreprocessor())
                    {
                        packBuilder.AppendLine("#endif");
                        unpackBuilder.AppendLine("#endif");
                    }
                }
            }
            //close function declarations
            packBuilder.AppendLine("return output;");
            packBuilder.DecreaseIndent();
            packBuilder.AppendLine("}");

            unpackBuilder.AppendLine("return output;");
            unpackBuilder.DecreaseIndent();
            unpackBuilder.AppendLine("}");
            
            interpolatorBuilder.Concat(packBuilder);
            interpolatorBuilder.Concat(unpackBuilder);
        }

        static void GetUpstreamNodesForShaderPass(AbstractMaterialNode outputNode, ShaderPass pass, out List<AbstractMaterialNode> vertexNodes, out List<AbstractMaterialNode> pixelNodes)
        {
            // Traverse Graph Data
            vertexNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.vertexPorts);

            pixelNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.pixelPorts);
        }

        static void GetActiveFieldsAndPermutationsForNodes(AbstractMaterialNode outputNode, ShaderPass pass, 
            KeywordCollector keywordCollector,  List<AbstractMaterialNode> vertexNodes, List<AbstractMaterialNode> pixelNodes,
            List<int>[] vertexNodePermutations, List<int>[] pixelNodePermutations,
            ActiveFields activeFields, out ShaderGraphRequirementsPerKeyword graphRequirements)
        {
            // Initialize requirements
            ShaderGraphRequirementsPerKeyword pixelRequirements = new ShaderGraphRequirementsPerKeyword();
            ShaderGraphRequirementsPerKeyword vertexRequirements = new ShaderGraphRequirementsPerKeyword();
            graphRequirements = new ShaderGraphRequirementsPerKeyword();

            // Evaluate all Keyword permutations
            if (keywordCollector.permutations.Count > 0)
            {
                for(int i = 0; i < keywordCollector.permutations.Count; i++)
                {
                    // Get active nodes for this permutation
                    var localVertexNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
                    var localPixelNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
                    NodeUtils.DepthFirstCollectNodesFromNode(localVertexNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.vertexPorts, keywordCollector.permutations[i]);
                    NodeUtils.DepthFirstCollectNodesFromNode(localPixelNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.pixelPorts, keywordCollector.permutations[i]);

                    // Track each vertex node in this permutation
                    foreach(AbstractMaterialNode vertexNode in localVertexNodes)
                    {
                        int nodeIndex = vertexNodes.IndexOf(vertexNode);

                        if(vertexNodePermutations[nodeIndex] == null)
                            vertexNodePermutations[nodeIndex] = new List<int>();
                        vertexNodePermutations[nodeIndex].Add(i);
                    }

                    // Track each pixel node in this permutation
                    foreach(AbstractMaterialNode pixelNode in localPixelNodes)
                    {
                        int nodeIndex = pixelNodes.IndexOf(pixelNode);

                        if(pixelNodePermutations[nodeIndex] == null)
                            pixelNodePermutations[nodeIndex] = new List<int>();
                        pixelNodePermutations[nodeIndex].Add(i);
                    }

                    // Get requirements for this permutation
                    vertexRequirements[i].SetRequirements(ShaderGraphRequirements.FromNodes(localVertexNodes, ShaderStageCapability.Vertex, false));
                    pixelRequirements[i].SetRequirements(ShaderGraphRequirements.FromNodes(localPixelNodes, ShaderStageCapability.Fragment, false));

                    // Add active fields
                    var conditionalFields = GetActiveFieldsFromConditionals(GetConditionalFieldsFromGraphRequirements(vertexRequirements[i].requirements, activeFields[i]));
                    conditionalFields.AddRange(GetActiveFieldsFromConditionals(GetConditionalFieldsFromGraphRequirements(pixelRequirements[i].requirements, activeFields[i])));
                    foreach(var field in conditionalFields)
                    {
                        activeFields[i].Add(field);
                    }                    
                }
            }
            // No Keywords
            else
            {
                // Get requirements
                vertexRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false));
                pixelRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false));

                // Add active fields
                var conditionalFields = GetActiveFieldsFromConditionals(GetConditionalFieldsFromGraphRequirements(vertexRequirements.baseInstance.requirements, activeFields.baseInstance));
                conditionalFields.AddRange(GetActiveFieldsFromConditionals(GetConditionalFieldsFromGraphRequirements(pixelRequirements.baseInstance.requirements, activeFields.baseInstance)));
                foreach(var field in conditionalFields)
                {
                    activeFields.baseInstance.Add(field);
                } 
            }
            
            // Build graph requirements
            graphRequirements.UnionWith(pixelRequirements);
            graphRequirements.UnionWith(vertexRequirements);
        }

        static ConditionalField[] GetConditionalFieldsFromGraphRequirements(ShaderGraphRequirements requirements, IActiveFields activeFields)
        {
            return new ConditionalField[]
            {
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ScreenPosition,          requirements.requiresScreenPosition),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ScreenPosition,           requirements.requiresScreenPosition &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.VertexColor,             requirements.requiresVertexColor),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.VertexColor,              requirements.requiresVertexColor &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.FaceSign,                requirements.requiresFaceSign),

                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceNormal,       (requirements.requiresNormal & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceNormal,         (requirements.requiresNormal & NeededCoordinateSpace.View) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal,        (requirements.requiresNormal & NeededCoordinateSpace.World) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceNormal,      (requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceNormal,        (requirements.requiresNormal & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceNormal,          (requirements.requiresNormal & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal,         (requirements.requiresNormal & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceNormal,       (requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),

                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceTangent,      (requirements.requiresTangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceTangent,        (requirements.requiresTangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent,       (requirements.requiresTangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceTangent,     (requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceTangent,       (requirements.requiresTangent & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceTangent,         (requirements.requiresTangent & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent,        (requirements.requiresTangent & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceTangent,      (requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceBiTangent,    (requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceBiTangent,      (requirements.requiresBitangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent,     (requirements.requiresBitangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceBiTangent,   (requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,     (requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceBiTangent,       (requirements.requiresBitangent & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent,      (requirements.requiresBitangent & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceBiTangent,    (requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),

                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpacePosition,     (requirements.requiresPosition & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpacePosition,       (requirements.requiresPosition & NeededCoordinateSpace.View) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition,      (requirements.requiresPosition & NeededCoordinateSpace.World) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpacePosition,    (requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,(requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpacePosition,     (requirements.requiresPosition & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpacePosition,       (requirements.requiresPosition & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition,      (requirements.requiresPosition & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpacePosition,    (requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.AbsoluteWorldSpacePosition,(requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv0,                     requirements.requiresMeshUVs.Contains(UVChannel.UV0)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv1,                     requirements.requiresMeshUVs.Contains(UVChannel.UV1)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv2,                     requirements.requiresMeshUVs.Contains(UVChannel.UV2)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv3,                     requirements.requiresMeshUVs.Contains(UVChannel.UV3)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv0,                      requirements.requiresMeshUVs.Contains(UVChannel.UV0) &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv1,                      requirements.requiresMeshUVs.Contains(UVChannel.UV1) &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv2,                      requirements.requiresMeshUVs.Contains(UVChannel.UV2) &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv3,                      requirements.requiresMeshUVs.Contains(UVChannel.UV3) &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),

                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TimeParameters,          requirements.requiresTime),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TimeParameters,           requirements.requiresTime &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
            };
        }

        static void AddRequiredFields(
            IField[] passRequiredFields,            // fields the pass requires
            IActiveFieldsSet activeFields)
        {
            if (passRequiredFields != null)
            {
                foreach (var requiredField in passRequiredFields)
                {
                    activeFields.AddAll(requiredField);
                }
            }
        }

        static void ApplyFieldDependencies(IActiveFields activeFields, FieldDependency[] dependencies)
        {
            // add active fields to queue
            Queue<IField> fieldsToPropagate = new Queue<IField>();
            foreach (var f in activeFields.fields)
            {
                fieldsToPropagate.Enqueue(f);
            }

            // foreach field in queue:
            while (fieldsToPropagate.Count > 0)
            {
                IField field = fieldsToPropagate.Dequeue();
                if (activeFields.Contains(field))           // this should always be true
                {
                    if(dependencies == null)
                        return;
                        
                    // find all dependencies of field that are not already active
                    foreach (FieldDependency d in dependencies.Where(d => (d.field == field) && !activeFields.Contains(d.dependsOn)))
                    {
                        // activate them and add them to the queue
                        activeFields.Add(d.dependsOn);
                        fieldsToPropagate.Enqueue(d.dependsOn);
                    }
                }
            }
        }

        static List<MaterialSlot> FindMaterialSlotsOnNode(IEnumerable<int> slots, AbstractMaterialNode node)
        {
            if (slots == null)
                return null;

            var activeSlots = new List<MaterialSlot>();
            foreach (var id in slots)
            {
                MaterialSlot slot = node.FindSlot<MaterialSlot>(id);
                if (slot != null)
                {
                    activeSlots.Add(slot);
                }
            }
            return activeSlots;
        }

        static string GetSpliceCommand(string command, string token)
        {
            return !string.IsNullOrEmpty(command) ? command : $"// {token}: <None>";
        }

        public static string GetDefaultTemplatePath(string templateName)
        {
            var basePath = "Packages/com.unity.shadergraph/Editor/Templates/";
            string templatePath = Path.Combine(basePath, templateName);

            if (File.Exists(templatePath))
                return templatePath;

            throw new FileNotFoundException(string.Format(@"Cannot find a template with name ""{0}"".", templateName));
        }

        public static string GetDefaultSharedTemplateDirectory()
        {
            return "Packages/com.unity.shadergraph/Editor/Templates";
        }

        public static string GetShaderForNode(AbstractMaterialNode node, GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null)
        {
            var activeNodeList = ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);

            var shaderProperties = new PropertyCollector();
            var shaderKeywords = new KeywordCollector();
            if (node.owner != null)
            {
                node.owner.CollectShaderProperties(shaderProperties, mode);
                node.owner.CollectShaderKeywords(shaderKeywords, mode);
            }

            if(node.owner.GetKeywordPermutationCount() > ShaderGraphPreferences.variantLimit)
            {
                node.owner.AddValidationError(node.tempId, ShaderKeyword.kVariantLimitWarning, Rendering.ShaderCompilerMessageSeverity.Error);
                
                configuredTextures = shaderProperties.GetConfiguredTexutres();
                return ShaderGraphImporter.k_ErrorShader;
            }

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                activeNode.CollectShaderProperties(shaderProperties, mode);

            var finalShader = new ShaderStringBuilder();
            finalShader.AppendLine(@"Shader ""{0}""", outputName);
            using (finalShader.BlockScope())
            {
                SubShaderGenerator.GeneratePropertiesBlock(finalShader, shaderProperties, shaderKeywords, mode);
                
                if(node is IMasterNode masterNode)
                {
                    foreach (var target in node.owner.targets)
                    {
                        ISubShader subShader;
                        if(target.TryGetSubShader(masterNode, out subShader))
                        {
                            if (mode != GenerationMode.Preview || target.Validate(GraphicsSettings.renderPipelineAsset))
                                finalShader.AppendLines(subShader.GetSubshader(node, target, mode, sourceAssetDependencyPaths));
                        }
                    }
                }
                else
                {
                    PreviewSubShader subShader = new PreviewSubShader();
                    PreviewTarget target = new PreviewTarget();
                    finalShader.AppendLines(subShader.GetSubshader(node, target, mode, sourceAssetDependencyPaths));
                }

                finalShader.AppendLine(@"FallBack ""Hidden/InternalErrorShader""");
            }
            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.ToString();
        }
    }
}
