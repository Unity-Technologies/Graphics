using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public abstract class AbstractGraphInspector : Editor
    {
        private ScriptableObjectFactory<INode, AbstractNodeInspector, BasicNodeInspector> m_InspectorFactory;

        private List<INode> m_SelectedNodes = new List<INode>();

        protected IEnumerable<INode> selectedNodes
        {
            get { return m_SelectedNodes; }
        }

        private List<AbstractNodeInspector> m_Inspectors = new List<AbstractNodeInspector>();

        protected IGraphAsset graphAsset
        {
            get { return target as IGraphAsset; }
        }

        protected AbstractGraphInspector(IEnumerable<TypeMapping> typeMappings)
        {
            m_InspectorFactory = new ScriptableObjectFactory<INode, AbstractNodeInspector, BasicNodeInspector>(typeMappings);
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

            using (var nodes = ListPool<INode>.GetDisposable())
            {
                nodes.value.AddRange(graphAsset.drawingData.selection.Select(graphAsset.graph.GetNodeFromGuid));
                if (m_SelectedNodes == null || m_Inspectors.Any(i => i.node == null) || !nodes.value.SequenceEqual(m_SelectedNodes))
                    OnSelectionChanged(nodes.value);
            }
        }

        protected virtual void OnSelectionChanged(IEnumerable<INode> selectedNodes)
        {
            m_SelectedNodes.Clear();
            m_SelectedNodes.AddRange(selectedNodes);
            m_Inspectors.Clear();
            foreach (var node in m_SelectedNodes.OfType<SerializableNode>())
            {
                var inspector = m_InspectorFactory.Create(node);
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
