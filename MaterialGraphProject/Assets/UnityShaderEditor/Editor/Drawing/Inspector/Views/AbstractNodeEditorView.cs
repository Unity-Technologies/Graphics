using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class AbstractNodeEditorView : DataWatchContainer
    {
        [SerializeField]
        AbstractNodeEditorPresenter m_Presenter;

        public AbstractNodeEditorPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (value == m_Presenter)
                    return;
                RemoveWatch();
                m_Presenter = value;
                m_ToWatch[0] = m_Presenter;
                OnDataChanged();
                AddWatch();
            }
        }

        Object[] m_ToWatch = { null };

        protected override Object[] toWatch
        {
            get { return m_ToWatch; }
        }
    }
}
