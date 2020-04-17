using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    sealed class CopyPasteGraph : JsonObject
    {
        [SerializeField]
        List<Edge> m_Edges = new List<Edge>();

        [SerializeField]
        List<JsonData<AbstractMaterialNode>> m_Nodes = new List<JsonData<AbstractMaterialNode>>();

        // TODO: Upgrade when GroupData is upgraded to JsonObject
        [SerializeField]
        List<GroupData> m_Groups = new List<GroupData>();

        // TODO: Upgrade when StickyNoteData is upgraded to JsonObject
        [SerializeField]
        List<StickyNoteData> m_StickyNotes = new List<StickyNoteData>();

        [SerializeField]
        List<JsonData<ShaderInput>> m_Inputs = new List<JsonData<ShaderInput>>();

        // The meta properties are properties that are not copied into the target graph
        // but sent along to allow property nodes to still hvae the data from the original
        // property present.
        [SerializeField]
        List<JsonData<AbstractShaderProperty>> m_MetaProperties = new List<JsonData<AbstractShaderProperty>>();

        // The meta keywords are keywords that are required by keyword nodes
        // These are copied into the target graph when there is no collision
        [SerializeField]
        List<JsonData<ShaderKeyword>> m_MetaKeywords = new List<JsonData<ShaderKeyword>>();

        [SerializeField]
        string m_SourceGraphGuid;

        public CopyPasteGraph() {}

        public CopyPasteGraph(string sourceGraphGuid, IEnumerable<GroupData> groups, IEnumerable<AbstractMaterialNode> nodes, IEnumerable<Edge> edges,
            IEnumerable<ShaderInput> inputs, IEnumerable<AbstractShaderProperty> metaProperties, IEnumerable<ShaderKeyword> metaKeywords, IEnumerable<StickyNoteData> notes, bool keepOutputEdges = false)
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

            var nodeSet = new HashSet<AbstractMaterialNode>();

            if (nodes != null)
            {
                foreach (var node in nodes.Distinct())
                {
                    if (!node.canCopyNode)
                    {
                        throw new InvalidOperationException($"Cannot copy node {node.name} ({node.objectId}).");
                    }

                    nodeSet.Add(node);
                    AddNode(node);
                    foreach (var edge in NodeUtils.GetAllEdges(node))
                        AddEdge((Edge)edge);
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
                foreach (var metaProperty in metaProperties.Distinct())
                    AddMetaProperty(metaProperty);
            }

            if (metaKeywords != null)
            {
                foreach (var metaKeyword in metaKeywords.Distinct())
                    AddMetaKeyword(metaKeyword);
            }

            m_Edges = m_Edges
                .Distinct()
                .Where(edge => nodeSet.Contains(edge.inputSlot.node) || (keepOutputEdges && nodeSet.Contains(edge.outputSlot.node)))
                .ToList();
        }

        void AddGroup(GroupData group)
        {
            m_Groups.Add(group);
        }

        void AddNote(StickyNoteData stickyNote)
        {
            m_StickyNotes.Add(stickyNote);
        }

        void AddNode(AbstractMaterialNode node)
        {
            m_Nodes.Add(node);
        }

        void AddEdge(Edge edge)
        {
            m_Edges.Add(edge);
        }

        void AddInput(ShaderInput input)
        {
            m_Inputs.Add(input);
        }

        void AddMetaProperty(AbstractShaderProperty metaProperty)
        {
            m_MetaProperties.Add(metaProperty);
        }

        void AddMetaKeyword(ShaderKeyword metaKeyword)
        {
            m_MetaKeywords.Add(metaKeyword);
        }

        public IEnumerable<T> GetNodes<T>()
        {
            return m_Nodes.SelectValue().OfType<T>();
        }

        public IEnumerable<GroupData> groups
        {
            get { return m_Groups; }
        }

        public IEnumerable<StickyNoteData> stickyNotes => m_StickyNotes;

        public IEnumerable<Edge> edges
        {
            get { return m_Edges; }
        }

        public DataValueEnumerable<ShaderInput> inputs
        {
            get { return m_Inputs.SelectValue(); }
        }

        public DataValueEnumerable<AbstractShaderProperty> metaProperties
        {
            get { return m_MetaProperties.SelectValue(); }
        }

        public DataValueEnumerable<ShaderKeyword> metaKeywords
        {
            get { return m_MetaKeywords.SelectValue(); }
        }

        public string sourceGraphGuid
        {
            get { return m_SourceGraphGuid; }
        }

        internal static CopyPasteGraph FromJson(string copyBuffer, GraphData targetGraph)
        {
            try
            {
                var graph = new CopyPasteGraph();
                MultiJson.Deserialize(graph, copyBuffer, targetGraph);
                return graph;
            }
            catch
            {
                // ignored. just means copy buffer was not a graph :(
                return null;
            }
        }
    }
}
