using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.Graphing.Drawing
{
    [StyleSheet("Assets/GraphFramework/SerializableGraph/Editor/Drawing/Styles/TitleBar.uss")]
    public class TitleBarDrawer : DataWatchContainer
    {
        private TitleBarDrawData m_dataProvider;
        private Label m_title;

        public TitleBarDrawData dataProvider
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

        public TitleBarDrawer(TitleBarDrawData dataProvider)
        {
            classList = ClassList.empty;
            name = "TitleBar";
            zBias = 99;

            var leftContainer = new VisualContainer()
            {
                name = "left"
            };
            AddChild(leftContainer);

            var rightContainer = new VisualContainer()
            {
                name = "right"
            };
            AddChild(rightContainer);

            m_title = new Label(new GUIContent("testttt"))
            {
                name = "title",
            };
            leftContainer.AddChild(m_title);

            this.dataProvider = dataProvider;
        }

        public override void OnDataChanged()
        {
            if (m_dataProvider == null)
                return;

            m_title.content.text = m_dataProvider.title;

            this.Touch(ChangeType.Repaint);
        }

        protected override object toWatch
        {
            get { return dataProvider; }
        }
    }
}