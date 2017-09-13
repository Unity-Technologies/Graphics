using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEditor.MaterialGraph.Drawing.Inspector;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class GraphInspectorPresenter : ScriptableObject
    {
        [SerializeField]
        AbstractNodeEditorPresenter m_Editor;

        [SerializeField]
        string m_Title;

        [SerializeField]
        int m_SelectionCount;

        TypeMapper m_TypeMapper;
       
        [SerializeField]
        private int version;

        public AbstractMaterialGraph graph { get; private set; }

        public AbstractNodeEditorPresenter editor
        {
            get { return m_Editor; }
        }

        public string title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        public int selectionCount
        {
            get { return m_SelectionCount; }
            set { m_SelectionCount = value; }
        }
        
        public void Dirty()
        {
            version++;
        }


         public void Initialize(AbstractMaterialGraph igraph, string graphName)
        {
            graph = igraph;
            m_Title = graphName;
            m_TypeMapper = new TypeMapper(typeof(INode), typeof(AbstractNodeEditorPresenter), typeof(StandardNodeEditorPresenter))
            {
                {typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeEditorPresenter)}
            };
            // Nodes missing custom editors:
            // - PropertyNode
            // - SubGraphInputNode
            // - SubGraphOutputNode
        }

        public void UpdateSelection(IEnumerable<INode> nodes)
        {
            using (var nodesList = ListPool<INode>.GetDisposable())
            {
                nodesList.value.AddRange(nodes);

                m_SelectionCount = nodesList.value.Count;

                if (m_SelectionCount == 1)
                {
                    var node = nodesList.value.First();
                    if (m_Editor == null || node != m_Editor.node)
                    {
                        m_Editor = (AbstractNodeEditorPresenter) CreateInstance(m_TypeMapper.MapType(node.GetType()));
                        m_Editor.node = node;
                    }
                }
                else
                {
                    m_Editor = null;
                }
            }
        }
    }
}
