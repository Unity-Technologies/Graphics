using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal class ReorderableListView<T> : VisualElement
    {
        internal enum ListActionType { Add,  Remove, Reorder }

        List<T> m_DataList;
        GUIContent m_header;
        float? m_ElementHeight;
        ReorderableList m_ReorderableList;

        public delegate void OnBeforeChangeDelegate(ListActionType actionType);
        public OnBeforeChangeDelegate OnBeforeChangeCallback;

        public delegate void OnChangeDelegate(ListActionType actionType);
        public OnChangeDelegate OnChangeCallback;

        public delegate T OnNewItemDelegate();
        public OnNewItemDelegate OnNewItemCallback;

        internal delegate void DrawItemDelegate(Rect rect, int idx);
        internal DrawItemDelegate DrawItemCallback;

        internal ReorderableListView(List<T> dataList, string header, float? elementHeight = null) : this(dataList, new GUIContent(header, ""), elementHeight) {}

        internal ReorderableListView(List<T> dataList, GUIContent header, float? elementHeight = null)
        {
            m_DataList = dataList;
            m_header = header;
            m_ElementHeight = elementHeight;

            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ReorderableSlotListView"));
            Add(new IMGUIContainer(() => OnGUIHandler()) { name = "ListContainer" });
        }

        void OnGUIHandler()
        {
            if (m_ReorderableList == null)
                RecreateList(m_DataList, m_header);

            m_ReorderableList.DoLayoutList();
        }

        internal void RecreateList(List<T> dataList, GUIContent header)
        {
            m_DataList = dataList;
            m_header = header;
            var m_TmpList = new List<bool>(dataList.Capacity);
            foreach (T item in dataList)
                m_TmpList.Add(false);

            m_ReorderableList = new ReorderableList(m_TmpList, typeof(bool), true, true, true, true);
            if (m_ElementHeight.HasValue)
                m_ReorderableList.elementHeight = m_ElementHeight.Value;

            m_ReorderableList.onAddCallback += (ReorderableList list) =>
            {
                OnBeforeChangeCallback?.Invoke(ListActionType.Add);
                T item = OnNewItemCallback != null ? OnNewItemCallback() : default(T);
                m_DataList.Add(item);
                m_TmpList.Add(false);
                OnChangeCallback?.Invoke(ListActionType.Add);
            };

            m_ReorderableList.onRemoveCallback += (ReorderableList list) =>
            {
                int index = list.index;
                if (index < 0 || index >= list.count)
                    return;
                OnBeforeChangeCallback?.Invoke(ListActionType.Remove);
                m_DataList.RemoveAt(index);
                m_TmpList.RemoveAt(index);
                OnChangeCallback?.Invoke(ListActionType.Remove);
            };

            m_ReorderableList.onReorderCallbackWithDetails += (ReorderableList list, int oldIndex, int newIndex) =>
            {
                OnBeforeChangeCallback?.Invoke(ListActionType.Reorder);
                var item = m_DataList[oldIndex];
                m_DataList.RemoveAt(oldIndex);
                m_DataList.Insert(newIndex, item);
                OnChangeCallback?.Invoke(ListActionType.Reorder);
            };

            m_ReorderableList.drawHeaderCallback += (Rect rect)
                => EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width - 10, rect.height), m_header);

            if (DrawItemCallback != null)
                m_ReorderableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused)
                    => DrawItemCallback(rect, index);
        }
    }
}
