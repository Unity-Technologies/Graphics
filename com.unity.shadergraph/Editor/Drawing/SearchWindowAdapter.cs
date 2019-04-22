using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    public class SearchWindowAdapter : SearcherAdapter
    {
        readonly VisualTreeAsset m_DefaultItemTemplate;
        public override bool HasDetailsPanel => false;

        Label m_DetailsLabel;

        public SearchWindowAdapter(string title) : base(title)
        {
            m_DefaultItemTemplate = Resources.Load<VisualTreeAsset>("SearcherItem");
        }

        
    }

    internal class SearchNodeItem : SearcherItem
    {
        public NodeEntry UserData;
        //public new SearchNodeItem Parent;
        //public new List<SearchNodeItem> Children;

        public SearchNodeItem(string name, NodeEntry userData, string help = " ", List<SearchNodeItem> children = null) : base(name)
        {
            UserData = userData;
            
        }

        
    }
    
}

