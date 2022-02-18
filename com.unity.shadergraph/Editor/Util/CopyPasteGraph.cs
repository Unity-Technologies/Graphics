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
    enum CopyPasteGraphSource
    {
        Default,
        Duplicate
    }

    [Serializable]
    sealed class CopyPasteGraph : JsonObject
    {
        CopyPasteGraphSource m_CopyPasteGraphSource;

        [SerializeField]
        List<Edge> m_Edges = new List<Edge>();

        [SerializeField]
        List<JsonData<AbstractMaterialNode>> m_Nodes = new List<JsonData<AbstractMaterialNode>>();

        [SerializeField]
        List<JsonData<GroupData>> m_Groups = new List<JsonData<GroupData>>();

        [SerializeField]
        List<JsonData<StickyNoteData>> m_StickyNotes = new List<JsonData<StickyNoteData>>();

        [SerializeField]
        List<JsonRef<ShaderInput>> m_Inputs = new List<JsonRef<ShaderInput>>();

        [SerializeField]
        List<JsonData<CategoryData>> m_Categories = new List<JsonData<CategoryData>>();

        // The meta properties are properties that are not copied into the target graph
        // but sent along to allow property nodes to still hvae the data from the original
        // property present.
        [SerializeField]
        List<JsonData<AbstractShaderProperty>> m_MetaProperties = new List<JsonData<AbstractShaderProperty>>();

        [SerializeField]
        List<string> m_MetaPropertyIds = new List<string>();

        // The meta keywords are keywords that are required by keyword nodes
        // These are copied into the target graph when there is no collision
        [SerializeField]
        List<JsonData<ShaderKeyword>> m_MetaKeywords = new List<JsonData<ShaderKeyword>>();

        [SerializeField]
        List<string> m_MetaKeywordIds = new List<string>();

        [SerializeField]
        List<JsonData<ShaderDropdown>> m_MetaDropdowns = new List<JsonData<ShaderDropdown>>();

        [SerializeField]
        List<string> m_MetaDropdownIds = new List<string>();

        public CopyPasteGraph() { }

        public CopyPasteGraph(IEnumerable<GroupData> groups,
                              IEnumerable<AbstractMaterialNode> nodes,
                              IEnumerable<Edge> edges,
                              IEnumerable<ShaderInput> inputs,
                              IEnumerable<CategoryData> categories,
                              IEnumerable<AbstractShaderProperty> metaProperties,
                              IEnumerable<ShaderKeyword> metaKeywords,
                              IEnumerable<ShaderDropdown> metaDropdowns,
                              IEnumerable<StickyNoteData> notes,
                              bool keepOutputEdges = false,
                              bool removeOrphanEdges = true,
                              CopyPasteGraphSource copyPasteGraphSource = CopyPasteGraphSource.Default)
        {
            m_CopyPasteGraphSource = copyPasteGraphSource;
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

            if (categories != null)
            {
                foreach (var category in categories)
                    AddCategory(category);
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

            if (metaDropdowns != null)
            {
                foreach (var metaDropdown in metaDropdowns.Distinct())
                    AddMetaDropdown(metaDropdown);
            }

            var distinct = m_Edges.Distinct();
            if (removeOrphanEdges)
            {
                distinct = distinct.Where(edge => nodeSet.Contains(edge.inputSlot.node) || (keepOutputEdges && nodeSet.Contains(edge.outputSlot.node)));
            }
            m_Edges = distinct.ToList();
        }

        public bool IsInputCategorized(ShaderInput shaderInput)
        {
            foreach (var category in categories)
            {
                if (category.IsItemInCategory(shaderInput))
                    return true;
            }

            return false;
        }

        // The only situation in which an input has an identical reference name to another input in a category, while not being the same instance, is if they are duplicates
        public bool IsInputDuplicatedFromCategory(ShaderInput shaderInput, CategoryData inputCategory, GraphData targetGraphData)
        {
            foreach (var child in inputCategory.Children)
            {
                if (child.referenceName.Equals(shaderInput.referenceName, StringComparison.Ordinal) && child.objectId != shaderInput.objectId)
                {
                    return true;
                }
            }

            // Need to check if they share same graph owner as well, if not then we can early out
            bool inputBelongsToTargetGraph = targetGraphData.ContainsInput(shaderInput);
            if (inputBelongsToTargetGraph == false)
                return false;

            return false;
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

        void AddCategory(CategoryData category)
        {
            m_Categories.Add(category);
        }

        void AddMetaProperty(AbstractShaderProperty metaProperty)
        {
            m_MetaProperties.Add(metaProperty);
            m_MetaPropertyIds.Add(metaProperty.objectId);
        }

        void AddMetaKeyword(ShaderKeyword metaKeyword)
        {
            m_MetaKeywords.Add(metaKeyword);
            m_MetaKeywordIds.Add(metaKeyword.objectId);
        }

        void AddMetaDropdown(ShaderDropdown metaDropdown)
        {
            m_MetaDropdowns.Add(metaDropdown);
            m_MetaDropdownIds.Add(metaDropdown.objectId);
        }

        public IEnumerable<T> GetNodes<T>()
        {
            return m_Nodes.SelectValue().OfType<T>();
        }

        public DataValueEnumerable<GroupData> groups => m_Groups.SelectValue();

        public DataValueEnumerable<StickyNoteData> stickyNotes => m_StickyNotes.SelectValue();

        public IEnumerable<Edge> edges
        {
            get { return m_Edges; }
        }

        public RefValueEnumerable<ShaderInput> inputs
        {
            get { return m_Inputs.SelectValue(); }
        }

        public DataValueEnumerable<CategoryData> categories
        {
            get { return m_Categories.SelectValue(); }
        }

        public DataValueEnumerable<AbstractShaderProperty> metaProperties
        {
            get { return m_MetaProperties.SelectValue(); }
        }

        public DataValueEnumerable<ShaderKeyword> metaKeywords
        {
            get { return m_MetaKeywords.SelectValue(); }
        }

        public DataValueEnumerable<ShaderDropdown> metaDropdowns
        {
            get { return m_MetaDropdowns.SelectValue(); }
        }

        public IEnumerable<string> metaPropertyIds => m_MetaPropertyIds;

        public IEnumerable<string> metaKeywordIds => m_MetaKeywordIds;

        public CopyPasteGraphSource copyPasteGraphSource => m_CopyPasteGraphSource;

        public override void OnAfterMultiDeserialize(string json)
        {
            // should we add support for versioning old CopyPasteGraphs from old versions of Unity?
            // so you can copy from old paste to new

            foreach (var node in m_Nodes.SelectValue())
            {
                node.UpdateNodeAfterDeserialization();
                node.SetupSlots();
            }
        }

        internal static CopyPasteGraph FromJson(string copyBuffer, GraphData targetGraph)
        {
            try
            {
                var graph = new CopyPasteGraph();
                MultiJson.Deserialize(graph, copyBuffer, targetGraph, true);
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
