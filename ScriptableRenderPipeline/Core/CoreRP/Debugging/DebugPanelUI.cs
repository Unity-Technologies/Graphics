using System.Collections.Generic;
using System;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugPanelUI
    {
        protected GameObject        m_Root = null;
        protected DebugPanel        m_DebugPanel = null;
        protected List<DebugItemUI> m_ItemsUI = new List<DebugItemUI>();
        protected int               m_SelectedItem = -1;

        public bool empty { get { return m_DebugPanel.itemCount == 0; } }

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
            if (m_Root == null)
                return;

            foreach (Transform child in m_Root.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            m_ItemsUI.Clear();

            BuildGUIImpl(m_Root);
        }

        protected void AddDebugItemUI(DebugItem item, GameObject parent)
        {
#if UNITY_EDITOR
            // We don't want runtime only items even in the "player" debug menu if we are in the editor.
            if (item.runtimeOnly)
                return;
#endif
            if(item.editorOnly)
                return;

            DebugItemUI itemUI = item.handler.BuildGUI(parent);
            if(itemUI == null)
            {
                Debug.LogWarning(String.Format("DebugItemUI not provided for item {0} of type {1}.\n Did you implement BuildGUI for your custom Handler?", item.name, item.type));
            }
            else
            {
                m_ItemsUI.Add(itemUI);
            }
        }

        // Default Implementation: just build all items with provided handler.
        public virtual void BuildGUIImpl(GameObject parent)
        {
            DebugMenuUI.CreateTextElement(string.Format("{0} Title", m_DebugPanel.name), m_DebugPanel.name, 14, TextAnchor.MiddleLeft, parent);

            for (int i = 0; i < m_DebugPanel.itemCount; i++)
            {
                DebugItem item = m_DebugPanel.GetDebugItem(i);
                AddDebugItemUI(item, parent);
            }
        }

#if UNITY_EDITOR
        // Default implementation for editor UI of a debug panel. Users may override this behavior.
        // This will just display all items via their specific handlers in a vertical layout.
        Vector2 m_ScrollViewPos;

        public virtual void OnEditorGUI()
        {
            using (var scope = new UnityEditor.EditorGUILayout.ScrollViewScope(m_ScrollViewPos))
            {
                using (new UnityEditor.EditorGUILayout.VerticalScope())
                {
                    for (int i = 0; i < m_DebugPanel.itemCount; ++i)
                    {
                        m_DebugPanel.GetDebugItem(i).handler.OnEditorGUI();
                    }
                }

                m_ScrollViewPos = scope.scrollPosition;
            }
        }
#endif

        public DebugItem GetSelectedDebugItem()
        {
            if (m_SelectedItem != -1)
            {
                return m_ItemsUI[m_SelectedItem].debugItem;
            }

            return null;
        }

        public void ResetSelectedItem()
        {
            SetSelectedItem(-1);
        }

        void SetSelectedItem(int index)
        {
            if (m_SelectedItem != -1)
            {
                m_ItemsUI[m_SelectedItem].SetSelected(false);
            }

            m_SelectedItem = index;
            if(m_SelectedItem != -1)
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
            if (m_SelectedItem != -1 && !m_ItemsUI[m_SelectedItem].debugItem.readOnly)
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
            if (m_SelectedItem != -1 && !m_ItemsUI[m_SelectedItem].debugItem.readOnly)
                m_ItemsUI[m_SelectedItem].OnValidate();
        }

        public void Update()
        {
            // A bit dirty... this will happen when we exit playmode.
            // The problem happens when the persistent menu is not empty and we leave playmode.
            // In this case, the gameObjects will be destroyed but not the ItemUIs (because we can't know when we exit playmode)
            // To avoid accessing destroyed GameObjects we test the root...
            if (m_Root == null)
                return;

            foreach (var itemUI in m_ItemsUI)
            {
                if (itemUI.dynamicDisplay)
                    itemUI.Update();
            }
        }
    }
}
