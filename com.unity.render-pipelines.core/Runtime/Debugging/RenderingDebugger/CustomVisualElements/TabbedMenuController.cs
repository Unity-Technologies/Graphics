using System;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.UI
{
    class TabbedMenuController
    {
        /* Define member variables*/
        public const string k_TabClassName = "tab";
        public const string k_CurrentlySelectedTabClassName = "currentlySelectedTab";
        public const string k_UnselectedContentClassName = "unselectedContent";
        public const string k_SelectedContentClassName = "selectedContent";
        // Tab and tab content have the same prefix but different suffix
        // Define the suffix of the tab name
        public const string k_TabNameSuffix = "Tab";
        // Define the suffix of the tab content name
        public const string k_ContentNameSuffix = "Content";

        private readonly VisualElement m_Root;

        public event Action<string> OnTabSelected;

        public TabbedMenuController(VisualElement root)
        {
            this.m_Root = root;
        }

        public void RegisterTabCallbacks()
        {
            UQueryBuilder<Label> tabs = GetAllTabs();
            tabs.ForEach((Label tab) => {
                tab.RegisterCallback<ClickEvent>(TabOnClick);
            });
        }

        /* Method for the tab on-click event:

           - If it is not selected, find other tabs that are selected, unselect them
           - Then select the tab that was clicked on
        */
        void TabOnClick(ClickEvent evt)
        {
            Label clickedTab = evt.currentTarget as Label;
            OnLabelClick(clickedTab);
        }

        public void OnLabelClick(Label clickedTab)
        {
            if (!TabIsCurrentlySelected(clickedTab))
            {
                GetAllTabs().Where(
                    (tab) => tab != clickedTab && TabIsCurrentlySelected(tab)
                ).ForEach(UnselectTab);
                SelectTab(clickedTab);
            }
        }

        //Method that returns a Boolean indicating whether a tab is currently selected
        private static bool TabIsCurrentlySelected(Label tab)
        {
            return tab.ClassListContains(k_CurrentlySelectedTabClassName);
        }

        private UQueryBuilder<Label> GetAllTabs()
        {
            return m_Root.Query<Label>(className: k_TabClassName);
        }

        /* Method for the selected tab:
           -  Takes a tab as a parameter and adds the currentlySelectedTab class
           -  Then finds the tab content and removes the unselectedContent class */
        void SelectTab(Label tab)
        {
            tab.AddToClassList(k_CurrentlySelectedTabClassName);
            VisualElement content = FindContent(tab);
            content.RemoveFromClassList(k_UnselectedContentClassName);
            OnTabSelected?.Invoke(tab.name);
        }

        /* Method for the unselected tab:
           -  Takes a tab as a parameter and removes the currentlySelectedTab class
           -  Then finds the tab content and adds the unselectedContent class */
        void UnselectTab(Label tab)
        {
            tab.RemoveFromClassList(k_CurrentlySelectedTabClassName);
            VisualElement content = FindContent(tab);
            content.AddToClassList(k_UnselectedContentClassName);
        }

        // Method to generate the associated tab content name by for the given tab name
        private static string GenerateContentName(Label tab) =>
            tab.name.Replace(k_TabNameSuffix, k_ContentNameSuffix);

        // Method that takes a tab as a parameter and returns the associated content element
        private VisualElement FindContent(Label tab)
        {
            return m_Root.Q(GenerateContentName(tab));
        }
    }
}
