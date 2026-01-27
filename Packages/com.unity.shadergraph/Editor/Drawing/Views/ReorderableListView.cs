using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal class ReorderableListView<T> : VisualElement
    {
        List<T> m_DataList;
        string m_header;
        ReorderableList m_ReorderableList;

        internal delegate T InitializeItemDelegate(List<T> list);
        internal InitializeItemDelegate InitializeItemCallback;

        internal delegate void ValueChangedDelegate(List<T> list);
        internal ValueChangedDelegate ValueChangedCallback;

        internal delegate void DrawItemDelegate(Rect rect, int idx);
        internal DrawItemDelegate DrawItemCallback;

        internal ReorderableListView(List<T> dataList, string header)
        {
            m_DataList = dataList;
            m_header = header;

            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ReorderableSlotListView"));
            Add(new IMGUIContainer(() => OnGUIHandler()) { name = "ListContainer" });
        }

        void OnGUIHandler()
        {
            if (m_ReorderableList == null)
                RecreateList(m_DataList, m_header);

            m_ReorderableList.DoLayoutList();
        }

        internal void RecreateList(List<T> dataList, string header)
        {
            m_DataList = dataList;
            m_header = header;
            m_ReorderableList = new ReorderableList(m_DataList, typeof(T), true, true, true, true);

            if (InitializeItemCallback != null)
            m_ReorderableList.onAddCallback += (ReorderableList list)
                => m_DataList.Add(InitializeItemCallback(m_DataList));

            m_ReorderableList.drawHeaderCallback += (Rect rect)
                => EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width - 10, rect.height), m_header);

            if (DrawItemCallback != null)
                m_ReorderableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused)
                    => DrawItemCallback(rect, index);

            if (ValueChangedCallback != null)
                m_ReorderableList.onChangedCallback += (ReorderableList list)
                    => ValueChangedCallback(m_DataList);
        }
    }
}
