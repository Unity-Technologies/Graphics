using System;
using RMGUI.GraphView;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    // TODO JOCE Remove all traces of dataSource

    // TODO JOCE: Maybe this needs to dereive from something we already have?
    [StyleSheet("Assets/GraphFramework/SerializableGraph/Editor/Drawing/Styles/GraphEditor.uss")]
    public class GraphEditorDrawer : DataWatchContainer
    {
        private GraphView m_GraphView;

        public GraphView GraphView
        {
            get { return m_GraphView; }
        }

        private TitleBarDrawer m_TitleBarDrawer;

        // TODO: Create graphView from here rather than have it passed in through constructor
        public GraphEditorDrawer(GraphView graphView, AbstractGraphDataSource dataSource)
        {
            m_GraphView = graphView;
            m_GraphView.name = "GraphView";
            m_TitleBarDrawer = new TitleBarDrawer(dataSource.titleBar);
            m_TitleBarDrawer.name = "TitleBar";

            AddChild(m_TitleBarDrawer);
            AddChild(m_GraphView);

            this.dataSource = dataSource;
        }

        public override void OnDataChanged()
        {
            m_GraphView.presenter = m_DataSource;
            m_TitleBarDrawer.dataProvider = m_DataSource.titleBar;
        }

        private AbstractGraphDataSource m_DataSource;

        public AbstractGraphDataSource dataSource
        {
            get { return m_DataSource; }
            set
            {
                if (m_DataSource == value)
                    return;

                RemoveWatch();
                m_DataSource = value;
                OnDataChanged();
                AddWatch();
            }
        }

        protected override object toWatch
        {
            get { return m_DataSource; }
        }
    }
}
