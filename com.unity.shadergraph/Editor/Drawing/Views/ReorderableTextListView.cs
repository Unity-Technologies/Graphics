using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal class ReorderableListView<T> : VisualElement
    {
        // generic control to display and allow the user to directly reorder/add/remove from a list of T

        // the List we are editing
        List<T> m_DataList;

        // this is how we get the string to display for each item
        Func<T, string> m_GetDisplayTextFunc;

        readonly bool m_AllowReorder;
        ReorderableList m_ReorderableList;
        List<string> m_TextList = new List<string>();

        IMGUIContainer m_Container;
        GUIStyle m_LabelStyle;
        int m_SelectedIndex = -1;
        string m_HeaderLabel;

        // list of options for the drop down menu when the user clicks the add button
        public delegate List<string> GetAddMenuOptionsDelegate();
        public GetAddMenuOptionsDelegate GetAddMenuOptions;

        // callback when the user clicks on an item from the AddMenu
        public delegate void OnAddMenuItemDelegate(List<T> targetList, int addMenuOptionIndex, string addMenuOption);
        public OnAddMenuItemDelegate OnAddMenuItemCallback;

        // callback to override how an item is removed from the list
        internal delegate void RemoveItemDelegate(List<T> list, int itemIndex);
        public RemoveItemDelegate RemoveItemCallback;

        // callback when the list is reordered
        internal delegate void ListReorderedDelegate(List<T> reorderedList);
        public ListReorderedDelegate OnListReorderedCallback;

        internal ReorderableListView(
            List<T> dataList,
            string header = "Reorder data:",
            bool allowReorder = true,
            Func<T, string> getDisplayText = null)
        {
            m_DataList = dataList;
            m_HeaderLabel = header;
            m_AllowReorder = allowReorder;

            // setup GetDisplayTextFunc
            if (getDisplayText == null)
                m_GetDisplayTextFunc = data => data.ToString();
            else
                m_GetDisplayTextFunc = getDisplayText;

            RebuildTextList();

            // should we set up a new style sheet?  allow user overrides?
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ReorderableSlotListView"));

            m_Container = new IMGUIContainer(() => OnGUIHandler()) {name = "ListContainer"};
            Add(m_Container);
        }

        void RebuildTextList()
        {
            m_TextList.Clear();
            try
            {
                foreach (var data in m_DataList)
                {
                    m_TextList.Add(m_GetDisplayTextFunc(data));
                }
            }
            catch (Exception e)
            {
                Debug.Log("Exception: " + e.ToString() + " while handling ReorderableListView of type: " + typeof(T).ToString());
            }
        }

        internal void RecreateList(List<T> dataList)
        {
            m_DataList = dataList;

            // Create reorderable list from data list
            m_ReorderableList = new ReorderableList(
                dataList,
                typeof(T),          // the type of the elements in dataList
                m_AllowReorder,     // draggable (to reorder)
                true,               // displayHeader
                true,               // displayAddButton
                true);              // displayRemoveButton
        }

        private void OnGUIHandler()
        {
            try
            {
                if (m_ReorderableList == null)
                {
                    RecreateList(m_DataList);
                    AddCallbacks();
                }

                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    m_ReorderableList.index = m_SelectedIndex;
                    m_ReorderableList.DoLayoutList();

                    if (changeCheckScope.changed)
                    {
                        // Do things when changed
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("Exception: " + e.ToString() + " while handling ReorderableListView of type: " + typeof(T).ToString());
            }
        }

        private void AddCallbacks()
        {
            m_ReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                var labelRect = new Rect(rect.x, rect.y, rect.width - 10, rect.height);
                EditorGUI.LabelField(labelRect, m_HeaderLabel);
            };

            // Draw Element
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight),
                    m_TextList[index], EditorStyles.label);
            };

            // Element height
            m_ReorderableList.elementHeightCallback = (int indexer) => { return m_ReorderableList.elementHeight; };

            // Add callback delegates
            m_ReorderableList.onSelectCallback += SelectEntry;              // should we propagate this up if user wants to do something with selection?
            m_ReorderableList.onReorderCallback += ReorderEntries;
            m_ReorderableList.onAddDropdownCallback += OnAddDropdownMenu;
            m_ReorderableList.onRemoveCallback += OnRemove;
        }

        private void SelectEntry(ReorderableList list)
        {
            m_SelectedIndex = list.index;
        }

        private void ReorderEntries(ReorderableList list)
        {
            RebuildTextList();
            OnListReorderedCallback(m_DataList);
        }

        private void OnAddDropdownMenu(Rect buttonRect, ReorderableList list)
        {
            //created the drop down menu on add item from the listview
            List<string> addMenuOptions;
            if (GetAddMenuOptions != null)
                addMenuOptions = GetAddMenuOptions();
            else
                addMenuOptions = new List<string>();

            var menu = new GenericMenu();
            for (int optionIndex = 0; optionIndex < addMenuOptions.Count; optionIndex++)
            {
                string optionText = addMenuOptions[optionIndex];

                // disable any option that is already in the text list
                bool optionEnabled = !m_TextList.Contains(optionText);

                // need to make a copy so the lambda will capture the value, not the variable
                int localOptionIndex = optionIndex;

                if (optionEnabled)
                    menu.AddItem(new GUIContent(optionText), false, () => OnAddMenuClicked(localOptionIndex, optionText));
                else
                    menu.AddDisabledItem(new GUIContent(optionText));
            }
            menu.ShowAsContext();
        }

        void OnAddMenuClicked(int optionIndex, string optionText)
        {
            OnAddMenuItemCallback(m_DataList, optionIndex, optionText);

            // if anything was changed about the list in the callback, we need to rebuild our text list
            RebuildTextList();
        }

        void OnRemove(ReorderableList list)
        {
            int indexToRemove = list.index;
            if (indexToRemove < 0)
                indexToRemove = m_DataList.Count - 1;

            if (indexToRemove >= 0)
            {
                if (RemoveItemCallback != null)
                {
                    RemoveItemCallback(m_DataList, indexToRemove);
                }
                else
                {
                    // no callback provided, do it ourselves
                    m_DataList.RemoveAt(indexToRemove);
                }

                // if anything was changed about the list in the callback, we need to rebuild our text list
                RebuildTextList();
            }
        }
    }
}

