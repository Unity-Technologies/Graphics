using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class GraphInspectorView : DataWatchContainer
    {
        [SerializeField]
        GraphInspectorPresenter m_Presenter;

        int m_EditorHash;

        VisualElement m_EditorContainer;
        VisualElement m_Title;

        TypeMapper m_TypeMapper;

        public GraphInspectorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
            var headerContainer = new VisualElement() { name = "header" };
            m_Title = new VisualElement() { name = "title" };
            headerContainer.Add(m_Title);
            Add(headerContainer);
            Add(m_EditorContainer = new VisualElement {name = "editorContainer"});
            m_TypeMapper = new TypeMapper(typeof(AbstractNodeEditorPresenter), typeof(AbstractNodeEditorView))
            {
                {typeof(StandardNodeEditorPresenter), typeof(StandardNodeEditorView)},
                {typeof(SurfaceMasterNodeEditorPresenter), typeof(SurfaceMasterNodeEditorView)}
            };
        }

        public override void OnDataChanged()
        {
            if (presenter == null)
            {
                m_EditorContainer.Clear();
                m_EditorHash = 0;
                return;
            }

            m_Title.text = presenter.title;

            var editorHash = 17;
            unchecked
            {
                foreach (var editorPresenter in presenter.editors)
                    editorHash = editorHash * 31 + editorPresenter.GetHashCode();
            }

            if (editorHash != m_EditorHash)
            {
                m_EditorHash = editorHash;
                m_EditorContainer.Clear();
                foreach (var editorPresenter in presenter.editors)
                {
                    var view = (AbstractNodeEditorView) Activator.CreateInstance(m_TypeMapper.MapType(editorPresenter.GetType()));
                    view.presenter = editorPresenter;
                    m_EditorContainer.Add(view);
                }
            }

            Dirty(ChangeType.Repaint);
        }

        public GraphInspectorPresenter presenter
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
            get { return new Object[] {m_Presenter}; }
        }
    }
}
