using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    [StyleSheet("Assets/GraphFramework/SerializableGraph/Editor/Drawing/Styles/Header.uss")]
    public class HeaderDrawer : GraphElement
    {
        private VisualElement m_Title;
        private VisualElement m_ExpandButton;
        private NodeExpander m_NodeExpander = new NodeExpander();

        public HeaderDrawer(HeaderDrawData data)
        {
            pickingMode = PickingMode.Ignore;
            RemoveFromClassList("graphElement");

            m_ExpandButton = new VisualElement()
            {
                name = "expandButton",
                content = new GUIContent("")
            };
            m_ExpandButton.AddManipulator(m_NodeExpander);
            AddChild(m_ExpandButton);

            m_Title = new VisualElement()
            {
                name = "title",
                content = new GUIContent(),
                pickingMode = PickingMode.Ignore
            };
            AddChild(m_Title);

            dataProvider = data;
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
            m_ExpandButton.content.text = headerData.expanded ? "-" : "+";
            m_NodeExpander.data = headerData;

            this.Touch(ChangeType.Repaint);
        }
    }
}