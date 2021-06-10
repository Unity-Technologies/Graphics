using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Searcher;

namespace UnityEditor.ShaderGraph
{
    public class SearchWindowAdapter : SearcherAdapter
    {
        readonly VisualTreeAsset m_DefaultItemTemplate;
        public override bool HasDetailsPanel => false;

        public SearchWindowAdapter(string title) : base(title)
        {
            m_DefaultItemTemplate = Resources.Load<VisualTreeAsset>("SearcherItem");
        }
    }

    internal class SearchNodeItem : SearcherItem
    {
        public NodeEntry NodeGUID;

        public SearchNodeItem(string name, NodeEntry nodeGUID, string[] synonyms,
                              string help = " ", List<SearchNodeItem> children = null) : base(name)
        {
            NodeGUID = nodeGUID;
            Synonyms = synonyms;
        }
    }
}
