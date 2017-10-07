using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.MaterialGraph.Drawing;
using UnityEditor.MaterialGraph.Drawing.Inspector;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class GraphEditorView : DataWatchContainer, IDisposable
    {
        GraphView m_GraphView;
        GraphInspectorView m_GraphInspectorView;

        public GraphView graphView
        {
            get { return m_GraphView; }
        }

        ToolbarView m_ToolbarView;

        public Action onUpdateAssetClick { get; set; }
        public Action onConvertToSubgraphClick { get; set; }
        public Action onShowInProjectClick { get; set; }

        public GraphEditorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");

            m_ToolbarView = new ToolbarView { name = "TitleBar" };
            {
                m_ToolbarView.Add(new ToolbarSpaceView());
                m_ToolbarView.Add(new ToolbarSeparatorView());

                var updateAssetButton = new ToolbarButtonView { text = "Update asset" };
                updateAssetButton.AddManipulator(new Clickable(() =>
                {
                    if (onUpdateAssetClick != null) onUpdateAssetClick();
                }));
                m_ToolbarView.Add(updateAssetButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());
                m_ToolbarView.Add(new ToolbarSpaceView());
                m_ToolbarView.Add(new ToolbarSeparatorView());

                var convertToSubgraphButton = new ToolbarButtonView { text = "Convert to subgraph" };
                convertToSubgraphButton.AddManipulator(new Clickable(() =>
                {
                    if (onConvertToSubgraphClick != null) onConvertToSubgraphClick();
                }));
                m_ToolbarView.Add(convertToSubgraphButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());
                m_ToolbarView.Add(new ToolbarSpaceView());
                m_ToolbarView.Add(new ToolbarSeparatorView());

                var showInProjectButton = new ToolbarButtonView { text = "Show in project" };
                showInProjectButton.AddManipulator(new Clickable(() =>
                {
                    if (onShowInProjectClick != null) onShowInProjectClick();
                }));
                m_ToolbarView.Add(showInProjectButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());
                m_ToolbarView.Add(new ToolbarSpaceView());
                m_ToolbarView.Add(new ToolbarSeparatorView());

                m_TimeButton = new ToolbarButtonView { text = "" };
                m_TimeButton.AddManipulator(new Clickable(() =>
                {
                    if (presenter == null)
                        return;
                    if (presenter.previewRate == PreviewRate.Full)
                        presenter.previewRate = PreviewRate.Throttled;
                    else if (presenter.previewRate == PreviewRate.Throttled)
                        presenter.previewRate = PreviewRate.Off;
                    else if (presenter.previewRate == PreviewRate.Off)
                        presenter.previewRate = PreviewRate.Full;
                    m_TimeButton.text = "Preview rate: " + presenter.previewRate;
                }));
                m_ToolbarView.Add(m_TimeButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());
            }
            Add(m_ToolbarView);

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
            m_GraphInspectorView.presenter = m_Presenter.graphInspectorPresenter;
            m_TimeButton.text = "Preview rate: " + presenter.previewRate;
        }

        GraphEditorPresenter m_Presenter;
        ToolbarButtonView m_TimeButton;

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

        public void Dispose()
        {
            onUpdateAssetClick = null;
            onConvertToSubgraphClick = null;
            onShowInProjectClick = null;
            if (m_GraphInspectorView != null) m_GraphInspectorView.Dispose();
        }
    }
}
