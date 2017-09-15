using System;
using System.Collections.Generic;
using UnityEngine.Graphing;
using UnityEngine.XR.WSA.WebCam;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class AbstractMaterialGraph : SerializableGraph, IGenerateProperties
    {
        [NonSerialized]
        private List<IShaderProperty> m_Properties = new List<IShaderProperty>();

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();


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

        public void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            foreach(var prop in properties)
                collector.AddShaderProperty(prop);
        }

        public void AddShaderProperty(IShaderProperty property)
        {
            if (property == null)
                return;

            if (m_Properties.Contains(property))
                return;

            m_Properties.Add(property);
        }

        public void RemoveShaderProperty(Guid guid)
        {
            m_Properties.RemoveAll(x => x.guid == guid);
        }

        public override Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            var result = base.GetLegacyTypeRemapping();
            var viewNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.ViewDirectionNode",
                assemblyName = "Assembly-CSharp"
            };
            result[viewNode] = SerializationHelper.GetTypeSerializableAsString(typeof(WorldSpaceViewDirectionNode));

            var normalNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.NormalNode",
                assemblyName = "Assembly-CSharp"
            };
            result[normalNode] = SerializationHelper.GetTypeSerializableAsString(typeof(WorldSpaceNormalNode));

            var worldPosNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.WorldPosNode",
                assemblyName = "Assembly-CSharp"
            };
            result[worldPosNode] = SerializationHelper.GetTypeSerializableAsString(typeof(WorldSpacePositionNode));


            return result;
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
    }
}
