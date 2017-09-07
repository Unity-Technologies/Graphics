using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.MaterialGraph.Drawing;
using UnityEditor.MaterialGraph.Drawing.Inspector;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.MaterialGraph.Drawing
{
    // TODO JOCE: Maybe this needs to derive from something we already have?
    public class GraphEditorView : DataWatchContainer
    {
        GraphView m_GraphView;
        GraphInspectorView m_GraphInspectorView;

        public GraphView graphView
        {
            get { return m_GraphView; }
        }

        TitleBarView m_TitleBarView;

        public GraphEditorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");

            m_TitleBarView = new TitleBarView { name = "TitleBar" };
            Add(m_TitleBarView);

            var content = new VisualElement();
            content.name = "content";
            {
                m_GraphView = new MaterialGraphView { name = "GraphView" };
                m_GraphInspectorView = new GraphInspectorView() { name = "inspector" };
                content.Add(m_GraphView);
                content.Add(m_GraphInspectorView);
            }
            Add(content);
        }

        public override void OnDataChanged()
        {
            m_GraphView.presenter = m_Presenter.graphPresenter;
            m_TitleBarView.dataProvider = m_Presenter.titleBarPresenter;
            m_GraphInspectorView.presenter = m_Presenter.graphInspectorPresenter;
        }

        GraphEditorPresenter m_Presenter;

        public GraphEditorPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (m_Presenter == value)
                    return;

                RemoveWatch();
                m_Presenter = value;
                OnDataChanged();
                AddWatch();
            }
        }

        protected override Object[] toWatch
        {
            get { return new Object[] { m_Presenter }; }
        }
    }
}
