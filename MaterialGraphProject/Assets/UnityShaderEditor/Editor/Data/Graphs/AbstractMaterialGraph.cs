using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class AbstractMaterialGraph : SerializableGraph, IGenerateProperties
    {
        [NonSerialized]
        List<IShaderProperty> m_Properties = new List<IShaderProperty>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        List<IShaderProperty> m_AddedProperties = new List<IShaderProperty>();

        [NonSerialized]
        List<Guid> m_RemovedProperties = new List<Guid>();

        [NonSerialized]
        InspectorPreviewData m_PreviewData = new InspectorPreviewData();

        public InspectorPreviewData previewData
        {
            get { return m_PreviewData; }
            set { m_PreviewData = value; }
        }

        public IEnumerable<IShaderProperty> properties
        {
            get { return m_Properties; }
        }

        public IEnumerable<IShaderProperty> addedProperties
        {
            get { return m_AddedProperties; }
        }

        public IEnumerable<Guid> removedProperties
        {
            get { return m_RemovedProperties; }
        }

        public override void ClearChanges()
        {
            base.ClearChanges();
            m_AddedProperties.Clear();
            m_RemovedProperties.Clear();
        }

        public override void AddNode(INode node)
        {
            if (node is AbstractMaterialNode)
            {
                base.AddNode(node);
            }
            else
            {
                Debug.LogWarningFormat("Trying to add node {0} to Material graph, but it is not a {1}", node, typeof(AbstractMaterialNode));
            }
        }

        public virtual void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            foreach (var prop in properties)
                collector.AddShaderProperty(prop);
        }

        public virtual void AddShaderProperty(IShaderProperty property)
        {
            if (property == null)
                return;

            if (m_Properties.Contains(property))
                return;

            property.displayName = property.displayName.Trim();
            if (m_Properties.Any(p => p.displayName == property.displayName))
            {
                var regex = new Regex(@"^" + Regex.Escape(property.displayName) + @" \((\d+)\)$");
                var existingDuplicateNumbers = m_Properties.Select(p => regex.Match(p.displayName)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).Where(n => n > 0).ToList();

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
                property.displayName = string.Format("{0} ({1})", property.displayName, duplicateNumber);
            }

            m_Properties.Add(property);
            m_AddedProperties.Add(property);
        }

        public void RemoveShaderProperty(Guid guid)
        {
            var propertyNodes = GetNodes<PropertyNode>().Where(x => x.propertyGuid == guid).ToList();
            foreach (var propNode in propertyNodes)
                ReplacePropertyNodeWithConcreteNode(propNode);

            RemoveShaderPropertyNoValidate(guid);

            ValidateGraph();
        }

        void RemoveShaderPropertyNoValidate(Guid guid)
        {
            if (m_Properties.RemoveAll(x => x.guid == guid) > 0)
                m_RemovedProperties.Add(guid);
        }

        static List<IEdge> s_TempEdges = new List<IEdge>();

        public void ReplacePropertyNodeWithConcreteNode(PropertyNode propertyNode)
        {
            var property = properties.FirstOrDefault(x => x.guid == propertyNode.propertyGuid);
            if (property != null)
            {
                AbstractMaterialNode node = null;
                int slotId = -1;
                if (property is FloatShaderProperty)
                {
                    var createdNode = new Vector1Node();
                    createdNode.value = ((FloatShaderProperty)property).value;
                    slotId = Vector1Node.OutputSlotId;
                    node = createdNode;
                }
                else if (property is Vector2ShaderProperty)
                {
                    var createdNode = new Vector2Node();
                    createdNode.value = ((Vector2ShaderProperty)property).value;
                    slotId = Vector2Node.OutputSlotId;
                    node = createdNode;
                }
                else if (property is Vector3ShaderProperty)
                {
                    var createdNode = new Vector3Node();
                    createdNode.value = ((Vector3ShaderProperty)property).value;
                    slotId = Vector3Node.OutputSlotId;
                    node = createdNode;
                }
                else if (property is Vector4ShaderProperty)
                {
                    var createdNode = new Vector4Node();
                    createdNode.value = ((Vector4ShaderProperty)property).value;
                    slotId = Vector4Node.OutputSlotId;
                    node = createdNode;
                }
                else if (property is ColorShaderProperty)
                {
                    var createdNode = new ColorNode();
                    createdNode.color = ((ColorShaderProperty)property).value;
                    slotId = ColorNode.OutputSlotId;
                    node = createdNode;
                }
                else if (property is TextureShaderProperty)
                {
                    var createdNode = new Texture2DAssetNode();
                    createdNode.texture = ((TextureShaderProperty)property).value.texture;
                    slotId = Texture2DAssetNode.OutputSlotId;
                    node = createdNode;
                }
                else if (property is CubemapShaderProperty)
                {
                    var createdNode = new CubemapAssetNode();
                    createdNode.cubemap = ((CubemapShaderProperty)property).value.cubemap;
                    slotId = CubemapAssetNode.OutputSlotId;
                    node = createdNode;
                }

                if (node == null)
                    return;

                var slot = propertyNode.FindOutputSlot<MaterialSlot>(PropertyNode.OutputSlotId);
                node.drawState = propertyNode.drawState;
                AddNodeNoValidate(node);

                s_TempEdges.Clear();
                GetEdges(slot.slotReference, s_TempEdges);
                foreach (var edge in s_TempEdges)
                    ConnectNoValidate(node.GetSlotReference(slotId), edge.inputSlot);

                RemoveNodeNoValidate(propertyNode);
            }
        }

        public override void ValidateGraph()
        {
            var propertyNodes = GetNodes<PropertyNode>().Where(n => !m_Properties.Any(p => p.guid == n.propertyGuid)).ToArray();
            foreach (var pNode in propertyNodes)
                ReplacePropertyNodeWithConcreteNode(pNode);
            base.ValidateGraph();
        }

        public override Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            var result = base.GetLegacyTypeRemapping();
            var viewNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.ViewDirectionNode"
            };
            result[viewNode] = SerializationHelper.GetTypeSerializableAsString(typeof(ViewDirectionNode));

            var normalNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.NormalNode"
            };
            result[normalNode] = SerializationHelper.GetTypeSerializableAsString(typeof(NormalVectorNode));

            var worldPosNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.WorldPosNode"
            };
            result[worldPosNode] = SerializationHelper.GetTypeSerializableAsString(typeof(PositionNode));

            var sampleTexture2DNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEditor.ShaderGraph.Texture2DNode"
            };
            result[sampleTexture2DNode] = SerializationHelper.GetTypeSerializableAsString(typeof(SampleTexture2DNode));

            var sampleCubemapNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEditor.ShaderGraph.CubemapNode"
            };
            result[sampleCubemapNode] = SerializationHelper.GetTypeSerializableAsString(typeof(SampleCubemapNode));

            return result;
        }

        public override void ReplaceWith(IGraph other)
        {
            var otherMG = other as AbstractMaterialGraph;
            if (otherMG != null)
            {
                using (var removedPropertiesPooledObject = ListPool<Guid>.GetDisposable())
                {
                    var removedPropertyGuids = removedPropertiesPooledObject.value;
                    foreach (var property in m_Properties)
                        removedPropertyGuids.Add(property.guid);
                    foreach (var propertyGuid in removedPropertyGuids)
                        RemoveShaderPropertyNoValidate(propertyGuid);
                }
                foreach (var otherProperty in otherMG.properties)
                {
                    if (!properties.Any(p => p.guid == otherProperty.guid))
                        AddShaderProperty(otherProperty);
                }
            }
            base.ReplaceWith(other);
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializedProperties = SerializationHelper.Serialize<IShaderProperty>(m_Properties);
        }

        public override void OnAfterDeserialize()
        {
            // have to deserialize 'globals' before nodes
            m_Properties = SerializationHelper.Deserialize<IShaderProperty>(m_SerializedProperties, null);
            base.OnAfterDeserialize();
        }

        internal static ShaderGraphRequirements GetRequirements(List<INode> nodes)
        {
            NeededCoordinateSpace requiresNormal = nodes.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal());
            NeededCoordinateSpace requiresBitangent = nodes.OfType<IMayRequireBitangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresBitangent());
            NeededCoordinateSpace requiresTangent = nodes.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresTangent());
            NeededCoordinateSpace requiresViewDir = nodes.OfType<IMayRequireViewDirection>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresViewDirection());
            NeededCoordinateSpace requiresPosition = nodes.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition());
            bool requiresScreenPosition = nodes.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
            bool requiresVertexColor = nodes.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor());

            var meshUV = new List<UVChannel>();
            for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
            {
                var channel = (UVChannel)uvIndex;
                if (nodes.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
                    meshUV.Add(channel);
            }

            // if anything needs tangentspace we have make
            // sure to have our othonormal basis!
            var compoundSpaces = requiresBitangent | requiresNormal | requiresPosition
                | requiresTangent | requiresViewDir | requiresPosition
                | requiresNormal;

            var needsTangentSpace = (compoundSpaces & NeededCoordinateSpace.Tangent) > 0;
            if (needsTangentSpace)
            {
                requiresBitangent |= NeededCoordinateSpace.Object;
                requiresNormal |= NeededCoordinateSpace.Object;
                requiresTangent |= NeededCoordinateSpace.Object;
            }

            var reqs = new ShaderGraphRequirements()
            {
                requiresNormal = requiresNormal,
                requiresBitangent = requiresBitangent,
                requiresTangent = requiresTangent,
                requiresViewDir = requiresViewDir,
                requiresPosition = requiresPosition,
                requiresScreenPosition = requiresScreenPosition,
                requiresVertexColor = requiresVertexColor,
                requiresMeshUVs = meshUV
            };

            return reqs;
        }

        public string GetPreviewShader(AbstractMaterialNode node, out PreviewMode previewMode)
        {
            List<PropertyCollector.TextureInfo> configuredTextures;
            FloatShaderProperty outputIdProperty;
            return GetShader(node, GenerationMode.Preview, string.Format("hidden/preview/{0}", node.GetVariableNameForNode()), out configuredTextures, out previewMode, out outputIdProperty);
        }

        public string GetUberPreviewShader(Dictionary<Guid, int> ids, out FloatShaderProperty outputIdProperty)
        {
            List<PropertyCollector.TextureInfo> configuredTextures;
            PreviewMode previewMode;
            return GetShader(null, GenerationMode.Preview, "hidden/preview", out configuredTextures, out previewMode, out outputIdProperty, ids);
        }

        internal static void GenerateSurfaceDescriptionStruct(ShaderGenerator surfaceDescriptionStruct, List<MaterialSlot> slots, bool isMaster)
        {
            surfaceDescriptionStruct.AddShaderChunk("struct SurfaceDescription{", false);
            surfaceDescriptionStruct.Indent();
            if (isMaster)
            {
                foreach (var slot in slots)
                    surfaceDescriptionStruct.AddShaderChunk(string.Format("{0} {1};", AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType), AbstractMaterialNode.GetHLSLSafeName(slot.shaderOutputName)), false);
                surfaceDescriptionStruct.Deindent();
            }
            else
            {
                surfaceDescriptionStruct.AddShaderChunk("float4 PreviewOutput;", false);
            }
            surfaceDescriptionStruct.Deindent();
            surfaceDescriptionStruct.AddShaderChunk("};", false);
        }

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
                vertexInputs.AddShaderChunk(string.Format("float4 texcoord{0} : TEXCOORD{1};", ((int)channel).ToString(), vertexInputIndex.ToString()), false);
                vertexInputIndex++;
            }

            vertexInputs.AddShaderChunk("UNITY_VERTEX_INPUT_INSTANCE_ID", false);
            vertexInputs.Deindent();
            vertexInputs.AddShaderChunk("};", false);
        }

        internal static void GenerateSurfaceDescription(
            List<INode> activeNodeList,
            AbstractMaterialNode masterNode,
            AbstractMaterialGraph graph,
            ShaderGenerator surfaceDescriptionFunction,
            ShaderGenerator shaderFunctionVisitor,
            PropertyCollector shaderProperties,
            ShaderGraphRequirements requirements,
            GenerationMode mode,
            string functionName = "PopulateSurfaceData",
            string surfaceDescriptionName = "SurfaceDescription",
            FloatShaderProperty outputIdProperty = null,
            Dictionary<Guid, int> ids = null,
            IEnumerable<MaterialSlot> slots = null)
        {
            if (graph == null)
                return;

            surfaceDescriptionFunction.AddShaderChunk(string.Format("{0} {1}(SurfaceInputs IN) {{", surfaceDescriptionName, functionName), false);
            surfaceDescriptionFunction.Indent();
            surfaceDescriptionFunction.AddShaderChunk(string.Format("{0} surface = ({0})0;", surfaceDescriptionName), false);

            foreach (CoordinateSpace space in Enum.GetValues(typeof(CoordinateSpace)))
            {
                var neededCoordinateSpace = space.ToNeededCoordinateSpace();
                if ((requirements.requiresNormal & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.Normal)), false);
                if ((requirements.requiresTangent & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.Tangent)), false);
                if ((requirements.requiresBitangent & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.BiTangent)), false);
                if ((requirements.requiresViewDir & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.ViewDirection)), false);
                if ((requirements.requiresPosition & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.Position)), false);
            }

            if (requirements.requiresScreenPosition)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
            if (requirements.requiresVertexColor)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceDescriptionFunction.AddShaderChunk(string.Format("half4 {0} = IN.{0};", channel.GetUVName()), false);

            graph.CollectShaderProperties(shaderProperties, mode);

            var currentId = -1;
            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (activeNode is IGeneratesFunction)
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(shaderFunctionVisitor, mode);
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(surfaceDescriptionFunction, mode);
                if (masterNode == null && activeNode.hasPreview)
                {
                    var outputSlot = activeNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                    if (outputSlot != null)
                    {
                        currentId++;
                        ids[activeNode.guid] = currentId;
                        surfaceDescriptionFunction.AddShaderChunk(string.Format("if ({0} == {1}) {{ surface.PreviewOutput = {2}; return surface; }}", outputIdProperty.referenceName, currentId, ShaderGenerator.AdaptNodeOutputForPreview(activeNode, outputSlot.id, activeNode.GetVariableNameForSlot(outputSlot.id))), false);
                    }
                }

                activeNode.CollectShaderProperties(shaderProperties, mode);
            }

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
                            surfaceDescriptionFunction.AddShaderChunk(string.Format("surface.{0} = {1};", AbstractMaterialNode.GetHLSLSafeName(input.shaderOutputName), fromNode.GetVariableNameForSlot(outputRef.slotId)), true);
                        }
                        else
                        {
                            surfaceDescriptionFunction.AddShaderChunk(string.Format("surface.{0} = {1};", AbstractMaterialNode.GetHLSLSafeName(input.shaderOutputName), input.GetDefaultValue(mode)), true);
                        }
                    }
                }
                else if (masterNode.hasPreview)
                {
                    foreach (var slot in masterNode.GetOutputSlots<MaterialSlot>())
                        surfaceDescriptionFunction.AddShaderChunk(string.Format("surface.{0} = {1};", AbstractMaterialNode.GetHLSLSafeName(slot.shaderOutputName), masterNode.GetVariableNameForSlot(slot.id)), true);
                }
            }

            surfaceDescriptionFunction.AddShaderChunk("return surface;", false);
            surfaceDescriptionFunction.Deindent();
            surfaceDescriptionFunction.AddShaderChunk("}", false);
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

        public string GetShader(AbstractMaterialNode node, GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures, out PreviewMode previewMode, out FloatShaderProperty outputIdProperty, Dictionary<Guid, int> ids = null)
        {
            bool isUber = node == null;

            var vertexInputs = new ShaderGenerator();
            var vertexShader = new ShaderGenerator();
            var surfaceDescriptionFunction = new ShaderGenerator();
            var surfaceDescriptionStruct = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var surfaceInputs = new ShaderGenerator();

            surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
            surfaceInputs.Indent();

            var activeNodeList = ListPool<INode>.Get();
            if (isUber)
            {
                var unmarkedNodes = GetNodes<INode>().Where(x => !(x is IMasterNode)).ToDictionary(x => x.guid);
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

            var requirements = GetRequirements(activeNodeList);
            GenerateApplicationVertexInputs(requirements, vertexInputs, 0, 8);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceInputs);

            if (requirements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

            if (requirements.requiresScreenPosition)
                surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

            previewMode = PreviewMode.Preview3D;
            if (!isUber)
            {
                foreach (var pNode in activeNodeList.OfType<AbstractMaterialNode>())
                {
                    if (pNode.previewMode == PreviewMode.Preview3D)
                    {
                        previewMode = PreviewMode.Preview3D;
                        break;
                    }
                }
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);

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
            outputIdProperty = new FloatShaderProperty
            {
                displayName = "OutputId",
                generatePropertyBlock = false,
                value = -1
            };
            if (isUber)
                shaderProperties.AddShaderProperty(outputIdProperty);

            GenerateSurfaceDescription(
                activeNodeList,
                node,
                this,
                surfaceDescriptionFunction,
                shaderFunctionVisitor,
                shaderProperties,
                requirements,
                mode,
                outputIdProperty: outputIdProperty,
                ids: ids);

            var finalShader = new ShaderGenerator();
            finalShader.AddShaderChunk(string.Format(@"Shader ""{0}""", name), false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();

            finalShader.AddShaderChunk("Properties", false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();
            finalShader.AddShaderChunk(shaderProperties.GetPropertiesBlock(2), false);
            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            finalShader.AddShaderChunk("CGINCLUDE", false);
            finalShader.AddShaderChunk("#include \"UnityCG.cginc\"", false);
            finalShader.AddShaderChunk(shaderFunctionVisitor.GetShaderString(2), false);
            finalShader.AddShaderChunk(vertexInputs.GetShaderString(2), false);
            finalShader.AddShaderChunk(surfaceInputs.GetShaderString(2), false);
            finalShader.AddShaderChunk(surfaceDescriptionStruct.GetShaderString(2), false);
            finalShader.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);
            finalShader.AddShaderChunk(vertexShader.GetShaderString(2), false);
            finalShader.AddShaderChunk(surfaceDescriptionFunction.GetShaderString(2), false);
            finalShader.AddShaderChunk("ENDCG", false);

            finalShader.AddShaderChunk(ShaderGenerator.GetPreviewSubShader(node, requirements), false);

            ListPool<INode>.Release(activeNodeList);

            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);
            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.GetShaderString(0);
        }
    }


    public class InspectorPreviewData
    {
        public Mesh mesh;
        public Quaternion rotation = Quaternion.identity;
        public float scale = 1f;
    }
}
