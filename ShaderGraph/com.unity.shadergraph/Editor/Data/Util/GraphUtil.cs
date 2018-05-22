using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public static class GraphUtil
    {
        internal static string ConvertCamelCase(string text, bool preserveAcronyms)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                        i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        public static void GenerateApplicationVertexInputs(ShaderGraphRequirements graphRequiements, ShaderStringBuilder vertexInputs)
        {
            vertexInputs.AppendLine("struct GraphVertexInput");
            using (vertexInputs.BlockSemicolonScope())
            {
                vertexInputs.AppendLine("float4 vertex : POSITION;");
                vertexInputs.AppendLine("float3 normal : NORMAL;");
                vertexInputs.AppendLine("float4 tangent : TANGENT;");
                if (graphRequiements.requiresVertexColor)
                {
                    vertexInputs.AppendLine("float4 color : COLOR;");
                }
                foreach (var channel in graphRequiements.requiresMeshUVs.Distinct())
                    vertexInputs.AppendLine("float4 texcoord{0} : TEXCOORD{0};", (int)channel);
                vertexInputs.AppendLine("UNITY_VERTEX_INPUT_INSTANCE_ID");
            }
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
            // ----------------------------------------------------- //
            //                         SETUP                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // String builders

            var finalShader = new ShaderStringBuilder();
            var results = new GenerationResults();
            bool isUber = node == null;
            
            var shaderProperties = new PropertyCollector();
            var functionBuilder = new ShaderStringBuilder();
            var functionRegistry = new FunctionRegistry(functionBuilder);

            var vertexDescriptionFunction = new ShaderStringBuilder(0);    

            var surfaceDescriptionInputStruct = new ShaderStringBuilder(0);
            var surfaceDescriptionStruct = new ShaderStringBuilder(0);
            var surfaceDescriptionFunction = new ShaderStringBuilder(0);

            var vertexInputs = new ShaderStringBuilder(0);        

            // -------------------------------------
            // Get Slot and Node lists

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

            var slots = new List<MaterialSlot>();
            foreach (var activeNode in isUber ? activeNodeList.Where(n => ((AbstractMaterialNode)n).hasPreview) : ((INode)node).ToEnumerable())
            {
                if (activeNode is IMasterNode || activeNode is SubGraphOutputNode)
                    slots.AddRange(activeNode.GetInputSlots<MaterialSlot>());
                else
                    slots.AddRange(activeNode.GetOutputSlots<MaterialSlot>());
            }

            // -------------------------------------
            // Get Requirements

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList, ShaderStageCapability.Fragment);

            // -------------------------------------
            // Add preview shader output property
            
            results.outputIdProperty = new Vector1ShaderProperty
            {
                displayName = "OutputId",
                generatePropertyBlock = false,
                value = -1
            };
            if (isUber)
                shaderProperties.AddShaderProperty(results.outputIdProperty);

            // ----------------------------------------------------- //
            //                START VERTEX DESCRIPTION               //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate Vertex Description function
            
            vertexDescriptionFunction.AppendLine("GraphVertexInput PopulateVertexData(GraphVertexInput v)");
            using(vertexDescriptionFunction.BlockScope())
            {
                vertexDescriptionFunction.AppendLine("return v;");
            }

            // ----------------------------------------------------- //
            //               START SURFACE DESCRIPTION               //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate Input structure for Surface Description function
            // Surface Description Input requirements are needed to exclude intermediate translation spaces
            
            surfaceDescriptionInputStruct.AppendLine("struct SurfaceDescriptionInputs");
            using(surfaceDescriptionInputStruct.BlockSemicolonScope())
            {
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceDescriptionInputStruct);

                if (requirements.requiresVertexColor)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (requirements.requiresScreenPosition)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

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
                    surfaceDescriptionInputStruct.AppendLine("half4 {0};", channel.GetUVName());
            }

            // -------------------------------------
            // Generate Output structure for Surface Description function

            GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, !isUber);

            // -------------------------------------
            // Generate Surface Description function

            GenerateSurfaceDescriptionFunction(
                activeNodeList,
                node,
                graph,
                surfaceDescriptionFunction,
                functionRegistry,
                shaderProperties,
                requirements,
                mode,
                outputIdProperty: results.outputIdProperty);

            // ----------------------------------------------------- //
            //           GENERATE VERTEX > PIXEL PIPELINE            //
            // ----------------------------------------------------- //

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
                finalShader.AppendLine("Properties");
                using (finalShader.BlockScope())
                {
                    finalShader.AppendLines(shaderProperties.GetPropertiesBlock(0));
                }
                finalShader.AppendNewLine();

                finalShader.AppendLine(@"HLSLINCLUDE");
                finalShader.AppendLine("#define USE_LEGACY_UNITY_MATRIX_VARIABLES");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/Common.hlsl""");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/Packing.hlsl""");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/Color.hlsl""");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/UnityInstancing.hlsl""");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/EntityLighting.hlsl""");
                finalShader.AppendLine(@"#include ""ShaderGraphLibrary/ShaderVariables.hlsl""");
                finalShader.AppendLine(@"#include ""ShaderGraphLibrary/ShaderVariablesFunctions.hlsl""");
                finalShader.AppendLine(@"#include ""ShaderGraphLibrary/Functions.hlsl""");
                finalShader.AppendNewLine();

                finalShader.AppendLines(shaderProperties.GetPropertiesDeclaration(0));

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
                ListPool<INode>.Release(activeNodeList);
            }

            // -------------------------------------
            // Finalize

            results.configuredTextures = shaderProperties.GetConfiguredTexutres();
            ShaderSourceMap sourceMap;
            results.shader = finalShader.ToString(out sourceMap);
            results.sourceMap = sourceMap;
            return results;
        }

        public static void GenerateSurfaceDescriptionStruct(ShaderStringBuilder surfaceDescriptionStruct, List<MaterialSlot> slots, bool isMaster)
        {
            surfaceDescriptionStruct.AppendLine("struct SurfaceDescription");
            using(surfaceDescriptionStruct.BlockSemicolonScope())
            {
                if (isMaster)
                {
                    foreach (var slot in slots)
                        surfaceDescriptionStruct.AppendLine("{0} {1};", 
                            NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType), 
                            NodeUtils.GetHLSLSafeName(slot.shaderOutputName));
                    //surfaceDescriptionStruct.Deindent();
                }
                else
                {
                    surfaceDescriptionStruct.AppendLine("float4 PreviewOutput;");
                }
            }
        }

        public static void GenerateSurfaceDescriptionFunction(
            List<INode> activeNodeList,
            AbstractMaterialNode masterNode,
            AbstractMaterialGraph graph,
            ShaderStringBuilder surfaceDescriptionFunction,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            ShaderGraphRequirements requirements,
            GenerationMode mode,
            string functionName = "PopulateSurfaceData",
            string surfaceDescriptionName = "SurfaceDescription",
            Vector1ShaderProperty outputIdProperty = null,
            IEnumerable<MaterialSlot> slots = null)
        {
            if (graph == null)
                return;

            graph.CollectShaderProperties(shaderProperties, mode);

            surfaceDescriptionFunction.AppendLine(String.Format("{0} {1}(SurfaceDescriptionInputs IN)", surfaceDescriptionName, functionName), false);
            using(surfaceDescriptionFunction.BlockScope())
            {
                ShaderGenerator sg = new ShaderGenerator();
                surfaceDescriptionFunction.AppendLine("{0} surface = ({0})0;", surfaceDescriptionName);
                foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                {
                    if (activeNode is IGeneratesFunction)
                    {
                        functionRegistry.builder.currentNode = activeNode;
                        (activeNode as IGeneratesFunction).GenerateNodeFunction(functionRegistry, mode);
                    }
                    if (activeNode is IGeneratesBodyCode)
                        (activeNode as IGeneratesBodyCode).GenerateNodeCode(sg, mode);
                    if (masterNode == null && activeNode.hasPreview)
                    {
                        var outputSlot = activeNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                        if (outputSlot != null)
                            sg.AddShaderChunk(String.Format("if ({0} == {1}) {{ surface.PreviewOutput = {2}; return surface; }}", outputIdProperty.referenceName, activeNode.tempId.index, ShaderGenerator.AdaptNodeOutputForPreview(activeNode, outputSlot.id, activeNode.GetVariableNameForSlot(outputSlot.id))), false);
                    }

                    // In case of the subgraph output node, the preview is generated
                    // from the first input to the node.
                    if (activeNode is SubGraphOutputNode)
                    {
                        var inputSlot = activeNode.GetInputSlots<MaterialSlot>().FirstOrDefault();
                        if (inputSlot != null)
                        {
                            var foundEdges = graph.GetEdges(inputSlot.slotReference).ToArray();
                            string slotValue = foundEdges.Any() ? activeNode.GetSlotValue(inputSlot.id, mode) : inputSlot.GetDefaultValue(mode);
                            sg.AddShaderChunk(String.Format("if ({0} == {1}) {{ surface.PreviewOutput = {2}; return surface; }}", outputIdProperty.referenceName, activeNode.tempId.index, slotValue), false);
                        }
                    }

                    activeNode.CollectShaderProperties(shaderProperties, mode);
                }
                surfaceDescriptionFunction.AppendLines(sg.GetShaderString(0));
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
                                surfaceDescriptionFunction.AppendLine("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(input.shaderOutputName), masterNode.GetSlotValue(input.id, mode));
                            }
                            else
                            {
                                surfaceDescriptionFunction.AppendLine("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(input.shaderOutputName), input.GetDefaultValue(mode));
                            }
                        }
                    }
                    else if (masterNode.hasPreview)
                    {
                        foreach (var slot in masterNode.GetOutputSlots<MaterialSlot>())
                            surfaceDescriptionFunction.AppendLine("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(slot.shaderOutputName), masterNode.GetSlotValue(slot.id, mode));
                    }
                }

                surfaceDescriptionFunction.AppendLine("return surface;");
            }
        }

        const string k_VertexDescriptionStructName = "VertexDescription";
        public static void GenerateVertexDescriptionStruct(ShaderStringBuilder builder, List<MaterialSlot> slots)
        {
            builder.AppendLine("struct {0}", k_VertexDescriptionStructName);
            using (builder.BlockSemicolonScope())
            {
                foreach (var slot in slots)
                    builder.AppendLine("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType), NodeUtils.GetHLSLSafeName(slot.shaderOutputName));
            }
        }
        public static void GenerateVertexDescriptionFunction(
            AbstractMaterialGraph graph,
            ShaderStringBuilder builder,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            GenerationMode mode,
            List<AbstractMaterialNode> nodes,
            List<MaterialSlot> slots)
        {
            if (graph == null)
                return;

            graph.CollectShaderProperties(shaderProperties, mode);

            builder.AppendLine("{0} PopulateVertexData(VertexDescriptionInputs IN)", k_VertexDescriptionStructName);
            using (builder.BlockScope())
            {
                ShaderGenerator sg = new ShaderGenerator();
                builder.AppendLine("{0} description = ({0})0;", k_VertexDescriptionStructName);
                foreach (var node in nodes)
                {
                    var generatesFunction = node as IGeneratesFunction;
                    if (generatesFunction != null)
                    {
                        functionRegistry.builder.currentNode = node;
                        generatesFunction.GenerateNodeFunction(functionRegistry, mode);
                    }
                    var generatesBodyCode = node as IGeneratesBodyCode;
                    if (generatesBodyCode != null)
                    {
                        generatesBodyCode.GenerateNodeCode(sg, mode);               
                    }
                    node.CollectShaderProperties(shaderProperties, mode);
                }
                builder.AppendLines(sg.GetShaderString(0));
                foreach (var slot in slots)
                {
                    var isSlotConnected = slot.owner.owner.GetEdges(slot.slotReference).Any();
                    var slotName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                    var slotValue = isSlotConnected ? ((AbstractMaterialNode)slot.owner).GetSlotValue(slot.id, mode) : slot.GetDefaultValue(mode);
                    builder.AppendLine("description.{0} = {1};", slotName, slotValue);
                }
                builder.AppendLine("return description;");
            }
        }

        public static GenerationResults GetPreviewShader(this AbstractMaterialGraph graph, AbstractMaterialNode node)
        {
            return graph.GetShader(node, GenerationMode.Preview, String.Format("hidden/preview/{0}", node.GetVariableNameForNode()));
        }

        public static GenerationResults GetUberColorShader(this AbstractMaterialGraph graph)
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
                    foreach (var type in assembly.GetTypesOrNothing())
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

        /// <summary>
        /// Sanitizes a supplied string such that it does not collide
        /// with any other name in a collection.
        /// </summary>
        /// <param name="existingNames">
        /// A collection of names that the new name should not collide with.
        /// </param>
        /// <param name="duplicateFormat">
        /// The format applied to the name if a duplicate exists.
        /// This must be a format string that contains `{0}` and `{1}`
        /// once each. An example could be `{0} ({1})`, which will append ` (n)`
        /// to the name for the n`th duplicate.
        /// </param>
        /// <param name="name">
        /// The name to be sanitized.
        /// </param>
        /// <returns>
        /// A name that is distinct form any name in `existingNames`.
        /// </returns>
        internal static string SanitizeName(IEnumerable<string> existingNames, string duplicateFormat, string name)
        {
            if (!existingNames.Contains(name))
                return name;

            string escapedDuplicateFormat = Regex.Escape(duplicateFormat);

            // Escaped format will escape string interpolation, so the escape caracters must be removed for these.
            escapedDuplicateFormat = escapedDuplicateFormat.Replace(@"\{0}", @"{0}");
            escapedDuplicateFormat = escapedDuplicateFormat.Replace(@"\{1}", @"{1}");

            var baseRegex = new Regex(string.Format(escapedDuplicateFormat, @"^(.*)", @"(\d+)"));

            var baseMatch = baseRegex.Match(name);
            if (baseMatch.Success)
                name = baseMatch.Groups[1].Value;

            string baseNameExpression= string.Format(@"^{0}", Regex.Escape(name));
            var regex = new Regex(string.Format(escapedDuplicateFormat, baseNameExpression, @"(\d+)") + "$");

            var existingDuplicateNumbers = existingNames.Select(existingName => regex.Match(existingName)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).Where(n => n > 0).Distinct().ToList();

            var duplicateNumber = 1;
            existingDuplicateNumbers.Sort();
            if (existingDuplicateNumbers.Any() && existingDuplicateNumbers.First() == 1)
            {
                duplicateNumber = existingDuplicateNumbers.Last() + 1;
                for (var i = 1; i < existingDuplicateNumbers.Count; i++)
                {
                    if (existingDuplicateNumbers[i - 1] != existingDuplicateNumbers[i] - 1)
                    {
                        duplicateNumber = existingDuplicateNumbers[i - 1] + 1;
                        break;
                    }
                }
            }

            return string.Format(duplicateFormat, name, duplicateNumber);
        }
    }
}
