using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SubGraphData : ISerializationCallbackReceiver
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

        public List<string> functionNames = new List<string>();

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

        public List<string> ancestors = new List<string>();

        public ShaderStageCapability effectiveShaderStage;

        public void Reset()
        {
            isValid = true;
            isRecursive = false;
            processedAt = 0;
            functionName = null;
            inputStructName = null;
            hlslName = null;
            assetGuid = null;
            requirements = ShaderGraphRequirements.none;
            path = null;
            functionNames.Clear();
            inputs.Clear();
            m_SerializedInputs.Clear();
            nodeProperties.Clear();
            m_SerializedProperties.Clear();
            outputs.Clear();
            m_SerializedOutputs.Clear();
            children.Clear();
            descendents.Clear();
            ancestors.Clear();
            effectiveShaderStage = ShaderStageCapability.All;
        }
        
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
