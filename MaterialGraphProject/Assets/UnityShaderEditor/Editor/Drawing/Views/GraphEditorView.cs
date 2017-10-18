using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.MaterialGraph.Drawing;
using UnityEditor.MaterialGraph.Drawing.Inspector;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class GraphEditorView : VisualElement, IDisposable
    {
        GraphView m_GraphView;
        GraphInspectorView m_GraphInspectorView;
        ToolbarView m_ToolbarView;
        ToolbarButtonView m_TimeButton;

        PreviewSystem m_PreviewSystem;

        [SerializeField]
        MaterialGraphPresenter m_GraphPresenter;

        public Action onUpdateAssetClick { get; set; }
        public Action onConvertToSubgraphClick { get; set; }
        public Action onShowInProjectClick { get; set; }

        public GraphView graphView
        {
            get { return m_GraphView; }
        }

        public PreviewRate previewRate
        {
            get { return previewSystem.previewRate; }
            set { previewSystem.previewRate = value; }
        }

        public MaterialGraphPresenter graphPresenter
        {
            get { return m_GraphPresenter; }
            set { m_GraphPresenter = value; }
        }

        public PreviewSystem previewSystem
        {
            get { return m_PreviewSystem; }
            set { m_PreviewSystem = value; }
        }

        public GraphEditorView(AbstractMaterialGraph graph, HelperMaterialGraphEditWindow container, string assetName)
        {
            AddStyleSheetPath("Styles/MaterialGraph");

            previewSystem = new PreviewSystem(graph);

            m_GraphPresenter = ScriptableObject.CreateInstance<MaterialGraphPresenter>();
            m_GraphPresenter.Initialize(graph, container, previewSystem);

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

                m_TimeButton = new ToolbarButtonView { text = "Preview rate: " + previewRate };
                m_TimeButton.AddManipulator(new Clickable(() =>
                {
                    if (previewRate == PreviewRate.Full)
                        previewRate = PreviewRate.Throttled;
                    else if (previewRate == PreviewRate.Throttled)
                        previewRate = PreviewRate.Off;
                    else if (previewRate == PreviewRate.Off)
                        previewRate = PreviewRate.Full;
                    m_TimeButton.text = "Preview rate: " + previewRate;
                }));
                m_ToolbarView.Add(m_TimeButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());
            }
            Add(m_ToolbarView);

            var content = new VisualElement();
            content.name = "content";
            {
                m_GraphView = new MaterialGraphView { name = "GraphView", presenter = m_GraphPresenter };
                m_GraphInspectorView = new GraphInspectorView(assetName, previewSystem, graph) { name = "inspector" };
                m_GraphPresenter.onSelectionChanged += m_GraphInspectorView.UpdateSelection;
                content.Add(m_GraphView);
                content.Add(m_GraphInspectorView);
            }
            Add(content);
        }

        public void Dispose()
        {
            onUpdateAssetClick = null;
            onConvertToSubgraphClick = null;
            onShowInProjectClick = null;
            if (m_GraphInspectorView != null) m_GraphInspectorView.Dispose();
            if (previewSystem != null)
            {
                previewSystem.Dispose();
                previewSystem = null;
            }
        }
    }
}
