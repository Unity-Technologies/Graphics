using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing.Util;

namespace UnityEditor.Graphing.Drawing
{
    // TODO JOCE: we should not need a title bar drawer. It should just be a visual element in the nodedrawer.
    public class TitleBarDrawer : DataWatchContainer
    {
        TitleBarPresenter m_DataProvider;
        VisualContainer m_LeftContainer;
        VisualContainer m_RightContainer;

        public TitleBarPresenter dataProvider
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

        public TitleBarDrawer(TitleBarPresenter dataProvider)
        {
            name = "TitleBar";

            m_LeftContainer = new VisualContainer()
            {
                name = "left"
            };
            AddChild(m_LeftContainer);

            m_RightContainer = new VisualContainer()
            {
                name = "right"
            };
            AddChild(m_RightContainer);

            foreach (var leftItemData in dataProvider.leftItems)
                m_LeftContainer.AddChild(new TitleBarButtonDrawer(leftItemData));

            foreach (var rightItemData in dataProvider.rightItems)
                m_RightContainer.AddChild(new TitleBarButtonDrawer(rightItemData));

            this.dataProvider = dataProvider;
            AddStyleSheetPath("Styles/TitleBar");
        }

        public override void OnDataChanged()
        {
            if (m_DataProvider == null)
                return;

            UpdateContainer(m_LeftContainer, m_DataProvider.leftItems);
            UpdateContainer(m_RightContainer, m_DataProvider.rightItems);
        }

        void UpdateContainer(VisualContainer container, IEnumerable<TitleBarButtonPresenter> itemDatas)
        {
            // The number of items can't change for now.
            foreach (var pair in itemDatas.Zip(container.OfType<TitleBarButtonDrawer>()))
            {
                var itemData = pair.Item1;
                var item = pair.Item2;
                item.dataProvider = itemData;
            }
        }

        protected override object toWatch
        {
            get { return dataProvider; }
        }
    }
}
