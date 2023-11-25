using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using TextureDimension = UnityEngine.Rendering.TextureDimension;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct TextureInfo
    {
        public TextureInfo(string name, Texture texture, TextureDimension dimension)
        {
            this.name = name;
            this.texture = texture;
            this.dimension = dimension;
            Debug.Assert(texture == null || texture.dimension == dimension);
        }

        public string name;
        public Texture texture;
        public TextureDimension dimension;

        public int instanceID => texture != null ? texture.GetInstanceID() : 0;
    }

    public sealed class ShaderGraphVfxAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        private class ShaderGraphVfxAssetData : JsonObject
        {
            public List<JsonData<ShaderInput>> m_Properties = new();
        }

        public const int BaseColorSlotId = 1;
        public const int MetallicSlotId = 2;
        public const int SmoothnessSlotId = 3;
        public const int NormalSlotId = 8;
        public const int AlphaSlotId = 4;
        public const int EmissiveSlotId = 5;
        public const int ColorSlotId = 6;
        public const int AlphaThresholdSlotId = 7;

        [SerializeField]
        public bool generatesWithShaderGraph;

        [SerializeField]
        public bool lit;

        [SerializeField]
        public bool alphaClipping;

        [SerializeField]
        internal ShaderStageCapability[] m_PropertiesStages;

        [SerializeField]
        internal GraphCompilationResult compilationResult;

        [SerializeField]
        internal ShaderGraphRequirements[] portRequirements;

        [SerializeField]
        string m_EvaluationFunctionName;

        [SerializeField]
        string m_InputStructName;

        [SerializeField]
        string m_OutputStructName;

        [SerializeField]
        ConcretePrecision m_ConcretePrecision = ConcretePrecision.Single;

        ShaderGraphVfxAssetData m_Data = new ShaderGraphVfxAssetData();

        [HideInInspector]
        [SerializeField]
        private SerializationHelper.JSONSerializedElement m_SerializedVfxAssetData;

        [SerializeField]
        internal IntArray[] outputPropertyIndices;

        internal ConcretePrecision concretePrecision
        {
            get => m_ConcretePrecision;
            set => m_ConcretePrecision = value;
        }

        [SerializeField]
        OutputMetadata[] m_Outputs;

        [SerializeField]
        TextureInfo[] m_TextureInfos;

        public IEnumerable<TextureInfo> textureInfos { get => m_TextureInfos; }

        internal void SetTextureInfos(IList<PropertyCollector.TextureInfo> textures)
        {
            m_TextureInfos = textures.Select(t => new TextureInfo(t.name, EditorUtility.InstanceIDToObject(t.textureId) as Texture, t.dimension)).ToArray();
        }

        internal void SetOutputs(OutputMetadata[] outputs)
        {
            m_Outputs = outputs;
        }

        public OutputMetadata GetOutput(int id)
        {
            return m_Outputs.FirstOrDefault(t => t.id == id);
        }

        public bool HasOutput(int id)
        {
            return m_Outputs.Any(t => t.id == id);
        }

        public string evaluationFunctionName
        {
            get { return m_EvaluationFunctionName; }
            internal set { m_EvaluationFunctionName = value; }
        }

        public string inputStructName
        {
            get { return m_InputStructName; }
            internal set { m_InputStructName = value; }
        }

        public string outputStructName
        {
            get { return m_OutputStructName; }
            internal set { m_OutputStructName = value; }
        }

        public List<ShaderInput> properties
        {
            get
            {
                EnsureProperties();
                return m_Data.m_Properties.SelectValue().ToList();
            }
        }

        public List<AbstractShaderProperty> fragmentProperties
        {
            get
            {
                EnsureProperties();
                var allProperties = m_Data.m_Properties.SelectValue().ToList();
                var fragProperties = new List<AbstractShaderProperty>();
                for (var i = 0; i < allProperties.Count(); i++)
                {
                    if (allProperties[i] is AbstractShaderProperty property
                        && (m_PropertiesStages[i] & ShaderStageCapability.Fragment) != 0)
                        fragProperties.Add(property);
                }
                return fragProperties;
            }
        }

        public List<AbstractShaderProperty> vertexProperties
        {
            get
            {
                EnsureProperties();
                var allProperties = m_Data.m_Properties.SelectValue().ToList();
                var vertexProperties = new List<AbstractShaderProperty>();
                for (var i = 0; i < allProperties.Count(); i++)
                {
                    if (allProperties[i] is AbstractShaderProperty property
                        && (m_PropertiesStages[i] & ShaderStageCapability.Vertex) != 0)
                        vertexProperties.Add(property);
                }
                return vertexProperties;
            }
        }

        internal void SetProperties(List<ShaderInput> propertiesList)
        {
            m_Data.m_Properties.Clear();
            foreach (var property in propertiesList)
            {
                m_Data.m_Properties.Add(property);
            }

            var json = MultiJson.Serialize(m_Data);
            m_SerializedVfxAssetData = new SerializationHelper.JSONSerializedElement() { JSONnodeData = json };
            m_Data = null;
        }

        void EnsureProperties()
        {
            if ((m_Data == null || m_Data.m_Properties == null || !m_Data.m_Properties.Any()) && !String.IsNullOrEmpty(m_SerializedVfxAssetData.JSONnodeData))
            {
                m_Data = new ShaderGraphVfxAssetData();
                MultiJson.Deserialize(m_Data, m_SerializedVfxAssetData.JSONnodeData);
            }

            foreach (var property in m_Data.m_Properties.SelectValue())
            {
                if (property is AbstractShaderProperty shaderProperty)
                    shaderProperty.SetupConcretePrecision(m_ConcretePrecision);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_Data = null;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        public GraphCode GetCode(OutputMetadata[] outputs)
        {
            var graphCode = new GraphCode();

            graphCode.requirements = ShaderGraphRequirements.none;
            var outputIndices = new int[outputs.Length];
            for (var i = 0; i < outputs.Length; i++)
            {
                if (!outputs[i].isValid)
                {
                    throw new ArgumentException($"Invalid {nameof(OutputMetadata)} at index {i}.", nameof(outputs));
                }

                outputIndices[i] = outputs[i].index;
                graphCode.requirements = graphCode.requirements.Union(portRequirements[outputs[i].index]);
            }

            graphCode.code = compilationResult.GenerateCode(outputIndices);

            var propertyIndexSet = new HashSet<int>();
            foreach (var outputIndex in outputIndices)
            {
                foreach (var propertyIndex in outputPropertyIndices[outputIndex].array)
                {
                    propertyIndexSet.Add(propertyIndex);
                }
            }
            var propertyIndices = propertyIndexSet.ToArray();
            Array.Sort(propertyIndices);
            var filteredProperties = propertyIndices.Select(i => properties[i]).OfType<AbstractShaderProperty>().ToArray();
            graphCode.properties = filteredProperties;

            return graphCode;
        }
    }
}
