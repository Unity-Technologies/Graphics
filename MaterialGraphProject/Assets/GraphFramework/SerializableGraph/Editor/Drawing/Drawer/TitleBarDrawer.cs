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
		private VisualElement m_title;

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

			var titleItem = new VisualContainer() { classList = new ClassList("titleBarItem") };
			titleItem.AddChild(new VisualElement() { classList = new ClassList("titleBarItemBorder") });
			m_title = new VisualElement()
            {
				classList = new ClassList("titleBarItemLabel"),
				content = new GUIContent("")
            };
			titleItem.AddChild(m_title);
			titleItem.AddChild(new VisualElement() { classList = new ClassList("titleBarItemBorder") });
			leftContainer.AddChild(titleItem);

			var showInProjectItem = new VisualContainer() { classList = new ClassList("titleBarItem") };
			showInProjectItem.AddChild(new VisualElement() { classList = new ClassList("titleBarItemBorder") });
			var showInProjectLabel = new VisualElement()
			{
				classList = new ClassList("titleBarItemLabel"),
				content = new GUIContent("Show in project")
			};
			showInProjectItem.AddChild(showInProjectLabel);
			showInProjectItem.AddChild(new VisualElement() { classList = new ClassList("titleBarItemBorder") });
			leftContainer.AddChild(showInProjectItem);

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