using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

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

        [SerializeField]
        List<StickyNoteData> m_StickyNotes = new List<StickyNoteData>();

        [NonSerialized]
        List<ShaderInput> m_Inputs = new List<ShaderInput>();

        // The meta properties are properties that are not copied into the target graph
        // but sent along to allow property nodes to still hvae the data from the original
        // property present.
        [NonSerialized]
        HashSet<AbstractShaderProperty> m_MetaProperties = new HashSet<AbstractShaderProperty>();

        // The meta keywords are keywords that are required by keyword nodes
        // These are copied into the target graph when there is no collision
        [NonSerialized]
        HashSet<ShaderKeyword> m_MetaKeywords = new HashSet<ShaderKeyword>();

        [SerializeField]
        string m_SourceGraphGuid;

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerilaizeableInputs = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableMetaProperties = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableMetaKeywords = new List<SerializationHelper.JSONSerializedElement>();

        public CopyPasteGraph() {}

        public CopyPasteGraph(string sourceGraphGuid, IEnumerable<GroupData> groups, IEnumerable<AbstractMaterialNode> nodes, IEnumerable<IEdge> edges,
            IEnumerable<ShaderInput> inputs, IEnumerable<AbstractShaderProperty> metaProperties, IEnumerable<ShaderKeyword> metaKeywords, IEnumerable<StickyNoteData> notes)
        {
            m_SourceGraphGuid = sourceGraphGuid;

            if (groups != null)
            {
                foreach (var groupData in groups)
                    AddGroup(groupData);
            }

            if (notes != null)
            {
                foreach (var stickyNote in notes)
                    AddNote(stickyNote);
            }

            if (nodes != null)
            {
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
            }

            if (edges != null)
            {
                foreach (var edge in edges)
                    AddEdge(edge);
            }

            if (inputs != null)
            {
                foreach (var input in inputs)
                    AddInput(input);
            }

            if (metaProperties != null)
            {
                foreach (var metaProperty in metaProperties)
                    AddMetaProperty(metaProperty);
            }

            if (metaKeywords != null)
            {
                foreach (var metaKeyword in metaKeywords)
                    AddMetaKeyword(metaKeyword);
            }
        }

        public void AddGroup(GroupData group)
        {
            m_Groups.Add(group);
        }

        public void AddNote(StickyNoteData stickyNote)
        {
            m_StickyNotes.Add(stickyNote);
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

        public void AddMetaProperty(AbstractShaderProperty metaProperty)
        {
            m_MetaProperties.Add(metaProperty);
        }

        public void AddMetaKeyword(ShaderKeyword metaKeyword)
        {
            m_MetaKeywords.Add(metaKeyword);
        }

        public IEnumerable<T> GetNodes<T>()
        {
            return m_Nodes.OfType<T>();
        }

        public IEnumerable<GroupData> groups
        {
            get { return m_Groups; }
        }

        public IEnumerable<StickyNoteData> stickyNotes => m_StickyNotes;

        public IEnumerable<IEdge> edges
        {
            get { return m_Edges; }
        }

        public IEnumerable<ShaderInput> inputs
        {
            get { return m_Inputs; }
        }

        public IEnumerable<AbstractShaderProperty> metaProperties
        {
            get { return m_MetaProperties; }
        }

        public IEnumerable<ShaderKeyword> metaKeywords
        {
            get { return m_MetaKeywords; }
        }

        public string sourceGraphGuid
        {
            get { return m_SourceGraphGuid; }
        }

        public void OnBeforeSerialize()
        {
            m_SerializableNodes = SerializationHelper.Serialize<AbstractMaterialNode>(m_Nodes);
            m_SerializableEdges = SerializationHelper.Serialize<IEdge>(m_Edges);
            m_SerilaizeableInputs = SerializationHelper.Serialize<ShaderInput>(m_Inputs);
            m_SerializableMetaProperties = SerializationHelper.Serialize<AbstractShaderProperty>(m_MetaProperties);
            m_SerializableMetaKeywords = SerializationHelper.Serialize<ShaderKeyword>(m_MetaKeywords);
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

            var inputs = SerializationHelper.Deserialize<ShaderInput>(m_SerilaizeableInputs, GraphUtil.GetLegacyTypeRemapping());
            m_Inputs.Clear();
            foreach (var input in inputs)
                m_Inputs.Add(input);
            m_SerilaizeableInputs = null;

            var metaProperties = SerializationHelper.Deserialize<AbstractShaderProperty>(m_SerializableMetaProperties, GraphUtil.GetLegacyTypeRemapping());
            m_MetaProperties.Clear();
            foreach (var metaProperty in metaProperties)
            {
                m_MetaProperties.Add(metaProperty);
            }
            m_SerializableMetaProperties = null;

            var metaKeywords = SerializationHelper.Deserialize<ShaderKeyword>(m_SerializableMetaKeywords, GraphUtil.GetLegacyTypeRemapping());
            m_MetaKeywords.Clear();
            foreach (var metaKeyword in metaKeywords)
            {
                m_MetaKeywords.Add(metaKeyword);
            }
            m_SerializableMetaKeywords = null;
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
