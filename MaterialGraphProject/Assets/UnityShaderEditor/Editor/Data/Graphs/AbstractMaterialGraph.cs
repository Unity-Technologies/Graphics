using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
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

            m_Properties.Add(property);
            m_AddedProperties.Add(property);
        }

        public void RemoveShaderProperty(Guid guid)
        {
            if (m_Properties.RemoveAll(x => x.guid == guid) > 0)
                m_RemovedProperties.Add(guid);
        }

        public override Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            var result = base.GetLegacyTypeRemapping();
            var viewNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.ViewDirectionNode",
                assemblyName = "Assembly-CSharp"
            };
            result[viewNode] = SerializationHelper.GetTypeSerializableAsString(typeof(ViewDirectionNode));

            var normalNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.NormalNode",
                assemblyName = "Assembly-CSharp"
            };
            result[normalNode] = SerializationHelper.GetTypeSerializableAsString(typeof(NormalNode));

            var worldPosNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.WorldPosNode",
                assemblyName = "Assembly-CSharp"
            };
            result[worldPosNode] = SerializationHelper.GetTypeSerializableAsString(typeof(PositionNode));

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
                        RemoveShaderProperty(propertyGuid);
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

        protected static ShaderGraphRequirements GetRequierments(AbstractMaterialNode nodeForRequirements)
        {
            if (nodeForRequirements == null)
                return ShaderGraphRequirements.none;

            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, nodeForRequirements);

            NeededCoordinateSpace requiresNormal = activeNodeList.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal());
            NeededCoordinateSpace requiresBitangent = activeNodeList.OfType<IMayRequireBitangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresBitangent());
            NeededCoordinateSpace requiresTangent = activeNodeList.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresTangent());
            NeededCoordinateSpace requiresViewDir = activeNodeList.OfType<IMayRequireViewDirection>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresViewDirection());
            NeededCoordinateSpace requiresPosition = activeNodeList.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition());
            bool requiresScreenPosition = activeNodeList.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
            bool requiresVertexColor = activeNodeList.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor());

            var meshUV = new List<UVChannel>();
            for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
            {
                var channel = (UVChannel)uvIndex;
                if (activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
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

            ListPool<INode>.Release(activeNodeList);
            return reqs;
        }


        public string GetPreviewShader(AbstractMaterialNode node, out PreviewMode previewMode)
        {
            List<PropertyCollector.TextureInfo> configuredTextures;
            return GetShader(node, GenerationMode.Preview, string.Format("hidden/preview/{0}", node.GetVariableNameForNode()), out configuredTextures, out previewMode);
        }

        protected static void GenerateSurfaceDescriptionStruct(ShaderGenerator surfaceDescriptionStruct, AbstractMaterialNode node, bool isMasterNode)
        {
            surfaceDescriptionStruct.AddShaderChunk("struct SurfaceDescription{", false);
            surfaceDescriptionStruct.Indent();
            if (isMasterNode)
            {
                foreach (var slot in node.GetInputSlots<MaterialSlot>())
                    surfaceDescriptionStruct.AddShaderChunk(string.Format("{0} {1};", AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType), slot.shaderOutputName), false);
            }
            else
            {
                foreach (var slot in node.GetOutputSlots<MaterialSlot>())
                    surfaceDescriptionStruct.AddShaderChunk(string.Format("{0} {1};", AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType), node.GetVariableNameForSlot(slot.id)), false);
            }
            surfaceDescriptionStruct.Deindent();
            surfaceDescriptionStruct.AddShaderChunk("};", false);

            surfaceDescriptionStruct.AddShaderChunk("void ScaleSurfaceDescription(inout SurfaceDescription surface, float scale){", false);
            surfaceDescriptionStruct.Indent();
            if (isMasterNode)
            {
                foreach (var slot in node.GetInputSlots<MaterialSlot>())
                    surfaceDescriptionStruct.AddShaderChunk( string.Format("surface.{0} = scale * surface.{0};", slot.shaderOutputName), false);
            }
            else
            {
                foreach (var slot in node.GetOutputSlots<MaterialSlot>())
                    surfaceDescriptionStruct.AddShaderChunk(string.Format("surface.{0} = scale * surface.{0};", node.GetVariableNameForSlot(slot.id)), false);
            }
            surfaceDescriptionStruct.Deindent();
            surfaceDescriptionStruct.AddShaderChunk("};", false);

            surfaceDescriptionStruct.AddShaderChunk("void AddSurfaceDescription(inout SurfaceDescription base, in SurfaceDescription add){", false);
            surfaceDescriptionStruct.Indent();
            if (isMasterNode)
            {
                foreach (var slot in node.GetInputSlots<MaterialSlot>())
                {
                    var str = string.Format("base.{0} = base.{0} + add.{0};", slot.shaderOutputName);
                    surfaceDescriptionStruct.AddShaderChunk(str, false);
                }
            }
            else
            {
                foreach (var slot in node.GetOutputSlots<MaterialSlot>())
                {
                    var str = string.Format("base.{0} = base.{0} + add.{0};", node.GetVariableNameForSlot(slot.id));
                    surfaceDescriptionStruct.AddShaderChunk(str, false);
                }
            }
            surfaceDescriptionStruct.Deindent();
            surfaceDescriptionStruct.AddShaderChunk("};", false);
        }

        protected static void GenerateSurfaceDescription(
            AbstractMaterialNode node,
            ShaderGenerator surfaceDescriptionFunction,
            ShaderGenerator shaderFunctionVisitor,
            PropertyCollector shaderProperties,
            ShaderGraphRequirements requirements,
            GenerationMode mode,
            bool isMasterNode,
            string functionName = "PopulateSurfaceData",
            string surfaceDescriptionName = "SurfaceDescription")
        {
            var graph = node.owner as AbstractMaterialGraph;
            if (graph == null)
                return;

            surfaceDescriptionFunction.AddShaderChunk(string.Format("{0} {1}(SurfaceInputs IN) {{", surfaceDescriptionName, functionName), false);
            surfaceDescriptionFunction.Indent();

            if ((requirements.requiresNormal & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Object.ToVariableName(InterpolatorType.Normal)), false);
            if ((requirements.requiresNormal & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.View.ToVariableName(InterpolatorType.Normal)), false);
            if ((requirements.requiresNormal & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.World.ToVariableName(InterpolatorType.Normal)), false);
            if ((requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Tangent.ToVariableName(InterpolatorType.Normal)), false);

            if ((requirements.requiresTangent & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Object.ToVariableName(InterpolatorType.Tangent)), false);
            if ((requirements.requiresTangent & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.View.ToVariableName(InterpolatorType.Tangent)), false);
            if ((requirements.requiresTangent & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.World.ToVariableName(InterpolatorType.Tangent)), false);
            if ((requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Tangent.ToVariableName(InterpolatorType.Tangent)), false);

            if ((requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Object.ToVariableName(InterpolatorType.BiTangent)), false);
            if ((requirements.requiresBitangent & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.View.ToVariableName(InterpolatorType.BiTangent)), false);
            if ((requirements.requiresBitangent & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.World.ToVariableName(InterpolatorType.BiTangent)), false);
            if ((requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Tangent.ToVariableName(InterpolatorType.BiTangent)), false);

            if ((requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Object.ToVariableName(InterpolatorType.ViewDirection)), false);
            if ((requirements.requiresViewDir & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.View.ToVariableName(InterpolatorType.ViewDirection)), false);
            if ((requirements.requiresViewDir & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.World.ToVariableName(InterpolatorType.ViewDirection)), false);
            if ((requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Tangent.ToVariableName(InterpolatorType.ViewDirection)), false);

            if ((requirements.requiresPosition & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Object.ToVariableName(InterpolatorType.Position)), false);
            if ((requirements.requiresPosition & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.View.ToVariableName(InterpolatorType.Position)), false);
            if ((requirements.requiresPosition & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.World.ToVariableName(InterpolatorType.Position)), false);
            if ((requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", CoordinateSpace.Tangent.ToVariableName(InterpolatorType.Position)), false);

            if (requirements.requiresScreenPosition)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
            if (requirements.requiresVertexColor)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceDescriptionFunction.AddShaderChunk(string.Format("half4 {0} = IN.{0};", channel.GetUVName()), false);

            graph.CollectShaderProperties(shaderProperties, mode);

            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);
            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (activeNode is IGeneratesFunction)
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(shaderFunctionVisitor, mode);
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(surfaceDescriptionFunction, mode);

                activeNode.CollectShaderProperties(shaderProperties, mode);
            }

            surfaceDescriptionFunction.AddShaderChunk(string.Format("{0} surface = ({0})0;", surfaceDescriptionName), false);
            if (isMasterNode)
            {
                foreach (var input in node.GetInputSlots<MaterialSlot>())
                {
                    var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                    if (foundEdges.Any())
                    {
                        var outputRef = foundEdges[0].outputSlot;
                        var fromNode = graph.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                        surfaceDescriptionFunction.AddShaderChunk(string.Format("surface.{0} = {1};", input.shaderOutputName, fromNode.GetVariableNameForSlot(outputRef.slotId)), true);
                    }
                    else
                    {
                        surfaceDescriptionFunction.AddShaderChunk(string.Format("surface.{0} = {1};", input.shaderOutputName, input.GetDefaultValue(mode)), true);
                    }
                }
            }
            else
            {
                foreach (var slot in node.GetOutputSlots<MaterialSlot>())
                    surfaceDescriptionFunction.AddShaderChunk(string.Format("surface.{0} = {0};", node.GetVariableNameForSlot(slot.id)), true);
            }

            surfaceDescriptionFunction.AddShaderChunk("return surface;", false);
            surfaceDescriptionFunction.Deindent();
            surfaceDescriptionFunction.AddShaderChunk("}", false);
            ListPool<INode>.Release(activeNodeList);
        }

        public string GetShader(AbstractMaterialNode node, GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures, out PreviewMode previewMode)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            var vertexShader = new ShaderGenerator();
            var surfaceDescriptionFunction = new ShaderGenerator();
            var surfaceDescriptionStruct = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var surfaceInputs = new ShaderGenerator();

            var graphVertexInput = @"
struct GraphVertexInput
{
     float4 vertex : POSITION;
     float3 normal : NORMAL;
     float4 tangent : TANGENT;
     float4 texcoord0 : TEXCOORD0;
     float4 lightmapUV : TEXCOORD1;
     UNITY_VERTEX_INPUT_INSTANCE_ID
};";

            surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
            surfaceInputs.Indent();
            var requirements = GetRequierments(node);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceInputs);

            if (requirements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

            if (requirements.requiresScreenPosition)
                surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);
            previewMode = PreviewMode.Preview2D;
            foreach (var pNode in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (pNode.previewMode == PreviewMode.Preview3D)
                {
                    previewMode = PreviewMode.Preview3D;
                    break;
                }
            }
            ListPool<INode>.Release(activeNodeList);

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);

            surfaceInputs.Deindent();
            surfaceInputs.AddShaderChunk("};", false);

            vertexShader.AddShaderChunk("GraphVertexInput PopulateVertexData(GraphVertexInput v){", false);
            vertexShader.Indent();
            vertexShader.AddShaderChunk("return v;", false);
            vertexShader.Deindent();
            vertexShader.AddShaderChunk("}", false);

            GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, node, node is IMasterNode);

            var shaderProperties = new PropertyCollector();
            GenerateSurfaceDescription(
                node,
                surfaceDescriptionFunction,
                shaderFunctionVisitor,
                shaderProperties,
                requirements,
                mode,
                node is IMasterNode);

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
            finalShader.AddShaderChunk(graphVertexInput, false);
            finalShader.AddShaderChunk(surfaceInputs.GetShaderString(2), false);
            finalShader.AddShaderChunk(surfaceDescriptionStruct.GetShaderString(2), false);
            finalShader.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);
            finalShader.AddShaderChunk(vertexShader.GetShaderString(2), false);
            finalShader.AddShaderChunk(surfaceDescriptionFunction.GetShaderString(2), false);
            finalShader.AddShaderChunk("ENDCG", false);

            var masterNode = node as IMasterNode;
            if (masterNode != null)
            {
                var subShaders = masterNode.GetSubshader(requirements, null);
                foreach (var ss in subShaders)
                    finalShader.AddShaderChunk(ss, false);
            }
            else
            {
                finalShader.AddShaderChunk(ShaderGenerator.GetPreviewSubShader(node, requirements), false);
            }

            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);
            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.GetShaderString(0);
        }
    }
}
