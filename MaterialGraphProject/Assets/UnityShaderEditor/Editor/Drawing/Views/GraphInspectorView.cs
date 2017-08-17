using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing.Views
{
    public class GraphInspectorView : DataWatchContainer
    {
        [SerializeField]
        GraphInspectorPresenter m_Presenter;

        IMGUIContainer m_ImguiContainer;

        public GraphInspectorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");
            Add(m_ImguiContainer = new IMGUIContainer(OnGUIHandler));
        }

        void OnGUIHandler()
        {
            if (m_Presenter == null)
                return;

            foreach (var inspector in presenter.inspectors)
            {
                inspector.OnInspectorGUI();
            }
        }

        public override void OnDataChanged()
        {
            if (presenter == null)
                return;
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
//                m_ImguiContainer.executionContext = presenter.GetInstanceID();
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
