using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Assertions;
using UnityEngine.Rendering.UI;

namespace UnityEngine.Rendering
{
    using UnityObject = UnityEngine.Object;

    public interface IDebugData
    {
        Action GetReset();
        //Action GetLoad();
        //Action GetSave();
    }

    public sealed partial class DebugManager
    {
        static readonly Lazy<DebugManager> s_Instance = new Lazy<DebugManager>(() => new DebugManager());
        public static DebugManager instance => s_Instance.Value;

        ReadOnlyCollection<DebugUI.Panel> m_ReadOnlyPanels;
        readonly List<DebugUI.Panel> m_Panels = new List<DebugUI.Panel>();

        void UpdateReadOnlyCollection()
        {
            m_Panels.Sort();
            m_ReadOnlyPanels = m_Panels.AsReadOnly();
        }

        public ReadOnlyCollection<DebugUI.Panel> panels
        {
            get
            {
                if (m_ReadOnlyPanels == null)
                    UpdateReadOnlyCollection();
                return m_ReadOnlyPanels;
            }
        }

        public event Action<bool> onDisplayRuntimeUIChanged = delegate {};
        public event Action onSetDirty = delegate {};

        event Action resetData;

        public bool refreshEditorRequested;

        GameObject m_Root;
        DebugUIHandlerCanvas m_RootUICanvas;

        GameObject m_PersistentRoot;
        DebugUIHandlerPersistentCanvas m_RootUIPersistentCanvas;

        // Knowing if the DebugWindows is open, is done by event as it is in another assembly.
        // The DebugWindows is responsible to link its event to ToggleEditorUI.
        bool m_EditorOpen = false;
        public bool displayEditorUI => m_EditorOpen;
        public void ToggleEditorUI(bool open) => m_EditorOpen = open;

        public bool displayRuntimeUI
        {
            get => m_Root != null && m_Root.activeInHierarchy;
            set
            {
                if (value)
                {
                    m_Root = UnityObject.Instantiate(Resources.Load<Transform>("DebugUI Canvas")).gameObject;
                    m_Root.name = "[Debug Canvas]";
                    m_Root.transform.localPosition = Vector3.zero;
                    m_RootUICanvas = m_Root.GetComponent<DebugUIHandlerCanvas>();
                    m_Root.SetActive(true);
                }
                else
                {
                    CoreUtils.Destroy(m_Root);
                    m_Root = null;
                    m_RootUICanvas = null;
                }

                onDisplayRuntimeUIChanged(value);
            }
        }


        public bool displayPersistentRuntimeUI
        {
            get => m_RootUIPersistentCanvas != null && m_PersistentRoot.activeInHierarchy;
            set
            {
                CheckPersistentCanvas();
                m_PersistentRoot.SetActive(value);
            }
        }

        DebugManager()
        {
            if (!Debug.isDebugBuild)
                return;

            RegisterInputs();
            RegisterActions();
        }

        public void RefreshEditor()
        {
            refreshEditorRequested = true;
        }

        public void Reset()
        {
            resetData?.Invoke();
            ReDrawOnScreenDebug();
        }

        public void ReDrawOnScreenDebug()
        {
            if (displayRuntimeUI)
                m_RootUICanvas?.ResetAllHierarchy();
        }

        public void RegisterData(IDebugData data) => resetData += data.GetReset();

        public void UnregisterData(IDebugData data) => resetData -= data.GetReset();

        public int GetState()
        {
            int hash = 17;

            foreach (var panel in m_Panels)
                hash = hash * 23 + panel.GetHashCode();

            return hash;
        }

        internal void RegisterRootCanvas(DebugUIHandlerCanvas root)
        {
            Assert.IsNotNull(root);
            m_Root = root.gameObject;
            m_RootUICanvas = root;
        }

        internal void ChangeSelection(DebugUIHandlerWidget widget, bool fromNext)
        {
            m_RootUICanvas.ChangeSelection(widget, fromNext);
        }

        void CheckPersistentCanvas()
        {
            if (m_RootUIPersistentCanvas == null)
            {
                var uiManager = UnityObject.FindObjectOfType<DebugUIHandlerPersistentCanvas>();

                if (uiManager == null)
                {
                    m_PersistentRoot = UnityObject.Instantiate(Resources.Load<Transform>("DebugUI Persistent Canvas")).gameObject;
                    m_PersistentRoot.name = "[Debug Canvas - Persistent]";
                    m_PersistentRoot.transform.localPosition = Vector3.zero;
                }
                else
                {
                    m_PersistentRoot = uiManager.gameObject;
                }

                m_RootUIPersistentCanvas = m_PersistentRoot.GetComponent<DebugUIHandlerPersistentCanvas>();
            }
        }

        public void TogglePersistent(DebugUI.Widget widget)
        {
            if (widget == null)
                return;

            var valueWidget = widget as DebugUI.Value;
            if (valueWidget == null)
            {
                Debug.Log("Only DebugUI.Value items can be made persistent.");
                return;
            }

            CheckPersistentCanvas();
            m_RootUIPersistentCanvas.Toggle(valueWidget);
        }

        void OnPanelDirty(DebugUI.Panel panel)
        {
            onSetDirty();
        }

        // TODO: Optimally we should use a query path here instead of a display name
        public DebugUI.Panel GetPanel(string displayName, bool createIfNull = false, int groupIndex = 0, bool overrideIfExist = false)
        {
            DebugUI.Panel p = null;

            foreach (var panel in m_Panels)
            {
                if (panel.displayName == displayName)
                {
                    p = panel;
                    break;
                }
            }

            if (p != null)
            {
                if (overrideIfExist)
                {
                    p.onSetDirty -= OnPanelDirty;
                    RemovePanel(p);
                    p = null;
                }
                else
                    return p;
            }

            if (createIfNull)
            {
                p = new DebugUI.Panel { displayName = displayName, groupIndex = groupIndex };
                p.onSetDirty += OnPanelDirty;
                m_Panels.Add(p);
                UpdateReadOnlyCollection();
            }

            return p;
        }

        // TODO: Use a query path here as well instead of a display name
        public void RemovePanel(string displayName)
        {
            DebugUI.Panel panel = null;

            foreach (var p in m_Panels)
            {
                if (p.displayName == displayName)
                {
                    p.onSetDirty -= OnPanelDirty;
                    panel = p;
                    break;
                }
            }

            RemovePanel(panel);
        }

        public void RemovePanel(DebugUI.Panel panel)
        {
            if (panel == null)
                return;

            m_Panels.Remove(panel);
            UpdateReadOnlyCollection();
        }

        public DebugUI.Widget GetItem(string queryPath)
        {
            foreach (var panel in m_Panels)
            {
                var w = GetItem(queryPath, panel);
                if (w != null)
                    return w;
            }

            return null;
        }

        DebugUI.Widget GetItem(string queryPath, DebugUI.IContainer container)
        {
            foreach (var child in container.children)
            {
                if (child.queryPath == queryPath)
                    return child;

                var containerChild = child as DebugUI.IContainer;
                if (containerChild != null)
                {
                    var w = GetItem(queryPath, containerChild);
                    if (w != null)
                        return w;
                }
            }

            return null;
        }
    }
}
