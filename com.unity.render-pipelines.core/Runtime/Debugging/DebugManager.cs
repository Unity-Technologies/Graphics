using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Assertions;
using UnityEngine.Rendering.UI;

namespace UnityEngine.Rendering
{
    using UnityObject = UnityEngine.Object;

    /// <summary>
    /// IDebugData interface.
    /// </summary>
    public interface IDebugData
    {
        /// <summary>Get the reset callback for this DebugData</summary>
        /// <returns>The reset callback</returns>
        Action GetReset();
        //Action GetLoad();
        //Action GetSave();
    }

    /// <summary>
    /// Manager class for the Debug Window.
    /// </summary>
    public sealed partial class DebugManager
    {
        static readonly Lazy<DebugManager> s_Instance = new Lazy<DebugManager>(() => new DebugManager());
        /// <summary>
        /// Global instance of the DebugManager.
        /// </summary>
        public static DebugManager instance => s_Instance.Value;

        ReadOnlyCollection<DebugUI.Panel> m_ReadOnlyPanels;
        readonly List<DebugUI.Panel> m_Panels = new List<DebugUI.Panel>();

        void UpdateReadOnlyCollection()
        {
            m_Panels.Sort();
            m_ReadOnlyPanels = m_Panels.AsReadOnly();
        }

        /// <summary>
        /// List of currently registered debug panels.
        /// </summary>
        public ReadOnlyCollection<DebugUI.Panel> panels
        {
            get
            {
                if (m_ReadOnlyPanels == null)
                    UpdateReadOnlyCollection();
                return m_ReadOnlyPanels;
            }
        }

        /// <summary>
        /// Callback called when the runtime UI changed.
        /// </summary>
        public event Action<bool> onDisplayRuntimeUIChanged = delegate {};
        /// <summary>
        /// Callback called when the debug window is dirty.
        /// </summary>
        public event Action onSetDirty = delegate {};

        event Action resetData;

        /// <summary>
        /// Force an editor request.
        /// </summary>
        public bool refreshEditorRequested;

        GameObject m_Root;
        DebugUIHandlerCanvas m_RootUICanvas;

        GameObject m_PersistentRoot;
        DebugUIHandlerPersistentCanvas m_RootUIPersistentCanvas;

        // Knowing if the DebugWindows is open, is done by event as it is in another assembly.
        // The DebugWindows is responsible to link its event to ToggleEditorUI.
        bool m_EditorOpen = false;
        /// <summary>
        /// Is the debug editor window open.
        /// </summary>
        public bool displayEditorUI => m_EditorOpen;
        /// <summary>
        /// Toggle the debug window.
        /// </summary>
        /// <param name="open">State of the debug window.</param>
        public void ToggleEditorUI(bool open) => m_EditorOpen = open;

        private bool m_EnableRuntimeUI = true;

        /// <summary>
        /// Controls whether runtime UI can be enabled. When this is set to false, there will be no overhead
        /// from debug GameObjects or runtime initialization.
        /// </summary>
        public bool enableRuntimeUI
        {
            get => m_EnableRuntimeUI;
            set
            {
                if (value != m_EnableRuntimeUI)
                {
                    m_EnableRuntimeUI = value;
                }
            }
        }

        /// <summary>
        /// Displays the runtime version of the debug window.
        /// </summary>
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

        /// <summary>
        /// Displays the persistent runtime debug window.
        /// </summary>
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

        /// <summary>
        /// Refresh the debug window.
        /// </summary>
        public void RefreshEditor()
        {
            refreshEditorRequested = true;
        }

        /// <summary>
        /// Reset the debug window.
        /// </summary>
        public void Reset()
        {
            resetData?.Invoke();
            ReDrawOnScreenDebug();
        }

        /// <summary>
        /// Redraw the runtime debug UI.
        /// </summary>
        public void ReDrawOnScreenDebug()
        {
            if (displayRuntimeUI)
                m_RootUICanvas?.ResetAllHierarchy();
        }

        /// <summary>
        /// Register debug data.
        /// </summary>
        /// <param name="data">Data to be registered.</param>
        public void RegisterData(IDebugData data) => resetData += data.GetReset();

        /// <summary>
        /// Register debug data.
        /// </summary>
        /// <param name="data">Data to be registered.</param>
        public void UnregisterData(IDebugData data) => resetData -= data.GetReset();

        /// <summary>
        /// Get hashcode state of the Debug Window.
        /// </summary>
        /// <returns></returns>
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

        internal void TogglePersistent(DebugUI.Widget widget)
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
        /// <summary>
        /// Returns a debug panel.
        /// </summary>
        /// <param name="displayName">Name of the debug panel.</param>
        /// <param name="createIfNull">Create the panel if it does not exists.</param>
        /// <param name="groupIndex">Group index.</param>
        /// <param name="overrideIfExist">Replace an existing panel.</param>
        /// <returns></returns>
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
        /// <summary>
        /// Remove a debug panel.
        /// </summary>
        /// <param name="displayName">Name of the debug panel to remove.</param>
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

        /// <summary>
        /// Remove a debug panel.
        /// </summary>
        /// <param name="panel">Reference to the debug panel to remove.</param>
        public void RemovePanel(DebugUI.Panel panel)
        {
            if (panel == null)
                return;

            m_Panels.Remove(panel);
            UpdateReadOnlyCollection();
        }

        /// <summary>
        /// Get a Debug Item.
        /// </summary>
        /// <param name="queryPath">Path of the debug item.</param>
        /// <returns>Reference to the requested debug item.</returns>
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

        /// <summary>
        /// Get a debug item from a specific container.
        /// </summary>
        /// <param name="queryPath">Path of the debug item.</param>
        /// <param name="container">Container to query.</param>
        /// <returns>Reference to the requested debug item.</returns>
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
