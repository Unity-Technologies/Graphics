using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct TextureInfo
    {
        public string name;
        public Texture texture;
    }

    public sealed class ShaderGraphVfxAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        public const int BaseColorSlotId = 1;
        public const int MetallicSlotId = 2;
        public const int SmoothnessSlotId = 3;
        public const int NormalSlotId = 8;
        public const int AlphaSlotId = 4;
        public const int EmissiveSlotId = 5;
        public const int ColorSlotId = 6;
        public const int AlphaThresholdSlotId = 7;

        [SerializeField]
        public bool lit;

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
        ConcretePrecision m_ConcretePrecision = ConcretePrecision.Float;

        [NonSerialized]
        List<AbstractShaderProperty> m_Properties;

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();

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

        internal void SetTextureInfos(IList<PropertyCollector.TextureInfo> textures )
        {
            m_TextureInfos = textures.Select(t => new TextureInfo() { name = t.name, texture = EditorUtility.InstanceIDToObject(t.textureId) as Texture }).ToArray();
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

        public IEnumerable<AbstractShaderProperty> properties
        {
            get
            {
                EnsureProperties();
                return m_Properties;
            }
        }

        internal void SetProperties(List<AbstractShaderProperty> propertiesList)
        {
            m_Properties = propertiesList;
            m_SerializedProperties = SerializationHelper.Serialize<AbstractShaderProperty>(m_Properties);
        }

        void EnsureProperties()
        {
            if (m_Properties == null)
            {
                m_Properties = SerializationHelper.Deserialize<AbstractShaderProperty>(m_SerializedProperties, GraphUtil.GetLegacyTypeRemapping());
                foreach (var property in m_Properties)
                {
                    property.ValidateConcretePrecision(m_ConcretePrecision);
                }
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            //After import the object will be deserialized into the previous instance, leading to an descynchronization between m_SerializedProperties and m_Properties
            m_Properties = null;
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
            EnsureProperties();
            var propertyIndices = propertyIndexSet.ToArray();
            Array.Sort(propertyIndices);
            var filteredProperties = propertyIndices.Select(i => m_Properties[i]).ToArray();
            graphCode.properties = filteredProperties;

            return graphCode;
        }
    }
}
