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

        public IEnumerable<IShaderProperty> properties
        {
            get { return m_Properties; }
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
            NotifyChange(new ShaderPropertyAdded(property));
        }

        public void RemoveShaderProperty(Guid guid)
        {
            if (m_Properties.RemoveAll(x => x.guid == guid) > 0)
                NotifyChange(new ShaderPropertyRemoved(guid));
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
            result[worldPosNode] = SerializationHelper.GetTypeSerializableAsString(typeof(WorldSpacePositionNode));

            return result;
        }

        class IndexedProperty
        {
            public int index;
            public IShaderProperty property;
        }

        public override void ReplaceWith(IGraph other)
        {
            var otherMG = other as AbstractMaterialGraph;
            if (otherMG != null)
            {
                using (var removedPropertiesPooledObject = ListPool<Guid>.GetDisposable())
                using (var replacedPropertiesPooledObject = ListPool<IndexedProperty>.GetDisposable())
                {
                    var removedPropertyGuids = removedPropertiesPooledObject.value;
                    var replacedProperties = replacedPropertiesPooledObject.value;
                    var index = 0;
                    foreach (var property in m_Properties)
                    {
                        var otherProperty = otherMG.properties.FirstOrDefault(op => op.guid == property.guid);
                        if (otherProperty == null)
                            removedPropertyGuids.Add(property.guid);
                        else
                            replacedProperties.Add(new IndexedProperty { index = index, property = otherProperty });
                        index++;
                    }

                    foreach (var propertyGuid in removedPropertyGuids)
                        RemoveShaderProperty(propertyGuid);

                    foreach (var indexedProperty in replacedProperties)
                    {
                        m_Properties[indexedProperty.index] = indexedProperty.property;
                        // TODO: Notify of change
                    }

                    foreach (var otherProperty in otherMG.properties)
                    {
                        if (!properties.Any(p => p.guid == otherProperty.guid))
                            AddShaderProperty(otherProperty);
                    }
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

        static ShaderGraphRequirements GetRequierments(AbstractMaterialNode nodeForRequirements)
        {
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

        static void GenerateSpaceTranslationSurfaceInputs(
            NeededCoordinateSpace neededSpaces,
            ShaderGenerator surfaceInputs,
            string objectSpaceName,
            string viewSpaceName,
            string worldSpaceName,
            string tangentSpaceName)
        {
            if ((neededSpaces & NeededCoordinateSpace.Object) > 0)
                surfaceInputs.AddShaderChunk(string.Format("float3 {0};", objectSpaceName), false);

            if ((neededSpaces & NeededCoordinateSpace.World) > 0)
                surfaceInputs.AddShaderChunk(string.Format("float3 {0};", worldSpaceName), false);

            if ((neededSpaces & NeededCoordinateSpace.View) > 0)
                surfaceInputs.AddShaderChunk(string.Format("float3 {0};", viewSpaceName), false);

            if ((neededSpaces & NeededCoordinateSpace.Tangent) > 0)
                surfaceInputs.AddShaderChunk(string.Format("float3 {0};", tangentSpaceName), false);
        }

        public string GetPreviewShader(AbstractMaterialNode node, out PreviewMode previewMode)
        {
            List<PropertyCollector.TextureInfo> configuredTextures;
            return GetShader(node, GenerationMode.Preview, string.Format("hidden/preview/{0}", node.GetVariableNameForNode()), out configuredTextures, out previewMode);
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
            GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, surfaceInputs,
                ShaderGeneratorNames.ObjectSpaceNormal, ShaderGeneratorNames.ViewSpaceNormal,
                ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.TangentSpaceNormal);

            GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, surfaceInputs,
                ShaderGeneratorNames.ObjectSpaceTangent, ShaderGeneratorNames.ViewSpaceTangent,
                ShaderGeneratorNames.WorldSpaceTangent, ShaderGeneratorNames.TangentSpaceTangent);

            GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, surfaceInputs,
                ShaderGeneratorNames.ObjectSpaceBiTangent, ShaderGeneratorNames.ViewSpaceBiTangent,
                ShaderGeneratorNames.WorldSpaceBiTangent, ShaderGeneratorNames.TangentSpaceBiTangent);

            GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, surfaceInputs,
                ShaderGeneratorNames.ObjectSpaceViewDirection, ShaderGeneratorNames.ViewSpaceViewDirection,
                ShaderGeneratorNames.WorldSpaceViewDirection, ShaderGeneratorNames.TangentSpaceViewDirection);

            GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, surfaceInputs,
                ShaderGeneratorNames.ObjectSpacePosition, ShaderGeneratorNames.ViewSpacePosition,
                ShaderGeneratorNames.WorldSpacePosition, ShaderGeneratorNames.TangentSpacePosition);

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

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);

            surfaceInputs.Deindent();
            surfaceInputs.AddShaderChunk("};", false);

            vertexShader.AddShaderChunk("GraphVertexInput PopulateVertexData(GraphVertexInput v){", false);
            vertexShader.Indent();
            vertexShader.AddShaderChunk("return v;", false);
            vertexShader.Deindent();
            vertexShader.AddShaderChunk("}", false);

            surfaceDescriptionStruct.AddShaderChunk("struct SurfaceDescription{", false);
            surfaceDescriptionStruct.Indent();
            if (node is IMasterNode)
            {
                foreach (var slot in node.GetInputSlots<MaterialSlot>())
                    surfaceDescriptionStruct.AddShaderChunk(AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType) + " " + slot.shaderOutputName + ";", false);
            }
            else
            {
                foreach (var slot in node.GetOutputSlots<MaterialSlot>())
                    surfaceDescriptionStruct.AddShaderChunk(AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType) + " " + node.GetVariableNameForSlot(slot.id) + ";", false);
            }
            surfaceDescriptionStruct.Deindent();
            surfaceDescriptionStruct.AddShaderChunk("};", false);

            surfaceDescriptionFunction.AddShaderChunk("SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {", false);
            surfaceDescriptionFunction.Indent();

            if ((requirements.requiresNormal & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpaceNormal), false);
            if ((requirements.requiresNormal & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ViewSpaceNormal), false);
            if ((requirements.requiresNormal & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.WorldSpaceNormal), false);
            if ((requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.TangentSpaceNormal), false);

            if ((requirements.requiresTangent & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpaceTangent), false);
            if ((requirements.requiresTangent & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ViewSpaceTangent), false);
            if ((requirements.requiresTangent & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.WorldSpaceTangent), false);
            if ((requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.TangentSpaceTangent), false);

            if ((requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpaceBiTangent), false);
            if ((requirements.requiresBitangent & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ViewSpaceBiTangent), false);
            if ((requirements.requiresBitangent & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.WorldSpaceBiTangent), false);
            if ((requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.TangentSpaceBiTangent), false);

            if ((requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpaceViewDirection), false);
            if ((requirements.requiresViewDir & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ViewSpaceViewDirection), false);
            if ((requirements.requiresViewDir & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.WorldSpaceViewDirection), false);
            if ((requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.TangentSpaceViewDirection), false);

            if ((requirements.requiresPosition & NeededCoordinateSpace.Object) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpacePosition), false);
            if ((requirements.requiresPosition & NeededCoordinateSpace.View) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.ViewSpacePosition), false);
            if ((requirements.requiresPosition & NeededCoordinateSpace.World) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.WorldSpacePosition), false);
            if ((requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float3 {0} = IN.{0};", ShaderGeneratorNames.TangentSpacePosition), false);

            if (requirements.requiresScreenPosition)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
            if (requirements.requiresVertexColor)
                surfaceDescriptionFunction.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceDescriptionFunction.AddShaderChunk(string.Format("half4 {0} = IN.{0};", channel.GetUVName()), false);

            var shaderProperties = new PropertyCollector();
            CollectShaderProperties(shaderProperties, mode);

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (activeNode is IGeneratesFunction)
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(shaderFunctionVisitor, mode);
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(surfaceDescriptionFunction, mode);

                activeNode.CollectShaderProperties(shaderProperties, mode);
            }

            surfaceDescriptionFunction.AddShaderChunk("SurfaceDescription surface = (SurfaceDescription)0;", false);
            if (node is IMasterNode)
            {
                foreach (var input in node.GetInputSlots<MaterialSlot>())
                {
                    var foundEdges = GetEdges(input.slotReference).ToArray();
                    if (foundEdges.Any())
                    {
                        var outputRef = foundEdges[0].outputSlot;
                        var fromNode = GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
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
