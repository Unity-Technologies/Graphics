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

        int m_PresenterHash;

        VisualElement m_Title;
        VisualElement m_ContentContainer;
        VisualElement m_MultipleSelectionsElement;

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

            m_TypeMapper = new TypeMapper(typeof(AbstractNodeEditorPresenter), typeof(AbstractNodeEditorView))
            {
                { typeof(StandardNodeEditorPresenter), typeof(StandardNodeEditorView) },
                { typeof(SurfaceMasterNodeEditorPresenter), typeof(SurfaceMasterNodeEditorView) }
            };
        }

        public override void OnDataChanged()
        {
            if (presenter == null)
            {
                m_ContentContainer.Clear();
                m_PresenterHash = 0;
                return;
            }

            var presenterHash = UIUtilities.GetHashCode(presenter.editor, presenter.selectionCount);

            m_Title.text = presenter.title;

            if (presenterHash != m_PresenterHash)
            {
                m_PresenterHash = presenterHash;
                m_ContentContainer.Clear();
                if (presenter.selectionCount > 1)
                {
                    var element = new VisualElement { name = "selectionCount", text = string.Format("{0} nodes selected.", presenter.selectionCount) };
                    m_ContentContainer.Add(element);
                }
                else if (presenter.editor != null)
                {
                    var view = (AbstractNodeEditorView)Activator.CreateInstance(m_TypeMapper.MapType(presenter.editor.GetType()));
                    view.presenter = presenter.editor;
                    m_ContentContainer.Add(view);
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
            get { return new Object[] { m_Presenter }; }
        }
    }
}
