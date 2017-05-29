using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugPanelUI
    {
        protected GameObject        m_Root = null;
        protected DebugPanel        m_DebugPanel = null;
        protected List<DebugItemUI> m_ItemsUI = new List<DebugItemUI>();
        protected int               m_SelectedItem = -1;

        public int itemCount { get { return m_ItemsUI.Count; } }

        public DebugPanelUI()
        {
        }

        public void SetDebugPanel(DebugPanel panel)
        {
            m_DebugPanel = panel;
        }

        public void BuildGUI(GameObject parent)
        {
            m_Root = new GameObject(string.Format("{0}", m_DebugPanel.name));
            m_Root.transform.SetParent(parent.transform);
            m_Root.transform.localPosition = Vector3.zero;
            m_Root.transform.localScale = Vector3.one;

            UI.VerticalLayoutGroup menuVL = m_Root.AddComponent<UI.VerticalLayoutGroup>();
            menuVL.spacing = 5.0f;
            menuVL.childControlWidth = true;
            menuVL.childControlHeight = true;
            menuVL.childForceExpandWidth = true;
            menuVL.childForceExpandHeight = false;

            RectTransform menuVLRectTransform = m_Root.GetComponent<RectTransform>();
            menuVLRectTransform.pivot = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.localPosition = new Vector3(0.0f, 0.0f);
            menuVLRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.anchorMax = new Vector2(1.0f, 1.0f);

            BuildGUIImpl(m_Root);
        }

        public void RebuildGUI()
        {
            for (int i = 0; i < m_Root.transform.childCount; ++i)
            {
                Object.DestroyImmediate(m_Root.transform.GetChild(i).gameObject);
            }
            BuildGUIImpl(m_Root);
        }

        // Default Implementation: just build all items with provided handler.
        public virtual void BuildGUIImpl(GameObject parent)
        {
            DebugMenuUI.CreateTextElement(string.Format("{0} Title", m_DebugPanel.name), m_DebugPanel.name, 14, TextAnchor.MiddleLeft, m_Root);

            m_ItemsUI.Clear();
            for (int i = 0; i < m_DebugPanel.itemCount; i++)
            {
                DebugItemHandler handler = m_DebugPanel.GetDebugItem(i).handler; // Should never be null, we have at least the default handler
                m_ItemsUI.Add(handler.BuildGUI(parent));
            }
        }

#if UNITY_EDITOR
        // Default implementation for editor UI of a debug panel. Users may override this behavior.
        // This will just display all items via their specific handlers in a vertical layout.
        public virtual void OnEditorGUI()
        {
            using (new UnityEditor.EditorGUILayout.VerticalScope())
            {
                for (int i = 0; i < m_DebugPanel.itemCount; ++i)
                {
                    m_DebugPanel.GetDebugItem(i).handler.OnEditorGUI();
                }
            }
        }
#endif

        public DebugItem GetSelectedDebugItem()
        {
            if (m_SelectedItem != -1)
            {
                return m_DebugPanel.GetDebugItem(m_SelectedItem);
            }

            return null;
        }

        void SetSelectedItem(int index)
        {
            if (m_SelectedItem != -1)
            {
                m_ItemsUI[m_SelectedItem].SetSelected(false);
            }

            m_SelectedItem = index;
            m_ItemsUI[m_SelectedItem].SetSelected(true);
        }

        public void SetSelected(bool value)
        {
            m_Root.SetActive(value);

            if (value)
            {
                if (m_SelectedItem == -1)
                {
                    NextItem();
                }
                else
                    SetSelectedItem(m_SelectedItem);
            }
        }

        void NextItem()
        {
            if (m_ItemsUI.Count != 0)
            {
                int newSelected = (m_SelectedItem + 1) % m_ItemsUI.Count;
                SetSelectedItem(newSelected);
            }
        }

        void PreviousItem()
        {
            if (m_ItemsUI.Count != 0)
            {
                int newSelected = m_SelectedItem - 1;
                if (newSelected == -1)
                    newSelected = m_ItemsUI.Count - 1;
                SetSelectedItem(newSelected);
            }
        }

        public void OnMoveHorizontal(float value)
        {
            if (m_SelectedItem != -1 && !m_DebugPanel.GetDebugItem(m_SelectedItem).readOnly)
            {
                if (value > 0.0f)
                    m_ItemsUI[m_SelectedItem].OnIncrement();
                else
                    m_ItemsUI[m_SelectedItem].OnDecrement();
            }
        }

        public void OnMoveVertical(float value)
        {
            if (value > 0.0f)
                PreviousItem();
            else
                NextItem();
        }

        public void OnValidate()
        {
            if (m_SelectedItem != -1 && !m_DebugPanel.GetDebugItem(m_SelectedItem).readOnly)
                m_ItemsUI[m_SelectedItem].OnValidate();
        }

        public void Update()
        {
            foreach (var itemUI in m_ItemsUI)
            {
                if (itemUI.dynamicDisplay)
                    itemUI.Update();
            }
        }
    }
}