using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    public sealed class ShaderGraphVfxAsset : ScriptableObject
    {
        [SerializeField]
        internal GraphCompilationResult compilationResult;

        [SerializeField]
        internal ShaderGraphRequirements[] portRequirements;

        [SerializeField]
        OutputMetadata m_PositionOutput;

        [SerializeField]
        OutputMetadata m_BaseColorOutput;

        [SerializeField]
        OutputMetadata m_MetallicOutput;

        [SerializeField]
        OutputMetadata m_SmoothnessOutput;

        [SerializeField]
        OutputMetadata m_AlphaOutput;

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

        public ConcretePrecision concretePrecision
        {
            get => m_ConcretePrecision;
            set => m_ConcretePrecision = value;
        }

        public OutputMetadata positionOutput
        {
            get => m_PositionOutput;
            internal set => m_PositionOutput = value;
        }

        public OutputMetadata baseColorOutput
        {
            get => m_BaseColorOutput;
            internal set => m_BaseColorOutput = value;
        }

        public OutputMetadata metallicOutput
        {
            get => m_MetallicOutput;
            internal set => m_MetallicOutput = value;
        }

        public OutputMetadata smoothnessOutput
        {
            get => m_SmoothnessOutput;
            internal set => m_SmoothnessOutput = value;
        }

        public OutputMetadata alphaOutput
        {
            get => m_AlphaOutput;
            internal set => m_AlphaOutput = value;
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
                graphCode.requirements = graphCode.requirements.Union(portRequirements[i]);
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
            var filteredProperties = propertyIndices.Select(i => m_Properties[i]).ToArray();
            graphCode.properties = filteredProperties;

            return graphCode;
        }
    }
}
