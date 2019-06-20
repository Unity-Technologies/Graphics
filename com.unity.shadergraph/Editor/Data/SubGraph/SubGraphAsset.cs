using System;
using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Graphing;
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

        [NonSerialized]
        public List<AbstractShaderProperty> inputs = new List<AbstractShaderProperty>();
        
        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedInputs = new List<SerializationHelper.JSONSerializedElement>();
        
        [NonSerialized]
        public List<AbstractShaderProperty> nodeProperties = new List<AbstractShaderProperty>();
        
        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();
        
        [NonSerialized]
        public List<MaterialSlot> outputs = new List<MaterialSlot>();
        
        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedOutputs = new List<SerializationHelper.JSONSerializedElement>();

        public List<string> children = new List<string>();

        public List<string> descendents = new List<string>();

        public ShaderStageCapability effectiveShaderStage;
        
        public ConcretePrecision graphPrecision;

        public ConcretePrecision outputPrecision;
        
        public void OnBeforeSerialize()
        {
            m_SerializedInputs = SerializationHelper.Serialize<AbstractShaderProperty>(inputs);
            m_SerializedProperties = SerializationHelper.Serialize<AbstractShaderProperty>(nodeProperties);
            m_SerializedOutputs = SerializationHelper.Serialize<MaterialSlot>(outputs);
        }

        public void OnAfterDeserialize()
        {
            var typeSerializationInfos = GraphUtil.GetLegacyTypeRemapping();
            inputs = SerializationHelper.Deserialize<AbstractShaderProperty>(m_SerializedInputs, typeSerializationInfos);
            nodeProperties = SerializationHelper.Deserialize<AbstractShaderProperty>(m_SerializedProperties, typeSerializationInfos);
            outputs = SerializationHelper.Deserialize<MaterialSlot>(m_SerializedOutputs, typeSerializationInfos);
        }
    }
}