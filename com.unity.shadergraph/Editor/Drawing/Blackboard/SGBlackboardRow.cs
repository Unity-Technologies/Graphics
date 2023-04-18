using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class SGBlackboardRow : VisualElement, IDisposable
    {
        static readonly string k_UxmlTemplatePath = "UXML/Blackboard/SGBlackboardRow";
        static readonly string k_StyleSheetPath = "Styles/SGBlackboard";

        VisualElement m_Root;
        Button m_ExpandButton;
        VisualElement m_ItemContainer;
        VisualElement m_PropertyViewContainer;
        bool m_Expanded = true;

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

        public SGBlackboardRow(VisualElement item, VisualElement propertyView)
        {
            var stylesheet = Resources.Load(k_StyleSheetPath) as StyleSheet;
            Assert.IsNotNull(stylesheet);
            styleSheets.Add(stylesheet);

            var uxmlTemplate = Resources.Load(k_UxmlTemplatePath) as VisualTreeAsset;
            Assert.IsNotNull(uxmlTemplate);

            VisualElement mainContainer = null;
            mainContainer = uxmlTemplate.Instantiate();
            Assert.IsNotNull(mainContainer);
            mainContainer.AddToClassList("mainContainer");

            m_Root = mainContainer.Q("root");
            m_ItemContainer = mainContainer.Q("itemContainer");
            m_PropertyViewContainer = mainContainer.Q("propertyViewContainer");

            m_ExpandButton = mainContainer.Q<Button>("expandButton");
            m_ExpandButton.clickable.clicked += () => expanded = !expanded;
            m_ExpandButton.RemoveFromHierarchy();

            Add(mainContainer);

            ClearClassList();
            AddToClassList("blackboardRow");

            name = "SGBlackboardRow";
            m_ItemContainer.Add(item);
            m_PropertyViewContainer.Add(propertyView);

            expanded = false;
        }

        public void Dispose()
        {
            Clear();
            m_ExpandButton.clickable = null;
            m_Root = null;
            m_ItemContainer = null;
            m_PropertyViewContainer = null;
            m_ExpandButton = null;
        }
    }
}
