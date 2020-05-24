using System;
using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct FunctionPair
    {
        public string key;
        public string value;

        public FunctionPair(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }

    class SubGraphData : JsonObject
    {
        public List<JsonData<AbstractShaderProperty>> inputs = new List<JsonData<AbstractShaderProperty>>();
        public List<JsonData<ShaderKeyword>> keywords = new List<JsonData<ShaderKeyword>>();
        public List<JsonData<AbstractShaderProperty>> nodeProperties = new List<JsonData<AbstractShaderProperty>>();
        public List<JsonData<MaterialSlot>> outputs = new List<JsonData<MaterialSlot>>();
    }
    
    class SubGraphAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        public bool isValid;

        public bool isRecursive;
        
        public long processedAt;

        public string functionName;

        public string inputStructName;

        public string hlslName;

        public string assetGuid;

        public ShaderGraphRequirements requirements;

        public string path;

        public List<FunctionPair> functions = new List<FunctionPair>();

        private SubGraphData m_SubGraphData;

        [SerializeField]
        private SerializationHelper.JSONSerializedElement m_SerializedSubGraphData;

        public DataValueEnumerable<AbstractShaderProperty> inputs => m_SubGraphData.inputs.SelectValue();
        
        public DataValueEnumerable<ShaderKeyword> keywords => m_SubGraphData.keywords.SelectValue();
        
        public DataValueEnumerable<AbstractShaderProperty> nodeProperties => m_SubGraphData.nodeProperties.SelectValue();

        public DataValueEnumerable<MaterialSlot> outputs => m_SubGraphData.outputs.SelectValue();

        public List<string> children = new List<string>();

        public List<string> descendents = new List<string>();

        public ShaderStageCapability effectiveShaderStage;
        
        public ConcretePrecision graphPrecision;

        public ConcretePrecision outputPrecision;
        
        public void WriteData(IEnumerable<AbstractShaderProperty> inputs, IEnumerable<ShaderKeyword> keywords, IEnumerable<AbstractShaderProperty> nodeProperties, IEnumerable<MaterialSlot> outputs)
        {
            if(m_SubGraphData == null)
            {
                m_SubGraphData = new SubGraphData();
            }
            m_SubGraphData.inputs.Clear();
            m_SubGraphData.keywords.Clear();
            m_SubGraphData.nodeProperties.Clear();
            m_SubGraphData.outputs.Clear();

            foreach(var input in inputs)
            {
                m_SubGraphData.inputs.Add(input);
            }

            foreach(var keyword in keywords)
            {
                m_SubGraphData.keywords.Add(keyword);
            }

            foreach(var nodeProperty in nodeProperties)
            {
                m_SubGraphData.nodeProperties.Add(nodeProperty);
            }

            foreach(var output in outputs)
            {
                m_SubGraphData.outputs.Add(output);
            }
            var json = MultiJson.Serialize(m_SubGraphData);
            m_SerializedSubGraphData = new SerializationHelper.JSONSerializedElement() { JSONnodeData = json };
            m_SubGraphData = null;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            
        }

        public void LoadGraphData()
        {
            if(!String.IsNullOrEmpty(m_SerializedSubGraphData.JSONnodeData))
            {
                m_SubGraphData = new SubGraphData();
                MultiJson.Deserialize(m_SubGraphData, m_SerializedSubGraphData.JSONnodeData);
            }
        }
    }
}
