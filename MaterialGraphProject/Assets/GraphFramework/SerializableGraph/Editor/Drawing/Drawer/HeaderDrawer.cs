using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public class HeaderDrawer : GraphElement
    {
        private VisualElement m_Title;

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

            m_Title.content.text = headerData.node.name;
        }
    }
}