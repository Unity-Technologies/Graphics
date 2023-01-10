using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.Rendering.UI;

#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH
using UnityEngine.UI;
#endif

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
        public event Action<bool> onDisplayRuntimeUIChanged = delegate { };
        /// <summary>
        /// Callback called when the debug window is dirty.
        /// </summary>
        public event Action onSetDirty = delegate { };

        event Action resetData;

        /// <summary>
        /// Force an editor request.
        /// </summary>
        public bool refreshEditorRequested;

        int? m_RequestedPanelIndex;

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
                    DebugUpdater.SetEnabled(value);
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
                    m_Root = UnityObject.Instantiate(Resources.Load<Transform>("DebugUICanvas")).gameObject;
                    m_Root.name = "[Debug Canvas]";
                    m_Root.transform.localPosition = Vector3.zero;
                    m_RootUICanvas = m_Root.GetComponent<DebugUIHandlerCanvas>();

#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH
                    var canvasScaler = m_Root.GetComponent<CanvasScaler>();
                    canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
#endif

                    m_Root.SetActive(true);
                }
                else
                {
                    CoreUtils.Destroy(m_Root);
                    m_Root = null;
                    m_RootUICanvas = null;
                }

                onDisplayRuntimeUIChanged(value);
                DebugUpdater.HandleInternalEventSystemComponents(value);
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
                if (value)
                {
                    EnsurePersistentCanvas();
                }
                else
                {
                    CoreUtils.Destroy(m_PersistentRoot);
                    m_PersistentRoot = null;
                    m_RootUIPersistentCanvas = null;
                }
            }
        }

        /// <summary>
        /// Is any debug window or UI currently active.
        /// </summary>
        public bool isAnyDebugUIActive
        {
            get
            {
                return
                    displayRuntimeUI || displayPersistentRuntimeUI
#if UNITY_EDITOR
                    || displayEditorUI
#endif
                    ;
            }
        }

        DebugManager()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            RegisterInputs();
            RegisterActions();
#endif
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
        /// Request the runtime debug UI be redrawn on the next update.
        /// </summary>
        public void ReDrawOnScreenDebug()
        {
            if (displayRuntimeUI)
                m_RootUICanvas?.RequestHierarchyReset();
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

        internal void SetScrollTarget(DebugUIHandlerWidget widget)
        {
            if (m_RootUICanvas != null)
                m_RootUICanvas.SetScrollTarget(widget);
        }

        void EnsurePersistentCanvas()
        {
            if (m_RootUIPersistentCanvas == null)
            {
                var uiManager = UnityObject.FindFirstObjectByType<DebugUIHandlerPersistentCanvas>();

                if (uiManager == null)
                {
                    m_PersistentRoot = UnityObject.Instantiate(Resources.Load<Transform>("DebugUIPersistentCanvas")).gameObject;
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

        internal void TogglePersistent(DebugUI.Widget widget, int? forceTupleIndex = null)
        {
            if (widget == null)
                return;

            EnsurePersistentCanvas();

            switch (widget)
            {
                case DebugUI.Value value:
                    m_RootUIPersistentCanvas.Toggle(value);
                    break;
                case DebugUI.ValueTuple valueTuple:
                    m_RootUIPersistentCanvas.Toggle(valueTuple, forceTupleIndex);
                    break;
                case DebugUI.Container container:
                    // When container is toggled, we make sure that if there are ValueTuples, they all get the same element index.
                    int pinnedIndex = container.children.Max(w => (w as DebugUI.ValueTuple)?.pinnedElementIndex ?? -1);
                    foreach (var child in container.children)
                    {
                        if (child is DebugUI.Value || child is DebugUI.ValueTuple)
                            TogglePersistent(child, pinnedIndex);
                    }
                    break;
                default:
                    Debug.Log("Only readonly items can be made persistent.");
                    break;
            }
        }

        void OnPanelDirty(DebugUI.Panel panel)
        {
            onSetDirty();
        }

        /// <summary>
        /// Returns the panel index
        /// </summary>
        /// <param name="displayName">The displayname for the panel</param>
        /// <returns>The index for the panel or -1 if not found.</returns>
        public int PanelIndex([DisallowNull] string displayName)
        {
            displayName ??= string.Empty;

            for (int i = 0; i < m_Panels.Count; ++i)
            {
                if (displayName.Equals(m_Panels[i].displayName, StringComparison.InvariantCultureIgnoreCase))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Request DebugWindow to open the specified panel.
        /// </summary>
        /// <param name="index">Index of the debug window panel to activate.</param>
        public void RequestEditorWindowPanelIndex(int index)
        {
            // Similar to RefreshEditor(), this function is required to bypass a dependency problem where DebugWindow
            // cannot be accessed from the Core.Runtime assembly. Should there be a better way to allow editor-dependent
            // features in DebugUI?
            m_RequestedPanelIndex = index;
        }

        internal int? GetRequestedEditorWindowPanelIndex()
        {
            int? requestedIndex = m_RequestedPanelIndex;
            m_RequestedPanelIndex = null;
            return requestedIndex;
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
            int panelIndex = PanelIndex(displayName);
            DebugUI.Panel p = panelIndex >= 0 ? m_Panels[panelIndex] : null;

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

        /// <summary>
        /// Find the index of the panel from it's display name.
        /// </summary>
        /// <param name="displayName">The display name of the panel to find.</param>
        /// <returns>The index of the panel in the list. -1 if not found.</returns>
        public int FindPanelIndex(string displayName)
            => m_Panels.FindIndex(p => p.displayName == displayName);

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
        /// Gets an <see cref="DebugUI.Widget[]"/> matching the given <see cref="DebugUI.Flags"/>
        /// </summary>
        /// <param name="flags">The flags of the widget</param>
        /// <returns>Reference to the requested debug item.</returns>
        public DebugUI.Widget[] GetItems(DebugUI.Flags flags)
        {
            using (ListPool<DebugUI.Widget>.Get(out var temp))
            {
                foreach (var panel in m_Panels)
                {
                    var widgets = GetItemsFromContainer(flags, panel);
                    temp.AddRange(widgets);
                }

                return temp.ToArray();
            }
        }

        internal DebugUI.Widget[] GetItemsFromContainer(DebugUI.Flags flags, DebugUI.IContainer container)
        {
            using (ListPool<DebugUI.Widget>.Get(out var temp))
            {
                foreach (var child in container.children)
                {
                    if (child.flags.HasFlag(flags))
                    {
                        temp.Add(child);
                        continue;
                    }

                    if (child is DebugUI.IContainer containerChild)
                    {
                        temp.AddRange(GetItemsFromContainer(flags, containerChild));
                    }
                }

                return temp.ToArray();
            }
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

                if (child is DebugUI.IContainer containerChild)
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
