using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEditor.Rendering.UI;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    public class PanelTab : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<PanelTab, UxmlTraits> { }

        private readonly Button m_PreviousButton;
        private readonly VisualElement m_Tabs;
        private readonly Button m_NextButton;

        public event Action<string> OnTabSelected;

        public PanelTab()
        {
            style.flexDirection = FlexDirection.Row;
            m_PreviousButton = new Button {name = "PreviousButton", text = "<<"};
            m_PreviousButton.clicked += OnPreviousClicked;
            Add(m_PreviousButton);

            m_Tabs = new VisualElement() {name = "CurrentTabName"};
            m_Tabs.style.flexDirection = FlexDirection.Row;
            Add(m_Tabs);

            m_NextButton = new Button {name = "NextButton", text = ">>"};
            m_NextButton.clicked += OnNextClicked;
            Add(m_NextButton);
        }

        private int m_CurrentSelectedChoice = 0;

        public void AddTab(Label element)
        {
            m_Tabs.Add(element);
            element.style.justifyContent = Justify.Center;
            element.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            element.AddToClassList(TabbedMenuController.k_UnselectedContentClassName);
        }

        internal void OnPreviousClicked()
        {
            var oldSelected = m_Tabs[m_CurrentSelectedChoice] as Label;

            m_CurrentSelectedChoice--;
            if (m_CurrentSelectedChoice < 0)
                m_CurrentSelectedChoice = m_Tabs.Children().Count() - 1;

            UnSelectTabAndSelectCurrent(oldSelected);
        }

        internal void OnNextClicked()
        {
            var oldSelected = m_Tabs[m_CurrentSelectedChoice] as Label;

            m_CurrentSelectedChoice++;
            if (m_CurrentSelectedChoice >= m_Tabs.Children().Count())
                m_CurrentSelectedChoice = 0;

            UnSelectTabAndSelectCurrent(oldSelected);
        }

        void UnSelectTabAndSelectCurrent(Label oldSelectedTabLabel)
        {
            oldSelectedTabLabel.RemoveFromClassList(TabbedMenuController.k_CurrentlySelectedTabClassName);
            oldSelectedTabLabel.AddToClassList(TabbedMenuController.k_UnselectedContentClassName);

            var oldSelectedContentName = oldSelectedTabLabel.name.Replace(TabbedMenuController.k_TabNameSuffix, TabbedMenuController.k_ContentNameSuffix);
            var oldSelectedContent = tabContentVisualElement.Q<VisualElement>(oldSelectedContentName);
            oldSelectedContent.AddToClassList(TabbedMenuController.k_UnselectedContentClassName);
            oldSelectedContent.RemoveFromClassList(TabbedMenuController.k_SelectedContentClassName);

            var newSelectedTabLabel = m_Tabs[m_CurrentSelectedChoice] as Label;
            newSelectedTabLabel.AddToClassList(TabbedMenuController.k_CurrentlySelectedTabClassName);
            newSelectedTabLabel.RemoveFromClassList(TabbedMenuController.k_UnselectedContentClassName);

            var newSelectedTabContentName = newSelectedTabLabel.name.Replace(TabbedMenuController.k_TabNameSuffix, TabbedMenuController.k_ContentNameSuffix);
            var newSelectedContent = tabContentVisualElement.Q<VisualElement>(newSelectedTabContentName);
            newSelectedContent.RemoveFromClassList(TabbedMenuController.k_UnselectedContentClassName);
            newSelectedContent.AddToClassList(TabbedMenuController.k_SelectedContentClassName);

            OnTabSelected.Invoke(newSelectedTabLabel.name);
        }

        public VisualElement tabContentVisualElement { get; set; }

        public void SetSelectedChoice(string choice)
        {
            var oldSelected = m_Tabs[m_CurrentSelectedChoice] as Label;

            var labels = m_Tabs.Children().ToList();
            m_CurrentSelectedChoice = labels.IndexOf(m_Tabs.Q<Label>(choice));

            UnSelectTabAndSelectCurrent(oldSelected);
        }
    }
}
