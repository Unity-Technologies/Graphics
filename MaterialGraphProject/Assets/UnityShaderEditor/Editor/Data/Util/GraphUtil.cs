using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class GraphUtil
    {
        internal static void GenerateApplicationVertexInputs(ShaderGraphRequirements graphRequiements, ShaderGenerator vertexInputs, int vertexInputStartIndex, int maxVertexInputs)
        {
            int vertexInputIndex = vertexInputStartIndex;

            vertexInputs.AddShaderChunk("struct GraphVertexInput", false);
            vertexInputs.AddShaderChunk("{", false);
            vertexInputs.Indent();
            vertexInputs.AddShaderChunk("float4 vertex : POSITION;", false);
            vertexInputs.AddShaderChunk("float3 normal : NORMAL;", false);
            vertexInputs.AddShaderChunk("float4 tangent : TANGENT;", false);

            if (graphRequiements.requiresVertexColor)
            {
                vertexInputs.AddShaderChunk("float4 color : COLOR;", false);
            }

            foreach (var channel in graphRequiements.requiresMeshUVs.Distinct())
            {
                vertexInputs.AddShaderChunk(String.Format("float4 texcoord{0} : TEXCOORD{1};", ((int)channel).ToString(), vertexInputIndex.ToString()), false);
                vertexInputIndex++;
            }

            vertexInputs.AddShaderChunk("UNITY_VERTEX_INPUT_INSTANCE_ID", false);
            vertexInputs.Deindent();
            vertexInputs.AddShaderChunk("};", false);
        }

        static void Visit(List<INode> outputList, Dictionary<Guid, INode> unmarkedNodes, INode node)
        {
            if (!unmarkedNodes.ContainsKey(node.guid))
                return;
            foreach (var slot in node.GetInputSlots<ISlot>())
            {
                foreach (var edge in node.owner.GetEdges(slot.slotReference))
                {
                    var inputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    Visit(outputList, unmarkedNodes, inputNode);
                }
            }
            unmarkedNodes.Remove(node.guid);
            outputList.Add(node);
        }

        public static GenerationResults GetShader(this AbstractMaterialGraph graph, AbstractMaterialNode node, GenerationMode mode, string name)
        {
            var results = new GenerationResults();
            bool isUber = node == null;

            var vertexInputs = new ShaderGenerator();
            var vertexShader = new ShaderGenerator();
            var surfaceDescriptionFunction = new ShaderGenerator();
            var surfaceDescriptionStruct = new ShaderGenerator();
            var functionBuilder = new ShaderStringBuilder();
            var functionRegistry = new FunctionRegistry(functionBuilder);
            var surfaceInputs = new ShaderGenerator();

            surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
            surfaceInputs.Indent();

            var activeNodeList = ListPool<INode>.Get();
            if (isUber)
            {
                var unmarkedNodes = graph.GetNodes<INode>().Where(x => !(x is IMasterNode)).ToDictionary(x => x.guid);
                while (unmarkedNodes.Any())
                {
                    var unmarkedNode = unmarkedNodes.FirstOrDefault();
                    Visit(activeNodeList, unmarkedNodes, unmarkedNode.Value);
                }
            }
            else
            {
                NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);
            }

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList);
            GenerateApplicationVertexInputs(requirements, vertexInputs, 0, 8);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceInputs);

            if (requirements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(String.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

            if (requirements.requiresScreenPosition)
                surfaceInputs.AddShaderChunk(String.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

            results.previewMode = PreviewMode.Preview3D;
            if (!isUber)
            {
                foreach (var pNode in activeNodeList.OfType<AbstractMaterialNode>())
                {
                    if (pNode.previewMode == PreviewMode.Preview3D)
                    {
                        results.previewMode = PreviewMode.Preview3D;
                        break;
                    }
                }
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(String.Format("half4 {0};", channel.GetUVName()), false);

            surfaceInputs.Deindent();
            surfaceInputs.AddShaderChunk("};", false);

            vertexShader.AddShaderChunk("GraphVertexInput PopulateVertexData(GraphVertexInput v){", false);
            vertexShader.Indent();
            vertexShader.AddShaderChunk("return v;", false);
            vertexShader.Deindent();
            vertexShader.AddShaderChunk("}", false);

            var slots = new List<MaterialSlot>();
            foreach (var activeNode in isUber ? activeNodeList.Where(n => ((AbstractMaterialNode)n).hasPreview) : ((INode)node).ToEnumerable())
            {
                if (activeNode is IMasterNode)
                    slots.AddRange(activeNode.GetInputSlots<MaterialSlot>());
                else
                    slots.AddRange(activeNode.GetOutputSlots<MaterialSlot>());
            }
            GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, !isUber);

            var shaderProperties = new PropertyCollector();
            results.outputIdProperty = new FloatShaderProperty
            {
                displayName = "OutputId",
                generatePropertyBlock = false,
                value = -1
            };
            if (isUber)
                shaderProperties.AddShaderProperty(results.outputIdProperty);

            GenerateSurfaceDescription(
                activeNodeList,
                node,
                graph,
                surfaceDescriptionFunction,
                functionRegistry,
                shaderProperties,
                requirements,
                mode,
                outputIdProperty: results.outputIdProperty);

            var finalBuilder = new ShaderStringBuilder();
            finalBuilder.AppendLine(@"Shader ""{0}""", name);
            using (finalBuilder.BlockScope())
            {
                finalBuilder.AppendLine("Properties");
                using (finalBuilder.BlockScope())
                {
                    finalBuilder.AppendLines(shaderProperties.GetPropertiesBlock(0));
                }

                finalBuilder.AppendLine(@"CGINCLUDE");
                finalBuilder.AppendLine(@"#include ""UnityCG.cginc""");
                finalBuilder.Concat(functionBuilder);
                finalBuilder.AppendLines(vertexInputs.GetShaderString(0));
                finalBuilder.AppendLines(surfaceInputs.GetShaderString(0));
                finalBuilder.AppendLines(surfaceDescriptionStruct.GetShaderString(0));
                finalBuilder.AppendLines(shaderProperties.GetPropertiesDeclaration(0));
                finalBuilder.AppendLines(vertexShader.GetShaderString(0));
                finalBuilder.AppendLines(surfaceDescriptionFunction.GetShaderString(0));
                finalBuilder.AppendLine(@"ENDCG");

                finalBuilder.AppendLines(ShaderGenerator.GetPreviewSubShader(node, requirements));
                ListPool<INode>.Release(activeNodeList);
            }

            results.configuredTextures = shaderProperties.GetConfiguredTexutres();
            ShaderSourceMap sourceMap;
            results.shader = finalBuilder.ToString(out sourceMap);
            results.sourceMap = sourceMap;
            return results;
        }

        internal static void GenerateSurfaceDescriptionStruct(ShaderGenerator surfaceDescriptionStruct, List<MaterialSlot> slots, bool isMaster)
        {
            surfaceDescriptionStruct.AddShaderChunk("struct SurfaceDescription{", false);
            surfaceDescriptionStruct.Indent();
            if (isMaster)
            {
                foreach (var slot in slots)
                    surfaceDescriptionStruct.AddShaderChunk(String.Format("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType), NodeUtils.GetHLSLSafeName(slot.shaderOutputName)), false);
                surfaceDescriptionStruct.Deindent();
            }
            else
            {
                surfaceDescriptionStruct.AddShaderChunk("float4 PreviewOutput;", false);
            }
            surfaceDescriptionStruct.Deindent();
            surfaceDescriptionStruct.AddShaderChunk("};", false);
        }

        internal static void GenerateSurfaceDescription(
            List<INode> activeNodeList,
            AbstractMaterialNode masterNode,
            AbstractMaterialGraph graph,
            ShaderGenerator surfaceDescriptionFunction,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            ShaderGraphRequirements requirements,
            GenerationMode mode,
            string functionName = "PopulateSurfaceData",
            string surfaceDescriptionName = "SurfaceDescription",
            FloatShaderProperty outputIdProperty = null,
            IEnumerable<MaterialSlot> slots = null)
        {
            if (graph == null)
                return;

            surfaceDescriptionFunction.AddShaderChunk(String.Format("{0} {1}(SurfaceInputs IN) {{", surfaceDescriptionName, functionName), false);
            surfaceDescriptionFunction.Indent();
            surfaceDescriptionFunction.AddShaderChunk(String.Format("{0} surface = ({0})0;", surfaceDescriptionName), false);

            foreach (CoordinateSpace space in Enum.GetValues(typeof(CoordinateSpace)))
            {
                var neededCoordinateSpace = space.ToNeededCoordinateSpace();
                if ((requirements.requiresNormal & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.Normal)), false);
                if ((requirements.requiresTangent & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.Tangent)), false);
                if ((requirements.requiresBitangent & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.BiTangent)), false);
                if ((requirements.requiresViewDir & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.ViewDirection)), false);
                if ((requirements.requiresPosition & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.Position)), false);
            }

            if (requirements.requiresScreenPosition)
                surfaceDescriptionFunction.AddShaderChunk(String.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
            if (requirements.requiresVertexColor)
                surfaceDescriptionFunction.AddShaderChunk(String.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceDescriptionFunction.AddShaderChunk(String.Format("half4 {0} = IN.{0};", channel.GetUVName()), false);

            graph.CollectShaderProperties(shaderProperties, mode);

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (activeNode is IGeneratesFunction)
                {
                    functionRegistry.builder.currentNode = activeNode;
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(functionRegistry, mode);
                }
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(surfaceDescriptionFunction, mode);
                if (masterNode == null && activeNode.hasPreview)
                {
                    var outputSlot = activeNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                    if (outputSlot != null)
                        surfaceDescriptionFunction.AddShaderChunk(String.Format("if ({0} == {1}) {{ surface.PreviewOutput = {2}; return surface; }}", outputIdProperty.referenceName, activeNode.tempId.index, ShaderGenerator.AdaptNodeOutputForPreview(activeNode, outputSlot.id, activeNode.GetVariableNameForSlot(outputSlot.id))), false);
                }

                activeNode.CollectShaderProperties(shaderProperties, mode);
            }
            functionRegistry.builder.currentNode = null;

            if (masterNode != null)
            {
                if (masterNode is IMasterNode)
                {
                    var usedSlots = slots ?? masterNode.GetInputSlots<MaterialSlot>();
                    foreach (var input in usedSlots)
                    {
                        var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                        if (foundEdges.Any())
                        {
                            var outputRef = foundEdges[0].outputSlot;
                            var fromNode = graph.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                            surfaceDescriptionFunction.AddShaderChunk(String.Format("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(input.shaderOutputName), fromNode.GetVariableNameForSlot(outputRef.slotId)), true);
                        }
                        else
                        {
                            surfaceDescriptionFunction.AddShaderChunk(String.Format("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(input.shaderOutputName), input.GetDefaultValue(mode)), true);
                        }
                    }
                }
                else if (masterNode.hasPreview)
                {
                    foreach (var slot in masterNode.GetOutputSlots<MaterialSlot>())
                        surfaceDescriptionFunction.AddShaderChunk(String.Format("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(slot.shaderOutputName), masterNode.GetVariableNameForSlot(slot.id)), true);
                }
            }

            surfaceDescriptionFunction.AddShaderChunk("return surface;", false);
            surfaceDescriptionFunction.Deindent();
            surfaceDescriptionFunction.AddShaderChunk("}", false);
        }

        public static GenerationResults GetPreviewShader(this AbstractMaterialGraph graph, AbstractMaterialNode node)
        {
            return graph.GetShader(node, GenerationMode.Preview, String.Format("hidden/preview/{0}", node.GetVariableNameForNode()));
        }

        public static GenerationResults GetUberPreviewShader(this AbstractMaterialGraph graph)
        {
            return graph.GetShader(null, GenerationMode.Preview, "hidden/preview");
        }

        static Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> s_LegacyTypeRemapping;

        public static Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            if (s_LegacyTypeRemapping == null)
            {
                s_LegacyTypeRemapping = new Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract)
                            continue;
                        foreach (var attribute in type.GetCustomAttributes(typeof(FormerNameAttribute), false))
                        {
                            var legacyAttribute = (FormerNameAttribute)attribute;
                            var serializationInfo = new SerializationHelper.TypeSerializationInfo { fullName = legacyAttribute.fullName };
                            s_LegacyTypeRemapping[serializationInfo] = SerializationHelper.GetTypeSerializableAsString(type);
                        }
                    }
                }
            }

            return s_LegacyTypeRemapping;
        }
    }
}
