using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class TitleBarView : DataWatchContainer
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

        public TitleBarView()
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

        static void UpdateContainer(VisualContainer container, IEnumerable<TitleBarButtonPresenter> itemDatas)
        {
            container.ClearChildren();
            foreach (var itemPresenter in itemDatas)
                container.AddChild(new TitleBarButtonView(itemPresenter));
        }

        protected override Object[] toWatch
        {
            get { return new Object[] {dataProvider}; }
        }
    }
}
