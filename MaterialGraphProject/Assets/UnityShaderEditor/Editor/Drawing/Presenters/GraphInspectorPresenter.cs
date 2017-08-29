using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEditor.MaterialGraph.Drawing.Inspector;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
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

        ScriptableObjectFactory<INode, AbstractNodeInspector, BasicNodeInspector> m_InspectorFactory;

        public void Initialize()
        {
            inspectors = new List<AbstractNodeInspector>();
            m_Editors = new List<AbstractNodeEditorPresenter>();
            m_Title = "Name of graph";
            m_InspectorFactory = new ScriptableObjectFactory<INode, AbstractNodeInspector, BasicNodeInspector>(new[]
            {
                new TypeMapping(typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeInspector)),
                new TypeMapping(typeof(PropertyNode), typeof(PropertyNodeInspector)),
                new TypeMapping(typeof(SubGraphInputNode), typeof(SubgraphInputNodeInspector)),
                new TypeMapping(typeof(SubGraphOutputNode), typeof(SubgraphOutputNodeInspector))
            });
        }

        public void UpdateSelection(IEnumerable<INode> nodes)
        {
            m_Inspectors.Clear();
            editors.Clear();
            foreach (var node in nodes.OfType<SerializableNode>())
            {
                var inspector = m_InspectorFactory.Create(node);
                inspector.Initialize(node);
                m_Inspectors.Add(inspector);

                var editor = CreateInstance<StandardNodeEditorPresenter>();
                editor.Initialize(node);
                editors.Add(editor);
            }
        }
    }
}
