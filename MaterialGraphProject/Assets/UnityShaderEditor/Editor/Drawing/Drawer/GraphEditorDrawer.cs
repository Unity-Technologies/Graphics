using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.MaterialGraph.Drawing;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    // TODO JOCE: Maybe this needs to derive from something we already have?
    public class GraphEditorDrawer : DataWatchContainer
    {
        private GraphView m_GraphView;

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

            Add(m_TitleBarDrawer);
            Add(m_GraphView);
        }

        public override void OnDataChanged()
        {
            m_GraphView.presenter = m_Presenter;
            m_TitleBarDrawer.dataProvider = m_Presenter.titleBar;
        }

        private MaterialGraphPresenter m_Presenter;

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
