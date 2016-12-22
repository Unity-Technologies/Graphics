using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    [StyleSheet("Assets/GraphFramework/SerializableGraph/Editor/Drawing/Styles/Header.uss")]
    public class HeaderDrawer : DataWatchContainer
    {
        private VisualElement m_Title;
        private VisualElement m_ExpandButton;
        private HeaderDrawData m_DataProvider;

        public HeaderDrawData dataProvider
        {
            get { return m_DataProvider; }
            set
            {
                if (m_DataProvider == value)
                    return;
                RemoveWatch();
                m_DataProvider = value;
                OnDataChanged();
                AddWatch();
            }
        }

        public HeaderDrawer()
        {
            m_Title = new VisualElement()
            {
                name = "title",
                content = new GUIContent()
            };
            AddChild(m_Title);

            m_ExpandButton = new VisualElement
            {
                name = "expandButton",
                content = new GUIContent("teeest")
            };
            var clickable = new Clickable(OnExpandClick);
            m_ExpandButton.AddManipulator(clickable);
            AddChild(m_ExpandButton);
        }

        public HeaderDrawer(HeaderDrawData dataProvider) : this()
        {
            this.dataProvider = dataProvider;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            if (dataProvider == null)
            {
                m_Title.content.text = "";
                return;
            }

            m_Title.content.text = dataProvider.title;
            m_ExpandButton.content.text = dataProvider.expanded ? "Collapse" : "Expand";

            this.Touch(ChangeType.Repaint);
        }

        private void OnExpandClick()
        {
            if (dataProvider == null) return;
            dataProvider.expanded = !dataProvider.expanded;
        }

        protected override object toWatch
        {
            get { return m_DataProvider; }
        }
    }
}