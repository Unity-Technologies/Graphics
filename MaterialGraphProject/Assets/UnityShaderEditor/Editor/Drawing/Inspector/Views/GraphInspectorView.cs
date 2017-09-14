using System;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class GraphInspectorView : VisualElement, IDisposable
    {
        [SerializeField]
        GraphInspectorPresenter m_Presenter;

        int m_SelectionHash;

        VisualElement m_Title;
        VisualElement m_ContentContainer;
        AbstractNodeEditorView m_EditorView;

        TypeMapper m_TypeMapper;

        public GraphInspectorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");

            var headerContainer = new VisualElement { name = "header" };
            {
                m_Title = new VisualElement() { name = "title" };
                headerContainer.Add(m_Title);
            }
            Add(headerContainer);

            m_ContentContainer = new VisualElement { name = "contentContainer" };
            Add(m_ContentContainer);

            // Nodes missing custom editors:
            // - PropertyNode
            // - SubGraphInputNode
            // - SubGraphOutputNode
            m_TypeMapper = new TypeMapper(typeof(INode), typeof(AbstractNodeEditorView), typeof(StandardNodeEditorView))
            {
                { typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeEditorView) }
            };
        }

        public void OnChange(GraphInspectorPresenter.ChangeType changeType)
        {
            if (presenter == null)
            {
                m_ContentContainer.Clear();
                m_SelectionHash = 0;
                return;
            }

            if ((changeType & GraphInspectorPresenter.ChangeType.AssetName) != 0)
                m_Title.text = presenter.assetName;

            if ((changeType & GraphInspectorPresenter.ChangeType.SelectedNodes) != 0)
            {
                var selectionHash = UIUtilities.GetHashCode(presenter.selectedNodes.Count, presenter.selectedNodes != null ? presenter.selectedNodes.FirstOrDefault() : null);
                if (selectionHash != m_SelectionHash)
                {
                    m_SelectionHash = selectionHash;
                    m_ContentContainer.Clear();
                    if (presenter.selectedNodes.Count > 1)
                    {
                        var element = new VisualElement { name = "selectionCount", text = string.Format("{0} nodes selected.", presenter.selectedNodes.Count) };
                        m_ContentContainer.Add(element);
                    }
                    else if (presenter.selectedNodes.Count == 1)
                    {
                        var node = presenter.selectedNodes.First();
                        var view = (AbstractNodeEditorView)Activator.CreateInstance(m_TypeMapper.MapType(node.GetType()));
                        view.node = node;
                        m_ContentContainer.Add(view);
                    }
                }
            }
        }

        public GraphInspectorPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (m_Presenter == value)
                    return;
                if (m_Presenter != null)
                    m_Presenter.onChange -= OnChange;
                m_Presenter = value;
                OnChange(GraphInspectorPresenter.ChangeType.All);
                m_Presenter.onChange += OnChange;
            }
        }

        public void Dispose()
        {
            if (m_Presenter != null)
                m_Presenter.onChange -= OnChange;
        }
    }
}
