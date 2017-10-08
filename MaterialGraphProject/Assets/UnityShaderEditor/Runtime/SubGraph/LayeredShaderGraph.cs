using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class LayeredShaderGraph : AbstractMaterialGraph, IShaderGraph
    {
        [Serializable]
        public class Layer
        {
            [SerializeField]
            private int m_Layer;

            [SerializeField]
            private Shader m_Shader;

            public Layer()
            {
                m_Layer = Guid.NewGuid().GetHashCode();
            }

            public int layer
            {
                get { return m_Layer; }
            }

            public Shader shader
            {
                get { return m_Shader; }
                set { m_Shader = value; }
            }
        }

        [NonSerialized]
        private List<Layer> m_Layers = new List<Layer>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedLayers = new List<SerializationHelper.JSONSerializedElement>();

        public IEnumerable<Layer> layers
        {
            get { return m_Layers; }
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
            NotifyChange(new LayerAdded(layer));

            if (outputNode != null)
                outputNode.onModified(outputNode, ModificationScope.Graph);
        }

        public bool SetLayer(int layerId, Shader newShader)
        {
            try
            {
                var path = AssetDatabase.GetAssetPath(newShader);

                if (!path.EndsWith("shaderGraph", StringComparison.InvariantCultureIgnoreCase))
                    return false;

                var name = Path.GetFileNameWithoutExtension(path);
                var textGraph = File.ReadAllText(path, Encoding.UTF8);
                var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);
                if (graph == null)
                    return false;

                var layer = layers.FirstOrDefault(x => x.layer == layerId);
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

        public void RemoveLayer(int id)
        {
            var num = m_Layers.RemoveAll(x => x.layer == id);

            if (num > 0)
            {
                NotifyChange(new LayerRemoved(id));

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

        public string GetShader(string name, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
           if (outputNode == null)
                throw new InvalidOperationException();

            var layerMap = new Dictionary<int,MaterialGraph>();

            foreach (var layer in layers)
            {
                var path = AssetDatabase.GetAssetPath(layer.shader);

                if (!path.EndsWith("shaderGraph", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var textGraph = File.ReadAllText(path, Encoding.UTF8);
                var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);
                if (graph == null)
                    continue;

                layerMap[layer.layer] = graph;
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

            var requirements = ShaderGraphRequirements.none;
            foreach (var layer in layerMap)
                requirements = requirements.Union(GetRequierments(layer.Value.masterNode as AbstractMaterialNode));

            requirements = requirements.Union(GetRequierments(outputNode));

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

            GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, layerMap[0].masterNode as AbstractMaterialNode, true);

            foreach (var layer in layerMap)
            {
                GenerateSurfaceDescription(
                    layer.Value.masterNode as AbstractMaterialNode,
                    surfaceDescriptionFunction,
                    shaderFunctionVisitor,
                    shaderProperties,
                    requirements,
                    mode,
                    true,
                    "Layer_" + Mathf.Abs(layer.Key));
            }



            surfaceDescriptionStruct.AddShaderChunk("struct WeightsSurfaceDescription{", false);
            surfaceDescriptionStruct.Indent();

            foreach (var slot in outputNode.GetInputSlots<MaterialSlot>())
                surfaceDescriptionStruct.AddShaderChunk(AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType) + " " + slot.shaderOutputName + ";", false);

            surfaceDescriptionStruct.Deindent();
            surfaceDescriptionStruct.AddShaderChunk("};", false);

            GenerateSurfaceDescription(
                outputNode,
                surfaceDescriptionFunction,
                shaderFunctionVisitor,
                shaderProperties,
                requirements,
                mode,
                true,
                "PopulateWeightsGraph",
                "WeightsSurfaceDescription");

            foreach (var layer in layerMap)
            {

            }

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

/*            var masterNode = node as IMasterNode;
            if (masterNode != null)
            {
                var subShaders = masterNode.GetSubshader(requirements, null);
                foreach (var ss in subShaders)
                    finalShader.AddShaderChunk(ss, false);
            }
            else
            {
                finalShader.AddShaderChunk(ShaderGenerator.GetPreviewSubShader(node, requirements), false);
            }*/

            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);
            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.GetShaderString(0);
        }

    }
}
