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
        PreviewData m_PreviewHandle;

        public string assetName { get; set; }

        public List<INode> selectedNodes { get; set; }

        public Texture previewTexture { get; private set; }

        [SerializeField]
        private int m_Version;

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

        [SerializeField]
        private HelperMaterialGraphEditWindow m_Owner;

        public AbstractMaterialGraph graph
        {
            get { return m_Owner.GetMaterialGraph(); }
        }

        public void Dirty()
        {
            m_Version++;
        }

        public void Initialize(string assetName, PreviewSystem previewSystem, HelperMaterialGraphEditWindow window)
        {
            m_Owner = window;
            var masterNode = graph.GetNodes<MasterNode>().FirstOrDefault();
            if (masterNode != null)
            {
                m_PreviewHandle = previewSystem.GetPreview(masterNode);
                m_PreviewHandle.onPreviewChanged += OnPreviewChanged;
            }
            this.assetName = assetName;
            selectedNodes = new List<INode>();

            NotifyChange(ChangeType.Graph | ChangeType.SelectedNodes | ChangeType.AssetName);
        }

        void OnPreviewChanged()
        {
            previewTexture = m_PreviewHandle.texture;
            NotifyChange(ChangeType.PreviewTexture);
        }

        public void UpdateSelection(IEnumerable<INode> nodes)
        {
            selectedNodes.Clear();
            selectedNodes.AddRange(nodes);

            NotifyChange(ChangeType.SelectedNodes);
        }

        void NotifyChange(ChangeType changeType)
        {
            if (onChange != null)
                onChange(changeType);
        }

        public void Dispose()
        {
            if (m_PreviewHandle != null)
            {
                m_PreviewHandle.onPreviewChanged -= OnPreviewChanged;
                m_PreviewHandle = null;
            };
        }
    }
}
