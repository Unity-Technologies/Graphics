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

namespace UnityEditor.ShaderGraph
{
    public class SearchWindowAdapter : SearcherAdapter
    {
        readonly VisualTreeAsset m_DefaultItemTemplate;
        public override string Title { get; }
        public override bool HasDetailsPanel => false;

        Label m_DetailsLabel;

        public SearchWindowAdapter(string title) : base("Create Node")
        {
            Title = title;
            m_DefaultItemTemplate = Resources.Load<VisualTreeAsset>("SearcherItem");
        }

    }
    
}

