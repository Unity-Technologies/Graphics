using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// Debug UI Prefab bundle.
    /// </summary>
    [Serializable]
    public class DebugUIPrefabBundle
    {
        /// <summary>type of the widget.</summary>
        public string type;
        /// <summary>Prefab for the widget.</summary>
        public RectTransform prefab;
    }

    /// <summary>
    /// DebugUIHandler for canvas widget.
    /// </summary>
    [CoreRPHelpURL("Rendering-Debugger")]
    public class DebugUIHandlerCanvas : MonoBehaviour
    {
        int m_DebugTreeState;
        Dictionary<Type, Transform> m_PrefabsMap;

        /// <summary>Panel prefab.</summary>
        public Transform panelPrefab;
        /// <summary>List of prefabs.</summary>
        public List<DebugUIPrefabBundle> prefabs;

        List<DebugUIHandlerPanel> m_UIPanels;
        int m_SelectedPanel;
        DebugUIHandlerWidget m_SelectedWidget;
        string m_CurrentQueryPath;

        void OnEnable()
        {
            if (prefabs == null)
                prefabs = new List<DebugUIPrefabBundle>();

            if (m_PrefabsMap == null)
                m_PrefabsMap = new Dictionary<Type, Transform>();

            if (m_UIPanels == null)
                m_UIPanels = new List<DebugUIHandlerPanel>();

            DebugManager.instance.RegisterRootCanvas(this);
        }

        void Update()
        {
            int state = DebugManager.instance.GetState();
            if (m_DebugTreeState != state)
            {
                ResetAllHierarchy();
            }

            HandleInput();

            // Update scroll position in the panel
            if (m_UIPanels != null && m_SelectedPanel < m_UIPanels.Count && m_UIPanels[m_SelectedPanel] != null)
                m_UIPanels[m_SelectedPanel].UpdateScroll();
        }

        internal void RequestHierarchyReset()
        {
            m_DebugTreeState = -1;
        }

        void ResetAllHierarchy()
        {
            foreach (Transform t in transform)
                CoreUtils.Destroy(t.gameObject);

            Rebuild();
        }

        void Rebuild()
        {
            // Check prefab associations
            m_PrefabsMap.Clear();
            foreach (var bundle in prefabs)
            {
                var type = Type.GetType(bundle.type);

                if (type != null && bundle.prefab != null)
                    m_PrefabsMap.Add(type, bundle.prefab);
            }

            m_UIPanels.Clear();

            m_DebugTreeState = DebugManager.instance.GetState();
            var panels = DebugManager.instance.panels;

#if UNITY_ANDROID || UNITY_IPHONE
            // Mobile device safe area
            Rect parentRect = GetComponentInParent<RectTransform>().rect;
            float parentWidth = Math.Min(parentRect.width, parentRect.height);
            float scaleRatio = parentWidth / Math.Min(Screen.height, Screen.width);

            Rect safeAreaRect = Screen.safeArea;
            Vector2 margin = new Vector2(5, 5);
            var safeAreaOffsetLeft = safeAreaRect.xMin * scaleRatio;
            var safeAreaOffsetTop = -safeAreaRect.yMin * scaleRatio;
            Vector2 safeAreaOffset = new Vector2(safeAreaOffsetLeft, safeAreaOffsetTop) + margin;
#endif

            DebugUIHandlerWidget selectedWidget = null;
            foreach (var panel in panels)
            {
                if (panel.isEditorOnly || panel.children.Count(x => !x.isEditorOnly && !x.isHidden) == 0)
                    continue;

                var go = Instantiate(panelPrefab, transform, false).gameObject;
                go.name = panel.displayName;

#if UNITY_ANDROID || UNITY_IPHONE
                RectTransform rectTransform = go.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = safeAreaOffset;
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, safeAreaRect.height * scaleRatio + 2 * safeAreaOffsetTop);
#endif

                var uiPanel = go.GetComponent<DebugUIHandlerPanel>();
                uiPanel.SetPanel(panel);
                uiPanel.Canvas = this;
                m_UIPanels.Add(uiPanel);
                var container = go.GetComponent<DebugUIHandlerContainer>();
                DebugUIHandlerWidget selected = null;
                Traverse(panel, container.contentHolder, null, ref selected);

                if (selected != null && selected.GetWidget().queryPath.Contains(panel.queryPath))
                {
                    selectedWidget = selected;
                }
            }

            ActivatePanel(m_SelectedPanel, selectedWidget);
        }

        void Traverse(DebugUI.IContainer container, Transform parentTransform, DebugUIHandlerWidget parentUIHandler, ref DebugUIHandlerWidget selectedHandler)
        {
            DebugUIHandlerWidget previousUIHandler = null;

            for (int i = 0; i < container.children.Count; i++)
            {
                var child = container.children[i];

                if (child.isEditorOnly || child.isHidden)
                    continue;

                Transform prefab;

                if (!m_PrefabsMap.TryGetValue(child.GetType(), out prefab))
                {
                    foreach (var pair in m_PrefabsMap)
                    {
                        if (pair.Key.IsAssignableFrom(child.GetType()))
                        {
                            prefab = pair.Value;
                            break;
                        }
                    }
                }

                if (prefab == null)
                {
                    Debug.LogWarning("DebugUI widget doesn't have a prefab: " + child.GetType());
                    continue;
                }

                var go = Instantiate(prefab, parentTransform, false).gameObject;
                go.name = child.displayName;
                var uiHandler = go.GetComponent<DebugUIHandlerWidget>();

                if (uiHandler == null)
                {
                    Debug.LogWarning("DebugUI prefab is missing a DebugUIHandler for: " + child.GetType());
                    continue;
                }

                if (!string.IsNullOrEmpty(m_CurrentQueryPath) && child.queryPath.Equals(m_CurrentQueryPath))
                {
                    selectedHandler = uiHandler;
                }

                if (previousUIHandler != null) previousUIHandler.nextUIHandler = uiHandler;
                uiHandler.previousUIHandler = previousUIHandler;
                previousUIHandler = uiHandler;
                uiHandler.parentUIHandler = parentUIHandler;
                uiHandler.SetWidget(child);

                var childContainer = go.GetComponent<DebugUIHandlerContainer>();
                if (childContainer != null && child is DebugUI.IContainer childAsContainer)
                    Traverse(childAsContainer, childContainer.contentHolder, uiHandler, ref selectedHandler);
            }
        }

        DebugUIHandlerWidget GetWidgetFromPath(string queryPath)
        {
            if (string.IsNullOrEmpty(queryPath))
                return null;

            var panel = m_UIPanels[m_SelectedPanel];

            return panel
                .GetComponentsInChildren<DebugUIHandlerWidget>()
                .FirstOrDefault(w => w.GetWidget().queryPath == queryPath);
        }

        void ActivatePanel(int index, DebugUIHandlerWidget selectedWidget = null)
        {
            if (m_UIPanels.Count == 0)
                return;

            if (index >= m_UIPanels.Count)
                index = m_UIPanels.Count - 1;

            m_UIPanels.ForEach(p => p.gameObject.SetActive(false));
            m_UIPanels[index].gameObject.SetActive(true);
            m_SelectedPanel = index;

            if (selectedWidget == null)
                selectedWidget = m_UIPanels[index].GetFirstItem();

            ChangeSelection(selectedWidget, true);
        }

        internal void ChangeSelection(DebugUIHandlerWidget widget, bool fromNext)
        {
            if (widget == null)
                return;

            if (m_SelectedWidget != null)
                m_SelectedWidget.OnDeselection();

            var prev = m_SelectedWidget;
            m_SelectedWidget = widget;
            SetScrollTarget(widget);

            if (!m_SelectedWidget.OnSelection(fromNext, prev))
            {
                if (fromNext)
                    SelectNextItem();
                else
                    SelectPreviousItem();
            }
            else
            {
                if (m_SelectedWidget == null || m_SelectedWidget.GetWidget() == null)
                    m_CurrentQueryPath = string.Empty;
                else
                    m_CurrentQueryPath = m_SelectedWidget.GetWidget().queryPath;
            }
        }

        internal void SelectPreviousItem()
        {
            if (m_SelectedWidget == null)
                return;

            var newSelection = m_SelectedWidget.Previous();

            if (newSelection != null)
                ChangeSelection(newSelection, false);
        }

        internal void SelectNextPanel()
        {
            int index = m_SelectedPanel + 1;
            if (index >= m_UIPanels.Count)
                index = 0;
            index = Mathf.Clamp(index, 0, m_UIPanels.Count - 1);
            ActivatePanel(index);
        }

        internal void SelectPreviousPanel()
        {
            int index = m_SelectedPanel - 1;
            if (index < 0)
                index = m_UIPanels.Count - 1;
            index = Mathf.Clamp(index, 0, m_UIPanels.Count - 1);
            ActivatePanel(index);
        }

        internal void SelectNextItem()
        {
            if (m_SelectedWidget == null)
                return;

            var newSelection = m_SelectedWidget.Next();

            if (newSelection != null)
                ChangeSelection(newSelection, true);
        }

        void ChangeSelectionValue(float multiplier)
        {
            if (m_SelectedWidget == null)
                return;

            bool fast = DebugManager.instance.GetAction(DebugAction.Multiplier) != 0f;

            if (multiplier < 0f)
                m_SelectedWidget.OnDecrement(fast);
            else
                m_SelectedWidget.OnIncrement(fast);
        }

        void ActivateSelection()
        {
            if (m_SelectedWidget == null)
                return;

            m_SelectedWidget.OnAction();
        }

        void HandleInput()
        {
            if (DebugManager.instance.GetAction(DebugAction.PreviousDebugPanel) != 0f)
            {
                SelectPreviousPanel();
            }

            if (DebugManager.instance.GetAction(DebugAction.NextDebugPanel) != 0f)
            {
                SelectNextPanel();
            }

            if (DebugManager.instance.GetAction(DebugAction.Action) != 0f)
                ActivateSelection();

            if (DebugManager.instance.GetAction(DebugAction.MakePersistent) != 0f && m_SelectedWidget != null)
                DebugManager.instance.TogglePersistent(m_SelectedWidget.GetWidget());

            float moveHorizontal = DebugManager.instance.GetAction(DebugAction.MoveHorizontal);
            if (moveHorizontal != 0f)
                ChangeSelectionValue(moveHorizontal);

            float moveVertical = DebugManager.instance.GetAction(DebugAction.MoveVertical);
            if (moveVertical != 0f)
            {
                if (moveVertical < 0f)
                    SelectNextItem();
                else
                    SelectPreviousItem();
            }
        }

        internal void SetScrollTarget(DebugUIHandlerWidget widget)
        {
            if (m_UIPanels != null && m_SelectedPanel < m_UIPanels.Count && m_UIPanels[m_SelectedPanel] != null)
                m_UIPanels[m_SelectedPanel].SetScrollTarget(widget);
        }
    }
}
