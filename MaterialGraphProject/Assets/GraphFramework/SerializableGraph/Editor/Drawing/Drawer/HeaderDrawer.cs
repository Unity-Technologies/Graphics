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
        private NodeExpander m_NodeExpander = new NodeExpander();

        private HeaderDrawData m_dataProvider;

        public HeaderDrawData dataProvider
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

        protected override object toWatch
        {
            get { return m_dataProvider; }
        }

        public HeaderDrawer()
        {
            pickingMode = PickingMode.Ignore;
            RemoveFromClassList("graphElement");

            m_Title = new VisualElement()
            {
                name = "title",
                content = new GUIContent(),
                pickingMode = PickingMode.Ignore
            };
            AddChild(m_Title);

            m_ExpandButton = new VisualElement()
            {
                name = "expandButton",
                content = new GUIContent("")
            };
            m_ExpandButton.AddManipulator(m_NodeExpander);
            AddChild(m_ExpandButton);
        }

        public HeaderDrawer(HeaderDrawData dataProvider) : this()
        {
            this.dataProvider = dataProvider;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var headerData = dataProvider as HeaderDrawData;

            if (headerData == null)
            {
                m_Title.content.text = "";
                return;
            }

            m_Title.content.text = headerData.title;
            m_ExpandButton.content.text = headerData.expanded ? "Collapse" : "Expand";
            m_NodeExpander.data = headerData;

            this.Touch(ChangeType.Repaint);
        }
    }
}