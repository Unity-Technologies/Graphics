using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public abstract class AbstractGraphInspector : Editor
    {
        protected GraphTypeMapper typeMapper { get; set; }

        protected List<INode> m_SelectedNodes = new List<INode>();

        protected List<AbstractNodeInspector> m_Inspectors = new List<AbstractNodeInspector>();

        protected IGraphAsset graphAsset
        {
            get { return target as IGraphAsset; }
        }

        protected AbstractGraphInspector()
        {
            typeMapper = new GraphTypeMapper(typeof(BasicNodeInspector));
        }

        public override void OnInspectorGUI()
        {
            UpdateSelection();

            foreach (var inspector in m_Inspectors)
            {
                inspector.OnInspectorGUI();
            }
        }

        private void UpdateSelection()
        {
            if (graphAsset == null)
                return;

            using (var selectedNodes = ListPool<INode>.GetDisposable())
            {
                selectedNodes.value.AddRange(graphAsset.drawingData.selection.Select(graphAsset.graph.GetNodeFromGuid));
                if (m_SelectedNodes == null || m_Inspectors.Any(i => i.node == null) || !selectedNodes.value.SequenceEqual(m_SelectedNodes))
                    OnSelectionChanged(selectedNodes.value);
            }
        }

        protected virtual void OnSelectionChanged(IEnumerable<INode> selectedNodes)
        {
            m_SelectedNodes.Clear();
            m_SelectedNodes.AddRange(selectedNodes);
            m_Inspectors.Clear();
            foreach (var node in m_SelectedNodes.OfType<SerializableNode>())
            {
                var inspector = (AbstractNodeInspector)typeMapper.Create(node);
                inspector.Initialize(node);
                m_Inspectors.Add(inspector);
            }
        }

        public virtual void OnEnable()
        {
            UpdateSelection();
        }
    }
}
