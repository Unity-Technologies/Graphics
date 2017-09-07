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
        List<AbstractNodeEditorPresenter> m_Editors;

        [SerializeField]
        string m_Title;

        public List<AbstractNodeEditorPresenter> editors
        {
            get { return m_Editors; }
        }

        public string title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        TypeMapper m_TypeMapper;

        public void Initialize(string graphName)
        {
            m_Editors = new List<AbstractNodeEditorPresenter>();
            m_Title = graphName;
            m_TypeMapper = new TypeMapper(typeof(INode), typeof(AbstractNodeEditorPresenter), typeof(StandardNodeEditorPresenter));
//            m_InspectorMapper = new TypeMapper<INode, AbstractNodeInspector>(typeof(BasicNodeInspector))
//            {
//                {typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeInspector)},
//                {typeof(PropertyNode), typeof(PropertyNodeInspector)},
//                {typeof(SubGraphInputNode), typeof(SubgraphInputNodeInspector)},
//                {typeof(SubGraphOutputNode), typeof(SubgraphOutputNodeInspector)}
//            };
        }

        public void UpdateSelection(IEnumerable<INode> nodes)
        {
            editors.Clear();
            foreach (var node in nodes.OfType<SerializableNode>())
            {
                var editor = (AbstractNodeEditorPresenter) CreateInstance(m_TypeMapper.MapType(node.GetType()));
                editor.Initialize(node);
                editors.Add(editor);
            }
        }
    }
}
