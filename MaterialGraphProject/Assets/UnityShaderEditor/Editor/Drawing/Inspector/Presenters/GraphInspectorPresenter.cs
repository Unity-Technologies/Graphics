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
        List<AbstractNodeInspector> m_Inspectors;

        [SerializeField]
        List<AbstractNodeEditorPresenter> m_Editors;

        [SerializeField]
        string m_Title;

        public List<AbstractNodeInspector> inspectors
        {
            get { return m_Inspectors; }
            set { m_Inspectors = value; }
        }

        public List<AbstractNodeEditorPresenter> editors
        {
            get { return m_Editors; }
        }

        public string title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        TypeMapper<INode, AbstractNodeInspector> m_InspectorMapper;

        public void Initialize(string graphName)
        {
            inspectors = new List<AbstractNodeInspector>();
            m_Editors = new List<AbstractNodeEditorPresenter>();
            m_Title = graphName;
            m_InspectorMapper = new TypeMapper<INode, AbstractNodeInspector>(typeof(BasicNodeInspector))
            {
                {typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeInspector)},
                {typeof(PropertyNode), typeof(PropertyNodeInspector)},
                {typeof(SubGraphInputNode), typeof(SubgraphInputNodeInspector)},
                {typeof(SubGraphOutputNode), typeof(SubgraphOutputNodeInspector)}
            };
        }

        public void UpdateSelection(IEnumerable<INode> nodes)
        {
            m_Inspectors.Clear();
            editors.Clear();
            foreach (var node in nodes.OfType<SerializableNode>())
            {
                var inspector = (AbstractNodeInspector) CreateInstance(m_InspectorMapper.MapType(node.GetType()));
                inspector.Initialize(node);
                m_Inspectors.Add(inspector);

                var editor = CreateInstance<StandardNodeEditorPresenter>();
                editor.Initialize(node);
                editors.Add(editor);
            }
        }
    }
}
