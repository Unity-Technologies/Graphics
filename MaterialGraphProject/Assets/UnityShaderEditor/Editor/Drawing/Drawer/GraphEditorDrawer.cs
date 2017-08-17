using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.MaterialGraph.Drawing;
using UnityEditor.MaterialGraph.Drawing.Views;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.MaterialGraph.Drawing
{
    // TODO JOCE: Maybe this needs to derive from something we already have?
    public class GraphEditorDrawer : DataWatchContainer
    {
        GraphView m_GraphView;
        GraphInspectorView m_GraphInspectorView;

        public GraphView graphView
        {
            get { return m_GraphView; }
        }

        private TitleBarDrawer m_TitleBarDrawer;

        // TODO: Create graphView from here rather than have it passed in through constructor
        public GraphEditorDrawer(GraphView graphView)
        {
            AddStyleSheetPath("Styles/MaterialGraph");

            m_GraphView = graphView;
            m_GraphView.name = "GraphView";
            m_TitleBarDrawer = new TitleBarDrawer();
            m_TitleBarDrawer.name = "TitleBar";

            m_GraphInspectorView = new GraphInspectorView();

            Add(m_TitleBarDrawer);
            var contentContainer = new VisualElement() { m_GraphView, m_GraphInspectorView };
            contentContainer.name = "content";
            Add(contentContainer);
        }

        public override void OnDataChanged()
        {
            m_GraphView.presenter = m_Presenter;
            m_TitleBarDrawer.dataProvider = m_Presenter.titleBar;
            m_GraphInspectorView.presenter = m_Presenter.graphInspectorPresenter;
        }

        MaterialGraphPresenter m_Presenter;

        public MaterialGraphPresenter presenter
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
            get { return new Object[]{m_Presenter}; }
        }
    }
}
