using System;
using RMGUI.GraphView;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
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
        public GraphEditorDrawer(GraphView graphView, AbstractGraphPresenter presenter)
        {
            m_GraphView = graphView;
            m_GraphView.name = "GraphView";
            m_TitleBarDrawer = new TitleBarDrawer(presenter.titleBar);
            m_TitleBarDrawer.name = "TitleBar";

            AddChild(m_TitleBarDrawer);
            AddChild(m_GraphView);

            this.presenter = presenter;

            AddStyleSheetPath("Styles/GraphEditor");
        }

        public override void OnDataChanged()
        {
            m_GraphView.presenter = m_Presenter;
            m_TitleBarDrawer.dataProvider = m_Presenter.titleBar;
        }

        private AbstractGraphPresenter m_Presenter;

        public AbstractGraphPresenter presenter
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

        protected override object toWatch
        {
            get { return m_Presenter; }
        }
    }
}
