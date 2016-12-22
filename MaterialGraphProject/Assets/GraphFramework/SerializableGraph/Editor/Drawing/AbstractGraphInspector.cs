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

        [SerializeField] private List<AbstractNodeInspector> m_Inspectors = new List<AbstractNodeInspector>();

        protected abstract void AddTypeMappings(Action<Type, Type> map);

        public override void OnInspectorGUI()
        {
            UpdateInspectors();

            foreach (var inspector in m_Inspectors)
            {
                inspector.OnInspectorGUI();
            }
        }

        private void UpdateInspectors()
        {
            var asset = target as IGraphAsset;
            if (asset == null)
                return;

            var selectedNodes = asset.drawingData.selection.Select(asset.graph.GetNodeFromGuid).ToList();
            if (m_Inspectors.All(i => i.node != null) && selectedNodes.Select(n => n.guid).SequenceEqual(m_Inspectors.Select(i => i.nodeGuid)))
                return;

            m_Inspectors.Clear();
            foreach (var node in selectedNodes.OfType<SerializableNode>())
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

        public void OnEnable()
        {
            m_DataMapper.Clear();
            AddTypeMappings(m_DataMapper.AddMapping);
        }
    }
}
