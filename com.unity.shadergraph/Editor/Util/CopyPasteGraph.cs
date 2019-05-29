using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Graphing.Util
{
    [Serializable]
    sealed class CopyPasteGraph : ISerializationCallbackReceiver
    {
        [NonSerialized]
        HashSet<IEdge> m_Edges = new HashSet<IEdge>();

        [NonSerialized]
        HashSet<AbstractMaterialNode> m_Nodes = new HashSet<AbstractMaterialNode>();

        [SerializeField]
        List<GroupData> m_Groups = new List<GroupData>();

        [NonSerialized]
        HashSet<ShaderInput> m_Inputs = new HashSet<ShaderInput>();

        // The meta properties are properties that are not copied into the tatget graph
        // but sent along to allow property nodes to still hvae the data from the original
        // property present.
        [NonSerialized]
        HashSet<ShaderInput> m_MetaInputs = new HashSet<ShaderInput>();

        [SerializeField]
        string m_SourceGraphGuid;

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerilaizeableProperties = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableMetaProperties = new List<SerializationHelper.JSONSerializedElement>();

        public CopyPasteGraph() {}

        public CopyPasteGraph(string sourceGraphGuid, IEnumerable<GroupData> groups, IEnumerable<AbstractMaterialNode> nodes, IEnumerable<IEdge> edges, IEnumerable<ShaderInput> properties, IEnumerable<ShaderInput> metaProperties)
        {
            m_SourceGraphGuid = sourceGraphGuid;

            foreach (var groupData in groups)
            {
                AddGroup(groupData);
            }

            foreach (var node in nodes)
            {
                if (!node.canCopyNode)
                {
                    throw new InvalidOperationException($"Cannot copy node {node.name} ({node.guid}).");
                }
                AddNode(node);
                foreach (var edge in NodeUtils.GetAllEdges(node))
                    AddEdge(edge);
            }

            foreach (var edge in edges)
                AddEdge(edge);

            foreach (var property in properties)
                AddInput(property);

            foreach (var metaProperty in metaProperties)
                AddMetaInput(metaProperty);
        }

        public void AddGroup(GroupData group)
        {
            m_Groups.Add(group);
        }

        public void AddNode(AbstractMaterialNode node)
        {
            m_Nodes.Add(node);
        }

        public void AddEdge(IEdge edge)
        {
            m_Edges.Add(edge);
        }

        public void AddInput(ShaderInput input)
        {
            m_Inputs.Add(input);
        }

        public void AddMetaInput(ShaderInput metaInput)
        {
            m_MetaInputs.Add(metaInput);
        }

        public IEnumerable<T> GetNodes<T>()
        {
            return m_Nodes.OfType<T>();
        }

        public IEnumerable<GroupData> groups
        {
            get { return m_Groups; }
        }

        public IEnumerable<IEdge> edges
        {
            get { return m_Edges; }
        }

        public IEnumerable<ShaderInput> inputs
        {
            get { return m_Inputs; }
        }

        public IEnumerable<ShaderInput> metaInputs
        {
            get { return m_MetaInputs; }
        }

        public string sourceGraphGuid
        {
            get { return m_SourceGraphGuid; }
        }

        public void OnBeforeSerialize()
        {
            m_SerializableNodes = SerializationHelper.Serialize<AbstractMaterialNode>(m_Nodes);
            m_SerializableEdges = SerializationHelper.Serialize<IEdge>(m_Edges);
            m_SerilaizeableProperties = SerializationHelper.Serialize<ShaderInput>(m_Inputs);
            m_SerializableMetaProperties = SerializationHelper.Serialize<ShaderInput>(m_MetaInputs);
        }

        public void OnAfterDeserialize()
        {
            var nodes = SerializationHelper.Deserialize<AbstractMaterialNode>(m_SerializableNodes, GraphUtil.GetLegacyTypeRemapping());
            m_Nodes.Clear();
            foreach (var node in nodes)
                m_Nodes.Add(node);
            m_SerializableNodes = null;

            var edges = SerializationHelper.Deserialize<IEdge>(m_SerializableEdges, GraphUtil.GetLegacyTypeRemapping());
            m_Edges.Clear();
            foreach (var edge in edges)
                m_Edges.Add(edge);
            m_SerializableEdges = null;

            var properties = SerializationHelper.Deserialize<ShaderInput>(m_SerilaizeableProperties, GraphUtil.GetLegacyTypeRemapping());
            m_Inputs.Clear();
            foreach (var property in properties)
                m_Inputs.Add(property);
            m_SerilaizeableProperties = null;

            var metaProperties = SerializationHelper.Deserialize<ShaderInput>(m_SerializableMetaProperties, GraphUtil.GetLegacyTypeRemapping());
            m_MetaInputs.Clear();
            foreach (var metaProperty in metaProperties)
            {
                m_MetaInputs.Add(metaProperty);
            }
            m_SerializableMetaProperties = null;
        }

        internal static CopyPasteGraph FromJson(string copyBuffer)
        {
            try
            {
                return JsonUtility.FromJson<CopyPasteGraph>(copyBuffer);
            }
            catch
            {
                // ignored. just means copy buffer was not a graph :(
                return null;
            }
        }
    }
}
