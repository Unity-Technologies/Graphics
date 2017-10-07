using System;
using UnityEditor.MaterialGraph.Drawing.Inspector;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class GraphEditorPresenter : ScriptableObject, IDisposable
    {
        [SerializeField]
        MaterialGraphPresenter m_GraphPresenter;

        [SerializeField]
        GraphInspectorPresenter m_GraphInspectorPresenter;

        PreviewSystem m_PreviewSystem;

        public PreviewRate previewRate
        {
            get { return m_PreviewSystem.previewRate; }
            set { m_PreviewSystem.previewRate = value; }
        }

        public MaterialGraphPresenter graphPresenter
        {
            get { return m_GraphPresenter; }
            set { m_GraphPresenter = value; }
        }

        public GraphInspectorPresenter graphInspectorPresenter
        {
            get { return m_GraphInspectorPresenter; }
            set { m_GraphInspectorPresenter = value; }
        }

        public void Initialize(AbstractMaterialGraph graph, HelperMaterialGraphEditWindow container, string assetName)
        {
            m_PreviewSystem = new PreviewSystem(graph);

            m_GraphInspectorPresenter = CreateInstance<GraphInspectorPresenter>();
            m_GraphInspectorPresenter.Initialize(assetName, m_PreviewSystem, container);

            m_GraphPresenter = CreateInstance<MaterialGraphPresenter>();
            m_GraphPresenter.Initialize(graph, container, m_PreviewSystem);
            m_GraphPresenter.onSelectionChanged += m_GraphInspectorPresenter.UpdateSelection;
        }

        public void UpdatePreviews()
        {
            m_PreviewSystem.Update();
        }

        public void Dispose()
        {
            if (m_GraphInspectorPresenter != null)
            {
                m_GraphInspectorPresenter.Dispose();
                m_GraphInspectorPresenter = null;
            }
            if (m_PreviewSystem != null)
            {
                m_PreviewSystem.Dispose();
                m_PreviewSystem = null;
            }
        }
    }
}
