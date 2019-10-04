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

        public static void GenerateSurfaceDescriptionStruct(ShaderStringBuilder surfaceDescriptionStruct, List<MaterialSlot> slots, string structName = "SurfaceDescription", IActiveFieldsSet activeFields = null, bool isSubgraph = false)
        {
            surfaceDescriptionStruct.AppendLine("struct {0}", structName);
            using (surfaceDescriptionStruct.BlockSemicolonScope())
            {
                if(slots != null)
                {
                    if (isSubgraph)
                    {
                        var firstSlot = slots.FirstOrDefault();
                        var hlslName = $"{NodeUtils.GetHLSLSafeName(firstSlot.shaderOutputName)}_{firstSlot.id}";
                        surfaceDescriptionStruct.AppendLine("{0} {1};", firstSlot.concreteValueType.ToShaderString(firstSlot.owner.concretePrecision), hlslName);
                        surfaceDescriptionStruct.AppendLine("{0} {1};", ConcreteSlotValueType.Vector4.ToShaderString(firstSlot.owner.concretePrecision), "Out");
                    }
                    else
                    {
                        foreach (var slot in slots)
                        {
                            string hlslName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);

                            surfaceDescriptionStruct.AppendLine("{0} {1};", slot.concreteValueType.ToShaderString(slot.owner.concretePrecision), hlslName);

                            if (activeFields != null)
                            {
                                activeFields.AddAll(structName + "." + hlslName);
                            }
                        }
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
            if (rootNode is IMasterNode)
            {
                var usedSlots = slots ?? rootNode.GetInputSlots<MaterialSlot>();
                foreach (var input in usedSlots)
                {
                    if (input != null)
                    {
                        var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                        var hlslName = NodeUtils.GetHLSLSafeName(input.shaderOutputName);
                        if (foundEdges.Any())
                            surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {rootNode.GetSlotValue(input.id, mode, rootNode.concretePrecision)};");
                        else
                            surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {input.GetDefaultValue(mode, rootNode.concretePrecision)};");
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
                        surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision)};");
                    else
                        surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {slot.GetDefaultValue(mode, rootNode.concretePrecision)};");
                    surfaceDescriptionFunction.AppendLine($"surface.Out = surface.{hlslName};");
                }
            }
            else if (rootNode.hasPreview)
            {
                var slot = rootNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                if (slot != null)
                {
                    var slotValue = rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision);
                    surfaceDescriptionFunction.AppendLine($"surface.Out = all(isfinite({slotValue})) ? {ShaderGenerator.AdaptNodeOutputForPreview(rootNode, slot.id)} : float4(1.0f, 0.0f, 1.0f, 1.0f);");
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
    }
}
