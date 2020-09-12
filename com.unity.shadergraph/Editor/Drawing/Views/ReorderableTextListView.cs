using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Drawing.Views
{
    internal class ReorderableListView<T> : VisualElement
    {
        ReorderableList m_ReorderableList;
        List<string> m_TextList = new List<string>();
        List<T> m_DataList;
        IMGUIContainer m_Container;
        GUIStyle m_LabelStyle;
        int m_SelectedIndex = -1;
        string headerLabel => "Reorder data:";

        public List<string> TextList => m_TextList;

        internal delegate void ListReorderedDelegate(IList<T> reorderedList);
        public ListReorderedDelegate OnListReorderedCallback;

        internal ReorderableListView(List<T> dataList)
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

            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ReorderableSlotListView"));
            m_Container = new IMGUIContainer(() => OnGUIHandler()) {name = "ListContainer"};
            Add(m_Container);
        }

        internal void RecreateList(List<T> dataList)
        {
            m_DataList = dataList;
            // Create reorderable list from data list
            m_ReorderableList = new ReorderableList(dataList, typeof(int), true, true, true, true);
            m_ReorderableList.displayAdd = false;
            m_ReorderableList.displayRemove = false;
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
    }
}

