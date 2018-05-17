#if UNITY_2018_1
using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardRow : GraphElement
    {
        private VisualElement m_Root;
        private Button m_ExpandButton;
        private VisualElement m_ItemContainer;
        private VisualElement m_PropertyViewContainer;
        private bool m_Expanded = true;

        public bool expanded
        {
            get { return m_Expanded; }
            set
            {
                if (m_Expanded == value)
                {
                    return;
                }

                m_Expanded = value;

                if (m_Expanded)
                {
                    m_Root.Add(m_PropertyViewContainer);
                    AddToClassList("expanded");
                }
                else
                {
                    m_Root.Remove(m_PropertyViewContainer);
                    RemoveFromClassList("expanded");
                }
            }
        }

        public BlackboardRow(VisualElement item, VisualElement propertyView)
        {
            var tpl = Resources.Load<VisualTreeAsset>("UXML/GraphView/BlackboardRow");
            VisualElement mainContainer = tpl.CloneTree(null);
            mainContainer.AddToClassList("mainContainer");

            m_Root = mainContainer.Q<VisualElement>("root");
            m_ItemContainer = mainContainer.Q<VisualElement>("itemContainer");
            m_PropertyViewContainer = mainContainer.Q<VisualElement>("propertyViewContainer");

            m_ExpandButton = mainContainer.Q<Button>("expandButton");
            m_ExpandButton.clickable.clicked += () => expanded = !expanded;

            Add(mainContainer);

            ClearClassList();
            AddToClassList("sgblackboardRow");

            m_ItemContainer.Add(item);
            m_PropertyViewContainer.Add(propertyView);

            expanded = false;
        }
    }
}
#endif
