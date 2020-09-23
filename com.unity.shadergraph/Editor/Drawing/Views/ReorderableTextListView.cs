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
        //this was made to be a generic adapter but is currently only used for target settings
        //up to us if we want to adapt this for our needs or create a new ReorderableList manager for targets 
        ReorderableList m_ReorderableList;
        List<string> m_TextList = new List<string>();
        List<string> m_MenuOptions = new List<string>();
        List<T> m_DataList;
        IMGUIContainer m_Container;
        GUIStyle m_LabelStyle;
        int m_SelectedIndex = -1;
        string headerLabel;

        public List<string> TextList => m_TextList;
        //populates the options for the drop down menu on add item
        public List<string> MenuOptions 
        {
            get { return m_MenuOptions; }
            set { m_MenuOptions = value; }
        }

        //temporarily added add and remove callbacks just in case we need them 
        internal delegate void ListRemoveItemDelegate(IList<T> removeList);
        public ListRemoveItemDelegate OnListItemRemovedCallback;

        internal delegate void ListAddItemDelegate(IList<T> addList);
        public ListAddItemDelegate OnListItemAddedCallback;

        internal delegate void ListReorderedDelegate(IList<T> reorderedList);
        public ListReorderedDelegate OnListReorderedCallback;

        internal ReorderableListView(List<T> dataList, string header = "Reorder data:") //added override for header text
        {
            m_DataList = dataList;
            try
            {
                foreach (var data in dataList)
                {
                    m_TextList.Add(data.ToString());
                }
            }
            catch (Exception e)
            {
                Debug.Log("Exception: " + e.ToString() + " while handling ReorderableListView of type: " + typeof(T).ToString());
            }
            headerLabel = header;

            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ReorderableSlotListView"));
            m_Container = new IMGUIContainer(() => OnGUIHandler()) {name = "ListContainer"};
            Add(m_Container);
        }

        internal void RecreateList(List<T> dataList)
        {
            m_DataList = dataList;
            // Create reorderable list from data list
            m_ReorderableList = new ReorderableList(dataList, typeof(int), false, true, true, true); //temporarily not actually reorderable
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
                EditorGUI.LabelField(labelRect, headerLabel);
            };

            // Draw Element
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight),
                    m_TextList[index], EditorStyles.label);
                if (EditorGUI.EndChangeCheck())
                {
                    RecreateList(m_DataList);
                }
            };

            // Element height
            m_ReorderableList.elementHeightCallback = (int indexer) => { return m_ReorderableList.elementHeight; };

            // Add callback delegates
            m_ReorderableList.onSelectCallback += SelectEntry;
            m_ReorderableList.onReorderCallback += ReorderEntries;
            m_ReorderableList.onAddDropdownCallback += AddEntry; //adds items from a limited list of options
        }

        private void SelectEntry(ReorderableList list)
        {
            m_SelectedIndex = list.index;
        }

        private void ReorderEntries(ReorderableList list)
        {
            var concreteList = (List<T>) list.list;
            RecreateList(concreteList);
            OnListReorderedCallback(concreteList);
        }

        private void AddEntry(Rect buttonRect, ReorderableList list)
        {
            //created the drop down menu on add item from the listview
            var menu = new GenericMenu();
            foreach (var entry in m_MenuOptions)
            {
                menu.AddItem(new GUIContent(entry), false, AddDataItem);
            }
            menu.ShowAsContext();
        }

        private void AddDataItem()
        {
            //needs to actually add items to the data list
        }

    }
}

