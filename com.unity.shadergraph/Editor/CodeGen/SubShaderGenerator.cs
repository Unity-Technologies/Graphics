using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using Data.Util;

namespace UnityEditor.ShaderGraph
{
    static class SubShaderGenerator
    {
        public static void GeneratePropertiesBlock(ShaderStringBuilder sb, PropertyCollector propertyCollector, KeywordCollector keywordCollector, GenerationMode mode)
        {
            sb.AppendLine("Properties");
            using (sb.BlockScope())
            {
                foreach (var prop in propertyCollector.properties.Where(x => x.generatePropertyBlock))
                {
                    sb.AppendLine(prop.GetPropertyBlockString());
                }

                // Keywords use hardcoded state in preview
                // Do not add them to the Property Block
                if(mode == GenerationMode.Preview)
                    return;

                foreach (var key in keywordCollector.keywords.Where(x => x.generatePropertyBlock))
                {
                    sb.AppendLine(key.GetPropertyBlockString());
                }
            }
        }

        public static void GenerateApplicationVertexInputs(ShaderGraphRequirements graphRequirements, ShaderStringBuilder vertexInputs)
        {
            vertexInputs.AppendLine("struct GraphVertexInput");
            using (vertexInputs.BlockSemicolonScope())
            {
                vertexInputs.AppendLine("float4 vertex : POSITION;");
                if(graphRequirements.requiresNormal != NeededCoordinateSpace.None || graphRequirements.requiresBitangent != NeededCoordinateSpace.None)
                    vertexInputs.AppendLine("float3 normal : NORMAL;");
                if(graphRequirements.requiresTangent != NeededCoordinateSpace.None || graphRequirements.requiresBitangent != NeededCoordinateSpace.None)
                    vertexInputs.AppendLine("float4 tangent : TANGENT;");
                if (graphRequirements.requiresVertexColor)
                {
                    vertexInputs.AppendLine("float4 color : COLOR;");
                }
                foreach (var channel in graphRequirements.requiresMeshUVs.Distinct())
                    vertexInputs.AppendLine("float4 texcoord{0} : TEXCOORD{0};", (int)channel);
                vertexInputs.AppendLine("UNITY_VERTEX_INPUT_INSTANCE_ID");
            }
        }

        public static GenerationResults GetShader(this GraphData graph, AbstractMaterialNode node, GenerationMode mode, string name)
        {
            // ----------------------------------------------------- //
            //                         SETUP                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // String builders

            var finalShader = new ShaderStringBuilder();
            var results = new GenerationResults();

            var shaderProperties = new PropertyCollector();
            var shaderKeywords = new KeywordCollector();
            var shaderPropertyUniforms = new ShaderStringBuilder();
            var shaderKeywordDeclarations = new ShaderStringBuilder();
            var shaderKeywordPermutations = new ShaderStringBuilder(1);

            var functionBuilder = new ShaderStringBuilder();
            var functionRegistry = new FunctionRegistry(functionBuilder);

            var vertexDescriptionFunction = new ShaderStringBuilder(0);

            var surfaceDescriptionInputStruct = new ShaderStringBuilder(0);
            var surfaceDescriptionStruct = new ShaderStringBuilder(0);
            var surfaceDescriptionFunction = new ShaderStringBuilder(0);

            var vertexInputs = new ShaderStringBuilder(0);

            graph.CollectShaderKeywords(shaderKeywords, mode);

            if(graph.GetKeywordPermutationCount() > ShaderGraphPreferences.variantLimit)
            {
                graph.AddValidationError(node.tempId, ShaderKeyword.kVariantLimitWarning, Rendering.ShaderCompilerMessageSeverity.Error);

                results.configuredTextures = shaderProperties.GetConfiguredTexutres();
                results.shader = string.Empty;
                return results;
            }

            // -------------------------------------
            // Get Slot and Node lists

            var activeNodeList = ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);

            var slots = new List<MaterialSlot>();
            if (node is IMasterNode || node is SubGraphOutputNode)
                slots.AddRange(node.GetInputSlots<MaterialSlot>());
            else
            {
                var outputSlots = node.GetOutputSlots<MaterialSlot>().ToList();
                if (outputSlots.Count > 0)
                    slots.Add(outputSlots[0]);
            }

            // -------------------------------------
            // Get Requirements

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList, ShaderStageCapability.Fragment);

            // ----------------------------------------------------- //
            //                         KEYWORDS                      //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Get keyword permutations

            graph.CollectShaderKeywords(shaderKeywords, mode);

            // Track permutation indicies for all nodes and requirements
            List<int>[] keywordPermutationsPerNode = new List<int>[activeNodeList.Count];

            // -------------------------------------
            // Evaluate all permutations

            for(int i = 0; i < shaderKeywords.permutations.Count; i++)
            {
                // Get active nodes for this permutation
                var localNodes = ListPool<AbstractMaterialNode>.Get();
                NodeUtils.DepthFirstCollectNodesFromNode(localNodes, node, keywordPermutation: shaderKeywords.permutations[i]);

                // Track each pixel node in this permutation
                foreach(AbstractMaterialNode pixelNode in localNodes)
                {
                    int nodeIndex = activeNodeList.IndexOf(pixelNode);

                    if(keywordPermutationsPerNode[nodeIndex] == null)
                        keywordPermutationsPerNode[nodeIndex] = new List<int>();
                    keywordPermutationsPerNode[nodeIndex].Add(i);
                }

                // Get active requirements for this permutation
                var localSurfaceRequirements = ShaderGraphRequirements.FromNodes(localNodes, ShaderStageCapability.Fragment, false);
                var localPixelRequirements = ShaderGraphRequirements.FromNodes(localNodes, ShaderStageCapability.Fragment);
            }


            // ----------------------------------------------------- //
            //                START VERTEX DESCRIPTION               //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate Vertex Description function

            vertexDescriptionFunction.AppendLine("GraphVertexInput PopulateVertexData(GraphVertexInput v)");
            using (vertexDescriptionFunction.BlockScope())
            {
                vertexDescriptionFunction.AppendLine("return v;");
            }

            // ----------------------------------------------------- //
            //               START SURFACE DESCRIPTION               //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate Input structure for Surface Description function
            // Surface Description Input requirements are needed to exclude intermediate translation spaces

            GenerateSurfaceInputStruct(surfaceDescriptionInputStruct, requirements, "SurfaceDescriptionInputs");

            results.previewMode = PreviewMode.Preview2D;
            foreach (var pNode in activeNodeList)
            {
                if (pNode.previewMode == PreviewMode.Preview3D)
                {
                    results.previewMode = PreviewMode.Preview3D;
                    break;
                }
            }

            // -------------------------------------
            // Generate Output structure for Surface Description function

            GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, useIdsInNames: !(node is IMasterNode));

            // -------------------------------------
            // Generate Surface Description function

            GenerateSurfaceDescriptionFunction(
                activeNodeList,
                keywordPermutationsPerNode,
                node,
                graph,
                surfaceDescriptionFunction,
                functionRegistry,
                shaderProperties,
                shaderKeywords,
                mode,
                outputIdProperty: results.outputIdProperty);

            // ----------------------------------------------------- //
            //           GENERATE VERTEX > PIXEL PIPELINE            //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Keyword declarations

            shaderKeywords.GetKeywordsDeclaration(shaderKeywordDeclarations, mode);

            // -------------------------------------
            // Property uniforms

            shaderProperties.GetPropertiesDeclaration(shaderPropertyUniforms, mode, graph.concretePrecision);

            // -------------------------------------
            // Generate Input structure for Vertex shader

            GenerateApplicationVertexInputs(requirements, vertexInputs);

            // ----------------------------------------------------- //
            //                      FINALIZE                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Build final shader

            finalShader.AppendLine(@"Shader ""{0}""", name);
            using (finalShader.BlockScope())
            {
                SubShaderGenerator.GeneratePropertiesBlock(finalShader, shaderProperties, shaderKeywords, mode);
                finalShader.AppendNewLine();

                finalShader.AppendLine(@"HLSLINCLUDE");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariables.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl""");

                finalShader.AppendLines(shaderKeywordDeclarations.ToString());
                finalShader.AppendLine(@"#define SHADERGRAPH_PREVIEW 1");
                finalShader.AppendNewLine();

                finalShader.AppendLines(shaderKeywordPermutations.ToString());

                finalShader.AppendLines(shaderPropertyUniforms.ToString());
                finalShader.AppendNewLine();

                finalShader.AppendLines(surfaceDescriptionInputStruct.ToString());
                finalShader.AppendNewLine();

                finalShader.Concat(functionBuilder);
                finalShader.AppendNewLine();

                finalShader.AppendLines(surfaceDescriptionStruct.ToString());
                finalShader.AppendNewLine();
                finalShader.AppendLines(surfaceDescriptionFunction.ToString());
                finalShader.AppendNewLine();

                finalShader.AppendLines(vertexInputs.ToString());
                finalShader.AppendNewLine();
                finalShader.AppendLines(vertexDescriptionFunction.ToString());
                finalShader.AppendNewLine();

                finalShader.AppendLine(@"ENDHLSL");

                finalShader.AppendLines(ShaderGenerator.GetPreviewSubShader(node, requirements));
                ListPool<AbstractMaterialNode>.Release(activeNodeList);
            }

            // -------------------------------------
            // Finalize

            results.configuredTextures = shaderProperties.GetConfiguredTexutres();
            ShaderSourceMap sourceMap;
            results.shader = finalShader.ToString(out sourceMap);
            results.sourceMap = sourceMap;
            return results;
        }

        public static void GenerateSurfaceInputStruct(ShaderStringBuilder sb, ShaderGraphRequirements requirements, string structName)
        {
            sb.AppendLine($"struct {structName}");
            using (sb.BlockSemicolonScope())
            {
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, sb);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, sb);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, sb);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, sb);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, sb);

                if (requirements.requiresVertexColor)
                    sb.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (requirements.requiresScreenPosition)
                    sb.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                if (requirements.requiresFaceSign)
                    sb.AppendLine("float {0};", ShaderGeneratorNames.FaceSign);

                foreach (var channel in requirements.requiresMeshUVs.Distinct())
                    sb.AppendLine("half4 {0};", channel.GetUVName());

                if (requirements.requiresTime)
                {
                    sb.AppendLine("float3 {0};", ShaderGeneratorNames.TimeParameters);
                }
            }
        }

        public static void GenerateSurfaceInputTransferCode(ShaderStringBuilder sb, ShaderGraphRequirements requirements, string structName, string variableName)
        {
            sb.AppendLine($"{structName} {variableName};");

            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, sb, $"{variableName}.{{0}} = IN.{{0}};");
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, sb, $"{variableName}.{{0}} = IN.{{0}};");
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, sb, $"{variableName}.{{0}} = IN.{{0}};");
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, sb, $"{variableName}.{{0}} = IN.{{0}};");
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, sb, $"{variableName}.{{0}} = IN.{{0}};");

            if (requirements.requiresVertexColor)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.VertexColor} = IN.{ShaderGeneratorNames.VertexColor};");

            if (requirements.requiresScreenPosition)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.ScreenPosition} = IN.{ShaderGeneratorNames.ScreenPosition};");

            if (requirements.requiresFaceSign)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.FaceSign} = IN.{ShaderGeneratorNames.FaceSign};");

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                sb.AppendLine($"{variableName}.{channel.GetUVName()} = IN.{channel.GetUVName()};");

            if (requirements.requiresTime)
            {
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.TimeParameters} = IN.{ShaderGeneratorNames.TimeParameters};");
            }
        }

        public static void GenerateSurfaceDescriptionStruct(ShaderStringBuilder surfaceDescriptionStruct, List<MaterialSlot> slots, string structName = "SurfaceDescription", IActiveFieldsSet activeFields = null, bool useIdsInNames = false)
        {
            surfaceDescriptionStruct.AppendLine("struct {0}", structName);
            using (surfaceDescriptionStruct.BlockSemicolonScope())
            {
                foreach (var slot in slots)
                {
                    string hlslName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                    if (useIdsInNames)
                    {
                        hlslName = $"{hlslName}_{slot.id}";
                    }

                    surfaceDescriptionStruct.AppendLine("{0} {1};", slot.concreteValueType.ToShaderString(slot.owner.concretePrecision), hlslName);

                    if (activeFields != null)
                    {
                        activeFields.AddAll(structName + "." + hlslName);
                    }
                }
            }
        }

        public static void GenerateSurfaceDescriptionFunction(
            List<AbstractMaterialNode> nodes,
            List<int>[] keywordPermutationsPerNode,
            AbstractMaterialNode rootNode,
            GraphData graph,
            ShaderStringBuilder surfaceDescriptionFunction,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            KeywordCollector shaderKeywords,
            GenerationMode mode,
            string functionName = "PopulateSurfaceData",
            string surfaceDescriptionName = "SurfaceDescription",
            Vector1ShaderProperty outputIdProperty = null,
            IEnumerable<MaterialSlot> slots = null,
            string graphInputStructName = "SurfaceDescriptionInputs")
        {
            if (graph == null)
                return;

            graph.CollectShaderProperties(shaderProperties, mode);

            surfaceDescriptionFunction.AppendLine(String.Format("{0} {1}(SurfaceDescriptionInputs IN)", surfaceDescriptionName, functionName), false);
            using (surfaceDescriptionFunction.BlockScope())
            {
                surfaceDescriptionFunction.AppendLine("{0} surface = ({0})0;", surfaceDescriptionName);
                for(int i = 0; i < nodes.Count; i++)
                {
                    GenerateDescriptionForNode(nodes[i], keywordPermutationsPerNode[i], functionRegistry, surfaceDescriptionFunction,
                        shaderProperties, shaderKeywords,
                        graph, mode);
                }

                functionRegistry.builder.currentNode = null;
                surfaceDescriptionFunction.currentNode = null;

                GenerateSurfaceDescriptionRemap(graph, rootNode, slots,
                    surfaceDescriptionFunction, mode);

                surfaceDescriptionFunction.AppendLine("return surface;");
            }
        }

        static void GenerateDescriptionForNode(
            AbstractMaterialNode activeNode,
            List<int> keywordPermutations,
            FunctionRegistry functionRegistry,
            ShaderStringBuilder descriptionFunction,
            PropertyCollector shaderProperties,
            KeywordCollector shaderKeywords,
            GraphData graph,
            GenerationMode mode)
        {
            if (activeNode is IGeneratesFunction functionNode)
            {
                functionRegistry.builder.currentNode = activeNode;
                functionNode.GenerateNodeFunction(functionRegistry, mode);
                functionRegistry.builder.ReplaceInCurrentMapping(PrecisionUtil.Token, activeNode.concretePrecision.ToShaderString());
            }

            if (activeNode is IGeneratesBodyCode bodyNode)
            {
                if(keywordPermutations != null)
                    descriptionFunction.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(keywordPermutations));

                descriptionFunction.currentNode = activeNode;
                bodyNode.GenerateNodeCode(descriptionFunction, mode);
                descriptionFunction.ReplaceInCurrentMapping(PrecisionUtil.Token, activeNode.concretePrecision.ToShaderString());

                if(keywordPermutations != null)
                    descriptionFunction.AppendLine("#endif");
            }

            activeNode.CollectShaderProperties(shaderProperties, mode);

            if (activeNode is SubGraphNode subGraphNode)
            {
                subGraphNode.CollectShaderKeywords(shaderKeywords, mode);
            }
        }

        static void GenerateSurfaceDescriptionRemap(
            GraphData graph,
            AbstractMaterialNode rootNode,
            IEnumerable<MaterialSlot> slots,
            ShaderStringBuilder surfaceDescriptionFunction,
            GenerationMode mode)
        {
            if (rootNode is IMasterNode || rootNode is SubGraphOutputNode)
            {
                var usedSlots = slots ?? rootNode.GetInputSlots<MaterialSlot>();
                foreach (var input in usedSlots)
                {
                    if (input != null)
                    {
                        var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                        var hlslName = NodeUtils.GetHLSLSafeName(input.shaderOutputName);
                        if (rootNode is SubGraphOutputNode)
                        {
                            hlslName = $"{hlslName}_{input.id}";
                        }
                        if (foundEdges.Any())
                        {
                            surfaceDescriptionFunction.AppendLine("surface.{0} = {1};",
                                hlslName,
                                rootNode.GetSlotValue(input.id, mode, rootNode.concretePrecision));
                        }
                        else
                        {
                            surfaceDescriptionFunction.AppendLine("surface.{0} = {1};",
                                hlslName, input.GetDefaultValue(mode, rootNode.concretePrecision));
                        }
                    }
                }
            }
            else if (rootNode.hasPreview)
            {
                var slot = rootNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                if (slot != null)
                {
                    var hlslSafeName = $"{NodeUtils.GetHLSLSafeName(slot.shaderOutputName)}_{slot.id}";
                    surfaceDescriptionFunction.AppendLine("surface.{0} = {1};",
                        hlslSafeName, rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision));
                }
            }
        }

        const string k_VertexDescriptionStructName = "VertexDescription";
        public static void GenerateVertexDescriptionStruct(ShaderStringBuilder builder, List<MaterialSlot> slots, string structName = k_VertexDescriptionStructName, IActiveFieldsSet activeFields = null)
        {
            builder.AppendLine("struct {0}", structName);
            using (builder.BlockSemicolonScope())
            {
                foreach (var slot in slots)
                {
                    string hlslName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                    builder.AppendLine("{0} {1};", slot.concreteValueType.ToShaderString(slot.owner.concretePrecision), hlslName);

                    if (activeFields != null)
                    {
                        activeFields.AddAll(structName + "." + hlslName);
                    }
                }
            }
        }

        public static void GenerateVertexDescriptionFunction(
            GraphData graph,
            ShaderStringBuilder builder,
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
            string graphOutputStructName = k_VertexDescriptionStructName)
        {
            if (graph == null)
                return;

            graph.CollectShaderProperties(shaderProperties, mode);

            builder.AppendLine("{0} {1}({2} IN)", graphOutputStructName, functionName, graphInputStructName);
            using (builder.BlockScope())
            {
                builder.AppendLine("{0} description = ({0})0;", graphOutputStructName);
                for(int i = 0; i < nodes.Count; i++)
                {
                    GenerateDescriptionForNode(nodes[i], keywordPermutationsPerNode[i], functionRegistry, builder,
                        shaderProperties, shaderKeywords,
                        graph, mode);
                }

                functionRegistry.builder.currentNode = null;
                builder.currentNode = null;

                if(slots.Count != 0)
                {
                    foreach (var slot in slots)
                    {
                        var isSlotConnected = slot.owner.owner.GetEdges(slot.slotReference).Any();
                        var slotName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                        var slotValue = isSlotConnected ?
                            ((AbstractMaterialNode)slot.owner).GetSlotValue(slot.id, mode, slot.owner.concretePrecision) : slot.GetDefaultValue(mode, slot.owner.concretePrecision);
                        builder.AppendLine("description.{0} = {1};", slotName, slotValue);
                    }
                }

                builder.AppendLine("return description;");
            }
        }

        public static GenerationResults GetPreviewShader(this GraphData graph, AbstractMaterialNode node)
        {
            return graph.GetShader(node, GenerationMode.Preview, String.Format("hidden/preview/{0}", node.GetVariableNameForNode()));
        }
    }
}
