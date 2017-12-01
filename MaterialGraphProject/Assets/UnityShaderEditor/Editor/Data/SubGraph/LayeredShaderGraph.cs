using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class LayeredShaderGraph : AbstractMaterialGraph, IShaderGraph
    {
        [Serializable]
        public class Layer
        {
            [SerializeField]
            private SerializableGuid m_Guid = new SerializableGuid();

            [SerializeField]
            private Shader m_Shader;

            public Layer() {}

            public Guid guid
            {
                get { return m_Guid.guid; }
            }

            public Shader shader
            {
                get { return m_Shader; }
                set { m_Shader = value; }
            }
        }

        [NonSerialized]
        private List<Layer> m_Layers = new List<Layer>();

        [NonSerialized]
        List<Layer> m_AddedLayers = new List<Layer>();

        [NonSerialized]
        List<Guid> m_RemovedLayers = new List<Guid>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedLayers = new List<SerializationHelper.JSONSerializedElement>();

        public IEnumerable<Layer> layers
        {
            get { return m_Layers; }
        }

        public List<Layer> addedLayers
        {
            get { return m_AddedLayers; }
        }

        public List<Guid> removedLayers
        {
            get { return m_RemovedLayers; }
        }

        [NonSerialized]
        private LayerWeightsOutputNode m_OutputNode;

        public LayerWeightsOutputNode outputNode
        {
            get
            {
                // find existing node
                if (m_OutputNode == null)
                    m_OutputNode = GetNodes<LayerWeightsOutputNode>().FirstOrDefault();

                return m_OutputNode;
            }
        }

        public override void ClearChanges()
        {
            base.ClearChanges();
            m_AddedLayers.Clear();
            m_RemovedLayers.Clear();
        }

        public override void AddNode(INode node)
        {
            if (outputNode != null && node is LayerWeightsOutputNode)
            {
                Debug.LogWarning("Attempting to add second LayerWeightsOutputNode to LayeredShaderGraph. This is not allowed.");
                return;
            }

            base.AddNode(node);
        }

        public void AddLayer()
        {
            var layer = new Layer();
            m_Layers.Add(layer);
            m_AddedLayers.Add(layer);

            if (outputNode != null)
                outputNode.onModified(outputNode, ModificationScope.Graph);
        }

        public bool SetLayer(Guid layerId, Shader newShader)
        {
            try
            {
                var path = AssetDatabase.GetAssetPath(newShader);

                if (!path.EndsWith("shaderGraph", StringComparison.InvariantCultureIgnoreCase))
                    return false;

                var textGraph = File.ReadAllText(path, Encoding.UTF8);
                var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);
                if (graph == null)
                    return false;

                var layer = layers.FirstOrDefault(x => x.guid == layerId);
                if (layer == null)
                    return false;

                layer.shader = newShader;

                if (outputNode != null)
                {
                    outputNode.OnEnable();
                    outputNode.onModified(outputNode, ModificationScope.Graph);
                }

                return true;
            }
            catch (Exception)
            {
                // ignored
            }
            return false;
        }

        public void RemoveLayer(Guid id)
        {
            var num = m_Layers.RemoveAll(x => x.guid == id);

            if (num > 0)
            {
                m_RemovedLayers.Add(id);

                if (outputNode != null)
                    outputNode.onModified(outputNode, ModificationScope.Graph);
            }
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializedLayers = SerializationHelper.Serialize<Layer>(m_Layers);
        }

        public override void OnAfterDeserialize()
        {
            m_OutputNode = null;
            m_Layers = SerializationHelper.Deserialize<Layer>(m_SerializedLayers, null);
            m_SerializedLayers = null;
            base.OnAfterDeserialize();
        }

        public static string LayerToFunctionName(Guid id)
        {
            return string.Format("Layer_{0}", GuidEncoder.Encode(id));
        }

        public string GetShader(string name, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            if (outputNode == null)
                throw new InvalidOperationException();

            var layerMap = new Dictionary<Guid, MaterialGraph>();

            foreach (var layer in layers)
            {
                var path = AssetDatabase.GetAssetPath(layer.shader);

                if (!path.EndsWith("shaderGraph", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var textGraph = File.ReadAllText(path, Encoding.UTF8);
                var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);
                if (graph == null)
                    continue;

                layerMap[layer.guid] = graph;
            }

            if (layerMap.Count == 0)
            {
                configuredTextures = new List<PropertyCollector.TextureInfo>();
                return string.Empty;
            }

            var vertexShader = new ShaderGenerator();
            var layerShaders = new ShaderGenerator();
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

            using (var activeNodesDisposable = ListPool<INode>.GetDisposable())
            {
                var activeNodes = activeNodesDisposable.value;
                foreach (var layer in layerMap)
                {
                    NodeUtils.DepthFirstCollectNodesFromNode(activeNodes, layer.Value.masterNode as AbstractMaterialNode);
                }
                NodeUtils.DepthFirstCollectNodesFromNode(activeNodes, outputNode);
                var requirements = GetRequirements(activeNodes);

                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceInputs);

                if (requirements.requiresVertexColor)
                    surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

                if (requirements.requiresScreenPosition)
                    surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

                foreach (var channel in requirements.requiresMeshUVs.Distinct())
                    surfaceInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);

                surfaceInputs.Deindent();
                surfaceInputs.AddShaderChunk("};", false);

                vertexShader.AddShaderChunk("GraphVertexInput PopulateVertexData(GraphVertexInput v){", false);
                vertexShader.Indent();
                vertexShader.AddShaderChunk("return v;", false);
                vertexShader.Deindent();
                vertexShader.AddShaderChunk("}", false);

                var shaderProperties = new PropertyCollector();

                var baseGraph = layerMap.Values.FirstOrDefault();
                if (baseGraph == null)
                {
                    configuredTextures = new List<PropertyCollector.TextureInfo>();
                    return string.Empty;
                }

                var masterNode = baseGraph.masterNode;
                GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, ((AbstractMaterialNode)masterNode).GetInputSlots<MaterialSlot>().ToList(), true);

                /*  foreach (var layer in layerMap)
                  {
                      activeNodes.Clear();
                      NodeUtils.DepthFirstCollectNodesFromNode(activeNodes, layer.Value.masterNode as AbstractMaterialNode);
                      GenerateSurfaceDescription(
                          activeNodes,
                          layer.Value.masterNode as AbstractMaterialNode,
                          this,
                          surfaceDescriptionFunction,
                          shaderFunctionVisitor,
                          shaderProperties,
                          requirements,
                          mode,
                          LayerToFunctionName(layer.Key));
                  }*/

                surfaceDescriptionStruct.AddShaderChunk("struct WeightsSurfaceDescription{", false);
                surfaceDescriptionStruct.Indent();

                foreach (var slot in outputNode.GetInputSlots<MaterialSlot>())
                    surfaceDescriptionStruct.AddShaderChunk(AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType) + " " + slot.shaderOutputName + ";", false);

                surfaceDescriptionStruct.Deindent();
                surfaceDescriptionStruct.AddShaderChunk("};", false);

                activeNodes.Clear();
                NodeUtils.DepthFirstCollectNodesFromNode(activeNodes, outputNode);
                /*GenerateSurfaceDescription(
                    activeNodes,
                    outputNode,
                    this,
                    surfaceDescriptionFunction,
                    shaderFunctionVisitor,
                    shaderProperties,
                    requirements,
                    mode,
                    "PopulateWeightsGraph",
                    "WeightsSurfaceDescription");
                    */

                string functionName = "PopulateSurfaceData";
                string surfaceDescriptionName = "SurfaceDescription";
                layerShaders.AddShaderChunk(string.Format("{0} {1}(SurfaceInputs IN) {{", surfaceDescriptionName, functionName), false);
                layerShaders.Indent();

                layerShaders.AddShaderChunk("WeightsSurfaceDescription weights = PopulateWeightsGraph(IN);", false);
                layerShaders.AddShaderChunk("SurfaceDescription result = (SurfaceDescription)0;", false);

                foreach (var layer in layerMap)
                {
                    layerShaders.AddShaderChunk(
                        string.Format(
                            "{0} {1} = {2}({3});",
                            surfaceDescriptionName,
                            LayerToFunctionName(layer.Key) + "_surface",
                            LayerToFunctionName(layer.Key),
                            "IN"), false);

                    layerShaders.AddShaderChunk(
                        string.Format("ScaleSurfaceDescription({0}_surface, weights.{0});", LayerToFunctionName(layer.Key)), false);

                    layerShaders.AddShaderChunk(string.Format("AddSurfaceDescription(result, {0}_surface);", LayerToFunctionName(layer.Key)), false);
                }
                layerShaders.AddShaderChunk("return result;", false);

                layerShaders.Deindent();
                layerShaders.AddShaderChunk("}", false);

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
                finalShader.AddShaderChunk(layerShaders.GetShaderString(2), false);
                finalShader.AddShaderChunk("ENDCG", false);

                if (masterNode != null)
                {
                    //var subShaders = masterNode.GetSubshader(requirements, null);
                    // foreach (var ss in subShaders)
                    //    finalShader.AddShaderChunk(ss, false);
                }

                finalShader.Deindent();
                finalShader.AddShaderChunk("}", false);
                configuredTextures = shaderProperties.GetConfiguredTexutres();
                return finalShader.GetShaderString(0);
            }
        }

        public void LoadedFromDisk()
        {
            OnEnable();
            ValidateGraph();
        }
    }
}
