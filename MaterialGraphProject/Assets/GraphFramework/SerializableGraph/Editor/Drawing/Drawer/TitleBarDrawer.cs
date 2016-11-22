using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.Graphing.Drawing
{
    [StyleSheet("Assets/GraphFramework/SerializableGraph/Editor/Drawing/Styles/TitleBar.uss")]
    public class TitleBarDrawer : DataWatchContainer
    {
        TitleBarDrawData m_dataProvider;
        VisualContainer m_leftContainer;
        VisualContainer m_rightContainer;

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

            m_leftContainer = new VisualContainer()
            {
                name = "left"
            };
            AddChild(m_leftContainer);

            m_rightContainer = new VisualContainer()
            {
                name = "right"
            };
            AddChild(m_rightContainer);

            foreach (var leftItemData in dataProvider.leftItems)
                m_leftContainer.AddChild(new TitleBarButtonDrawer(leftItemData));

            foreach (var rightItemData in dataProvider.rightItems)
                m_rightContainer.AddChild(new TitleBarButtonDrawer(rightItemData));

            this.dataProvider = dataProvider;
        }

        public override void OnDataChanged()
        {
            if (m_dataProvider == null)
                return;

            UpdateContainer(m_leftContainer, m_dataProvider.leftItems);
            UpdateContainer(m_rightContainer, m_dataProvider.rightItems);
        }

        void UpdateContainer(VisualContainer container, IEnumerable<TitleBarButtonDrawData> itemDatas)
        {
            // The number of items can't change for now.
            int i = 0;
            foreach (var itemData in itemDatas)
            {
                var item = container.GetChildAtIndex(i) as TitleBarButtonDrawer;
                if (item != null)
                    item.dataProvider = itemData;
                i++;
            }
        }

        protected override object toWatch
        {
            get { return dataProvider; }
        }
    }
}