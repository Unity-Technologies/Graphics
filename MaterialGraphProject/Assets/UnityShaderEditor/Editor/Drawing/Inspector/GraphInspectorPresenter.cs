using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class GraphInspectorPresenter : ScriptableObject, IDisposable
    {
        PreviewHandle m_PreviewHandle;

        public IGraph graph { get; set; }

        public string assetName { get; set; }

        public List<INode> selectedNodes { get; set; }

        public Texture previewTexture { get; private set; }

        [Flags]
        public enum ChangeType
        {
            Graph = 1 << 0,
            SelectedNodes = 1 << 1,
            AssetName = 1 << 2,
            PreviewTexture = 1 << 3,
            All = -1
        }

        public delegate void OnChange(ChangeType changeType);

        public OnChange onChange;

        public void Initialize(string assetName, IGraph graph, PreviewSystem previewSystem)
        {
            var masterNode = graph.GetNodes<AbstractMasterNode>().FirstOrDefault();
            if (masterNode != null)
            {
                m_PreviewHandle = previewSystem.GetPreviewHandle(masterNode.guid);
                m_PreviewHandle.onPreviewChanged += OnPreviewChanged;
            }
            this.graph = graph;
            this.assetName = assetName;
            selectedNodes = new List<INode>();

            Change(ChangeType.Graph | ChangeType.SelectedNodes | ChangeType.AssetName);
        }

        void OnPreviewChanged()
        {
            previewTexture = m_PreviewHandle.texture;
            Change(ChangeType.PreviewTexture);
        }

        public void UpdateSelection(IEnumerable<INode> nodes)
        {
            selectedNodes.Clear();
            selectedNodes.AddRange(nodes);

            Change(ChangeType.SelectedNodes);
        }

        void Change(ChangeType changeType)
        {
            if (onChange != null)
                onChange(changeType);
        }

        public void Dispose()
        {
            if (m_PreviewHandle != null)
            {
                m_PreviewHandle.Dispose();
                m_PreviewHandle = null;
            };
        }
    }
}
