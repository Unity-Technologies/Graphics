using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using Data.Util;

namespace UnityEditor.ShaderGraph
{
    static class GenerationUtils
    {
        const string kDebugSymbol = "SHADERGRAPH_DEBUG";

        public static ActiveFields GetActiveFieldsFromConditionals(ConditionalField[] conditionalFields)
        {
            var activeFields = new ActiveFields();
            var baseFields = activeFields.baseInstance;

            foreach(ConditionalField conditionalField in conditionalFields)
            {
                if(conditionalField.condition == true)
                {
                    baseFields.Add(conditionalField.field.ToFieldString());
                }
            }

            return activeFields;
        }

        public static string ToFieldString(this IField field)
        {
            if(!string.IsNullOrEmpty(field.tag))
                return $"{field.tag}.{field.name}";
            else
                return field.name;
        }

        public static bool GenerateShaderPass(AbstractMaterialNode outputNode, ITarget target, ShaderPass pass, GenerationMode mode, 
            ActiveFields activeFields, ShaderGenerator result, List<string> sourceAssetDependencyPaths,
            List<Dependency[]> dependencies, string resourceClassName, string assemblyName)
        {
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
            AddRequiredFields(pass.requiredAttributes, activeFields.baseInstance);
            AddRequiredFields(pass.requiredVaryings, activeFields.baseInstance);

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
                ShaderSpliceUtil.ApplyDependencies(instance, dependencies);

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

            // Render state
            BuildRenderStatesFromPass(pass, ref spliceCommands);

            // --------------------------------------------------
            // Pass Code

            // Pragmas
            using (var passPragmaBuilder = new ShaderStringBuilder())
            {
                if(pass.pragmas != null)
                {
                    foreach(string pragma in pass.pragmas)
                    {
                        passPragmaBuilder.AppendLine($"#pragma {pragma}");
                    }
                }
                if(passPragmaBuilder.length == 0)
                    passPragmaBuilder.AppendLine("// PassPragmas: <None>");
                spliceCommands.Add("PassPragmas", passPragmaBuilder.ToCodeBlack());
            }

            // Includes
            using (var passIncludeBuilder = new ShaderStringBuilder())
            {
                if(pass.includes != null)
                {
                    foreach(string include in pass.includes)
                    {
                        passIncludeBuilder.AppendLine($"#include \"{include}\"");
                    }
                }
                if(passIncludeBuilder.length == 0)
                    passIncludeBuilder.AppendLine("// PassIncludes: <None>");
                spliceCommands.Add("PassIncludes", passIncludeBuilder.ToCodeBlack());
            }

            // Keywords
            using (var passKeywordBuilder = new ShaderStringBuilder())
            {
                if(pass.keywords != null)
                {
                    foreach(KeywordDescriptor keyword in pass.keywords)
                    {
                        passKeywordBuilder.AppendLine(keyword.ToDeclarationString());
                    }
                }
                if(passKeywordBuilder.length == 0)
                    passKeywordBuilder.AppendLine("// PassKeywords: <None>");
                spliceCommands.Add("PassKeywords", passKeywordBuilder.ToCodeBlack());
            }

            // --------------------------------------------------
            // Graph Vertex

            var vertexBuilder = new ShaderStringBuilder();

            // If vertex modification enabled
            if (activeFields.baseInstance.Contains("features.graphVertex"))
            {
                // Setup
                string vertexGraphInputName = "VertexDescriptionInputs";
                string vertexGraphOutputName = "VertexDescription";
                string vertexGraphFunctionName = "VertexDescriptionFunction";
                var vertexGraphInputGenerator = new ShaderGenerator();
                var vertexGraphFunctionBuilder = new ShaderStringBuilder();
                var vertexGraphOutputBuilder = new ShaderStringBuilder();

                // Build vertex graph inputs
                ShaderSpliceUtil.BuildType(GetTypeForStruct("VertexDescriptionInputs", resourceClassName, assemblyName), activeFields, vertexGraphInputGenerator, isDebug);

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

            // Build pixel graph inputs
            ShaderSpliceUtil.BuildType(GetTypeForStruct("SurfaceDescriptionInputs", resourceClassName, assemblyName), activeFields, pixelGraphInputGenerator, isDebug);

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
                graphDefines.AppendLine("#define {0}", pass.referenceName);
                if (pass.defines != null)
                {
                    foreach (var define in pass.defines)
                        graphDefines.AppendLine($"#define {define}");
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
                    foreach (string field in activeFields.baseInstance.fields)
                    {
                        debugBuilder.AppendLine("// " + field);
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
            
            // Get Template preprocessor
            string templatePath = "Packages/com.unity.shadergraph/Editor/Templates";
            var templatePreprocessor = new ShaderSpliceUtil.TemplatePreprocessor(activeFields, spliceCommands, 
                isDebug, sharedTemplateDirectory, sourceAssetDependencyPaths, assemblyName, resourceClassName);
            
            // Process Template
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
                    AddActiveFieldsFromGraphRequirements(activeFields[i], vertexRequirements[i].requirements, "VertexDescriptionInputs");
                    AddActiveFieldsFromGraphRequirements(activeFields[i], pixelRequirements[i].requirements, "SurfaceDescriptionInputs");
                }
            }
            // No Keywords
            else
            {
                // Get requirements
                vertexRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false));
                pixelRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false));

                // Add active fields
                AddActiveFieldsFromGraphRequirements(activeFields.baseInstance, vertexRequirements.baseInstance.requirements, "VertexDescriptionInputs");
                AddActiveFieldsFromGraphRequirements(activeFields.baseInstance, pixelRequirements.baseInstance.requirements, "SurfaceDescriptionInputs");
            }
            
            // Build graph requirements
            graphRequirements.UnionWith(pixelRequirements);
            graphRequirements.UnionWith(vertexRequirements);
        }

        static void AddActiveFieldsFromGraphRequirements(IActiveFields activeFields, ShaderGraphRequirements requirements, string structName)
        {
            if (requirements.requiresScreenPosition)
            {
                activeFields.Add($"{structName}.ScreenPosition");
            }

            if (requirements.requiresVertexColor)
            {
                activeFields.Add($"{structName}.VertexColor");
            }

            if (requirements.requiresFaceSign)
            {
                activeFields.Add($"{structName}.FaceSign");
            }

            if (requirements.requiresNormal != 0)
            {
                if ((requirements.requiresNormal & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add($"{structName}.ObjectSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.View) > 0)
                    activeFields.Add($"{structName}.ViewSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.World) > 0)
                    activeFields.Add($"{structName}.WorldSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add($"{structName}.TangentSpaceNormal");
            }

            if (requirements.requiresTangent != 0)
            {
                if ((requirements.requiresTangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add($"{structName}.ObjectSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add($"{structName}.ViewSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add($"{structName}.WorldSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add($"{structName}.TangentSpaceTangent");
            }

            if (requirements.requiresBitangent != 0)
            {
                if ((requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add($"{structName}.ObjectSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add($"{structName}.ViewSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add($"{structName}.WorldSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add($"{structName}.TangentSpaceBiTangent");
            }

            if (requirements.requiresViewDir != 0)
            {
                if ((requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add($"{structName}.ObjectSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.View) > 0)
                    activeFields.Add($"{structName}.ViewSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.World) > 0)
                    activeFields.Add($"{structName}.WorldSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add($"{structName}.TangentSpaceViewDirection");
            }

            if (requirements.requiresPosition != 0)
            {
                if ((requirements.requiresPosition & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add($"{structName}.ObjectSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.View) > 0)
                    activeFields.Add($"{structName}.ViewSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.World) > 0)
                    activeFields.Add($"{structName}.WorldSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add($"{structName}.TangentSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0)
                    activeFields.Add($"{structName}.AbsoluteWorldSpacePosition");
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
            {
                activeFields.Add($"{structName}.{channel.GetUVName()}");
            }

            if (requirements.requiresTime)
            {
                activeFields.Add($"{structName}.TimeParameters");
            }
        }

        static void AddRequiredFields(
            List<string> passRequiredFields,            // fields the pass requires
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

        static void BuildRenderStatesFromPass(ShaderPass pass, ref Dictionary<string, string> spliceCommands)
        {
            spliceCommands.Add("Blending", pass.BlendOverride != null ? pass.BlendOverride : "// Blending: <None>");
            spliceCommands.Add("Culling", pass.CullOverride != null ? pass.CullOverride : "// Culling: <None>");
            spliceCommands.Add("ZTest", pass.ZTestOverride != null ? pass.ZTestOverride : "// ZTest: <None>");
            spliceCommands.Add("ZWrite", pass.ZWriteOverride != null ? pass.ZWriteOverride : "// ZWrite: <None>");
            spliceCommands.Add("ZClip", pass.ZClipOverride != null ? pass.ZClipOverride : "// ZClip: <None>");
            spliceCommands.Add("ColorMask", pass.ColorMaskOverride != null ? pass.ColorMaskOverride : "// ColorMask: <None>");

            using(var stencilBuilder = new ShaderStringBuilder())
            {
                if (pass.StencilOverride != null)
                {
                    foreach (var str in pass.StencilOverride)
                        stencilBuilder.AppendLine(str);
                }
                
                spliceCommands.Add("Stencil", stencilBuilder.ToCodeBlack());
            }
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
