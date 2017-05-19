using UnityEngine.Experimental.UIElements;
using RMGUI.GraphView;
using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    public class TitleBarButtonDrawer : DataWatchContainer
    {
        TitleBarButtonPresenter m_dataProvider;
        Clicker m_clicker;
        VisualElement m_label;

        public TitleBarButtonPresenter dataProvider
        {
            get { return m_dataProvider; }
            set
            {
                if (m_dataProvider == value)
                    return;
                RemoveWatch();
                m_dataProvider = value;
                OnDataChanged();
                AddWatch();
            }
        }

        public TitleBarButtonDrawer(TitleBarButtonPresenter dataProvider)
        {
            AddToClassList("titleBarItem");

            m_clicker = new Clicker();
            m_clicker.onClick += OnClick;
            m_clicker.onStateChange += OnClickStateChanged;
            AddManipulator(m_clicker);

            var ve = new VisualElement();
            ve.AddToClassList("titleBarItemBorder");
            AddChild(ve);


            m_label = new VisualElement();
            m_label.AddToClassList("titleBarItemLabel");
            AddChild(m_label);

            var ve2 = new VisualElement();
            ve2.AddToClassList("titleBarItemBorder");

            this.dataProvider = dataProvider;
        }

        public override void OnDataChanged()
        {
            if (m_dataProvider == null)
                return;

            m_label.text = m_dataProvider.text;

            this.Dirty(ChangeType.Repaint);
        }

        void OnClick()
        {
            if (m_dataProvider != null && m_dataProvider.onClick != null)
                m_dataProvider.onClick();
        }

        void OnClickStateChanged(ClickerState newState)
        {
            if (newState == ClickerState.Active)
                AddToClassList("active");
            else if (newState == ClickerState.Inactive)
                RemoveFromClassList("active");
            this.Dirty(ChangeType.Repaint);
        }

        protected override object toWatch
        {
            get { return dataProvider; }
        }
    }
}
