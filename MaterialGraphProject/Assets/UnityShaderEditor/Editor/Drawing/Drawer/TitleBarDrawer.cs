using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

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

        public TitleBarDrawer()
        {
            name = "TitleBar";

            m_LeftContainer = new VisualContainer()
            {
                name = "left"
            };
            Add(m_LeftContainer);

            m_RightContainer = new VisualContainer()
            {
                name = "right"
            };
            Add(m_RightContainer);

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
            container.ClearChildren();
            foreach (var itemPresenter in itemDatas)
                container.AddChild(new TitleBarButtonDrawer(itemPresenter));
        }

        protected override Object[] toWatch
        {
            get { return new Object[]{dataProvider}; }
        }
    }
}
