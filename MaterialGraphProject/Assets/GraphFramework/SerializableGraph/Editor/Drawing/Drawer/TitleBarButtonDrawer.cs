using UnityEngine.RMGUI;
using RMGUI.GraphView;
using UnityEngine.RMGUI.StyleSheets;
using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    public class TitleBarButtonDrawer : DataWatchContainer
    {
        TitleBarButtonDrawData m_dataProvider;
        Clicker m_clicker;
        VisualElement m_label;

        public TitleBarButtonDrawData dataProvider
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

        public TitleBarButtonDrawer(TitleBarButtonDrawData dataProvider)
        {
            classList = new ClassList("titleBarItem");

            m_clicker = new Clicker();
            m_clicker.onClick += OnClick;
            m_clicker.onStateChange += OnClickStateChanged;
            AddManipulator(m_clicker);

            AddChild(new VisualElement() { classList = new ClassList("titleBarItemBorder") });
            m_label = new VisualElement()
            {
                classList = new ClassList("titleBarItemLabel"),
                content = new GUIContent("")
            };
            AddChild(m_label);
            AddChild(new VisualElement() { classList = new ClassList("titleBarItemBorder") });

            this.dataProvider = dataProvider;
        }

        public override void OnDataChanged()
        {
            if (m_dataProvider == null)
                return;

            m_label.content.text = m_dataProvider.text;

            this.Touch(ChangeType.Repaint);
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
            this.Touch(ChangeType.Repaint);
        }

        protected override object toWatch
        {
            get { return dataProvider; }
        }
    }
}
