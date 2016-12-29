using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public abstract class AbstractGraphInspector : Editor
    {
        private readonly TypeMapper m_DataMapper = new TypeMapper(typeof(BasicNodeInspector));

        protected List<INode> m_SelectedNodes = new List<INode>();

        protected List<AbstractNodeInspector> m_Inspectors = new List<AbstractNodeInspector>();

        protected IGraphAsset m_GraphAsset;

        protected abstract void AddTypeMappings(Action<Type, Type> map);

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
            if (m_GraphAsset == null)
                return;

            using (var selectedNodes = ListPool<INode>.GetDisposable())
            {
                selectedNodes.value.AddRange(m_GraphAsset.drawingData.selection.Select(m_GraphAsset.graph.GetNodeFromGuid));
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
                var inspector = CreateInspector(node);
                inspector.Initialize(node);
                m_Inspectors.Add(inspector);
            }
        }

        private AbstractNodeInspector CreateInspector(INode node)
        {
            var type = m_DataMapper.MapType(node.GetType());
            return CreateInstance(type) as AbstractNodeInspector;
        }

        public virtual void OnEnable()
        {
            m_GraphAsset = target as IGraphAsset;
            m_DataMapper.Clear();
            AddTypeMappings(m_DataMapper.AddMapping);
            UpdateSelection();
        }
    }
}
