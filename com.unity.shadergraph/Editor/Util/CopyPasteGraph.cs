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
        HashSet<INode> m_Nodes = new HashSet<INode>();

        [NonSerialized]
        HashSet<IShaderProperty> m_Properties = new HashSet<IShaderProperty>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerilaizeableProperties = new List<SerializationHelper.JSONSerializedElement>();

        public CopyPasteGraph() {}

        public CopyPasteGraph(IEnumerable<INode> nodes, IEnumerable<IEdge> edges, IEnumerable<IShaderProperty> properties)
        {
            foreach (var node in nodes)
            {
                AddNode(node);
                foreach (var edge in NodeUtils.GetAllEdges(node))
                    AddEdge(edge);
            }

            foreach (var edge in edges)
                AddEdge(edge);

            foreach (var property in properties)
                AddProperty(property);
        }

        public void AddNode(INode node)
        {
            m_Nodes.Add(node);
        }

        public void AddEdge(IEdge edge)
        {
            m_Edges.Add(edge);
        }

        public void AddProperty(IShaderProperty property)
        {
            m_Properties.Add(property);
        }

        public IEnumerable<T> GetNodes<T>() where T : INode
        {
            return m_Nodes.OfType<T>();
        }

        public IEnumerable<IEdge> edges
        {
            get { return m_Edges; }
        }

        public IEnumerable<IShaderProperty> properties
        {
            get { return m_Properties; }
        }

        public void OnBeforeSerialize()
        {
            m_SerializableNodes = SerializationHelper.Serialize<INode>(m_Nodes);
            m_SerializableEdges = SerializationHelper.Serialize<IEdge>(m_Edges);
            m_SerilaizeableProperties = SerializationHelper.Serialize<IShaderProperty>(m_Properties);
        }

        public void OnAfterDeserialize()
        {
            var nodes = SerializationHelper.Deserialize<INode>(m_SerializableNodes, GraphUtil.GetLegacyTypeRemapping());
            m_Nodes.Clear();
            foreach (var node in nodes)
                m_Nodes.Add(node);
            m_SerializableNodes = null;

            var edges = SerializationHelper.Deserialize<IEdge>(m_SerializableEdges, GraphUtil.GetLegacyTypeRemapping());
            m_Edges.Clear();
            foreach (var edge in edges)
                m_Edges.Add(edge);
            m_SerializableEdges = null;

            var properties = SerializationHelper.Deserialize<IShaderProperty>(m_SerilaizeableProperties, GraphUtil.GetLegacyTypeRemapping());
            m_Properties.Clear();
            foreach (var property in properties)
                m_Properties.Add(property);
            m_SerilaizeableProperties = null;
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
