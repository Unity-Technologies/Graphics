using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class GraphInspectorPresenter : ScriptableObject
    {
        public IGraph graph { get; set; }

        public string assetName { get; set; }

        public List<INode> selectedNodes { get; set; }

        [Flags]
        public enum ChangeType
        {
            Graph = 1 << 0,
            SelectedNodes = 1 << 1,
            AssetName = 1 << 2,
            All = -1
        }

        public delegate void OnChange(ChangeType changeType);

        public OnChange onChange;

        public void Initialize(string assetName, IGraph graph)
        {
            this.graph = graph;
            this.assetName = assetName;
            selectedNodes = new List<INode>();

            if (onChange != null)
                onChange(ChangeType.Graph | ChangeType.SelectedNodes | ChangeType.AssetName);
        }

        public void UpdateSelection(IEnumerable<INode> nodes)
        {
            selectedNodes.Clear();
            selectedNodes.AddRange(nodes);

            if (onChange != null)
                onChange(ChangeType.SelectedNodes);
        }
    }
}
