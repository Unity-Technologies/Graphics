#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
#pragma warning disable 414

    // Manages the debug data even if the window is closed
    class RenderingDebuggerState : ScriptableSingleton<RenderingDebuggerState>
    {
        public WidgetStateDictionary widgetStates;
        public DebugWindowSettings settings;
        public int previousTreeState;

        private void OnEnable()
        {
            Debug.Log("On enable singleton");
            if (settings == null)
                settings = CreateInstance<DebugWindowSettings>();
        }
    }

    [Serializable]
    sealed class WidgetStateDictionary : SerializedDictionary<string, DebugState> { }

    sealed class DebugWindowSettings : ScriptableObject
    {
        // Keep these settings in a separate scriptable object so we can handle undo/redo on them
        // without the rest of the debug window interfering
        public int currentStateHash;
        public int selectedPanel;

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }
    }

    sealed class DebugWindow : EditorWindow, IHasCustomMenu
    {
        static Styles s_Styles;
        static GUIStyle s_SplitterLeft;

        static float splitterPos = 150f;
        const float minSideBarWidth = 100;
        const float minContentWidth = 100;
        bool dragging = false;

        bool m_IsDirty;

        Vector2 m_PanelScroll;
        Vector2 m_ContentScroll;

        static bool s_TypeMapDirty;
        static Dictionary<Type, Type> s_WidgetStateMap; // DebugUI.Widget type -> DebugState type
        static Dictionary<Type, DebugUIDrawer> s_WidgetDrawerMap; // DebugUI.Widget type -> DebugUIDrawer

        static bool s_Open;
        public static bool open
        {
            get => s_Open;
            private set
            {
                if (s_Open ^ value)
                    OnDebugWindowToggled?.Invoke(value);
                s_Open = value;
            }
        }
        static event Action<bool> OnDebugWindowToggled;

        [DidReloadScripts]
        static void OnEditorReload()
        {
            s_TypeMapDirty = true;

            //find if it where open, relink static event end propagate the info
            open = (Resources.FindObjectsOfTypeAll<DebugWindow>()?.Length ?? 0) > 0;
            if (OnDebugWindowToggled == null)
                OnDebugWindowToggled += DebugManager.instance.ToggleEditorUI;
            DebugManager.instance.ToggleEditorUI(open);
        }

        static void RebuildTypeMaps()
        {
            // Map states to widget (a single state can map to several widget types if the value to
            // serialize is the same)
            var attrType = typeof(DebugStateAttribute);
            var stateTypes = CoreUtils.GetAllTypesDerivedFrom<DebugState>()
                .Where(
                    t => t.IsDefined(attrType, false)
                    && !t.IsAbstract
                );

            s_WidgetStateMap = new Dictionary<Type, Type>();

            foreach (var stateType in stateTypes)
            {
                var attr = (DebugStateAttribute)stateType.GetCustomAttributes(attrType, false)[0];

                foreach (var t in attr.types)
                    s_WidgetStateMap.Add(t, stateType);
            }

            // Drawers
            attrType = typeof(DebugUIDrawerAttribute);
            var types = CoreUtils.GetAllTypesDerivedFrom<DebugUIDrawer>()
                .Where(
                    t => t.IsDefined(attrType, false)
                    && !t.IsAbstract
                );

            s_WidgetDrawerMap = new Dictionary<Type, DebugUIDrawer>();

            foreach (var t in types)
            {
                var attr = (DebugUIDrawerAttribute)t.GetCustomAttributes(attrType, false)[0];
                var inst = (DebugUIDrawer)Activator.CreateInstance(t);
                s_WidgetDrawerMap.Add(attr.type, inst);
            }

            // Done
            s_TypeMapDirty = false;
        }

        [MenuItem("Window/Analysis/Rendering Debugger", priority = 10005)]
        static void Init()
        {
            var window = GetWindow<DebugWindow>();
            window.titleContent = Styles.windowTitle;
        }

        [MenuItem("Window/Analysis/Rendering Debugger", validate = true)]
        static bool ValidateMenuItem()
        {
            return RenderPipelineManager.currentPipeline != null;
        }

        void OnEnable()
        {
            Debug.Log("On enable window");
            if (OnDebugWindowToggled == null)
                OnDebugWindowToggled += DebugManager.instance.ToggleEditorUI;

            open = true;

            DebugManager.instance.refreshEditorRequested = false;

            hideFlags = HideFlags.HideAndDontSave;
            autoRepaintOnSceneChange = true;

            // States are ScriptableObjects (necessary for Undo/Redo) but are not saved on disk so when the editor is closed then reopened, any existing debug window will have its states set to null
            // Since we don't care about persistence in this case, we just re-init everything.
            if (RenderingDebuggerState.instance.widgetStates == null || !AreWidgetStatesValid())
                RenderingDebuggerState.instance.widgetStates = new WidgetStateDictionary();

            if (s_WidgetStateMap == null || s_WidgetDrawerMap == null || s_TypeMapDirty)
                RebuildTypeMaps();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
            DebugManager.instance.onSetDirty += MarkDirty;

            // First init
            RenderingDebuggerState.instance.previousTreeState = DebugManager.instance.GetState();
            UpdateWidgetStates();

            EditorApplication.update -= Repaint;
            var panels = DebugManager.instance.panels;
            var selectedPanelIndex = RenderingDebuggerState.instance.settings.selectedPanel;
            if (selectedPanelIndex >= 0
                && selectedPanelIndex < panels.Count
                && panels[selectedPanelIndex].editorForceUpdate)
                EditorApplication.update += Repaint;
        }

        private void PlaymodeStateChanged()
        {
            DebugManager.instance.RefreshEditor();
        }

        // Note: this won't get called if the window is opened when the editor itself is closed
        void OnDestroy()
        {
            open = false;
            DebugManager.instance.onSetDirty -= MarkDirty;
            Undo.ClearUndo(RenderingDebuggerState.instance.settings);

            DestroyWidgetStates();
        }

        public void DestroyWidgetStates()
        {
            if (RenderingDebuggerState.instance.widgetStates == null)
                return;

            // Clear all the states from memory
            foreach (var state in RenderingDebuggerState.instance.widgetStates)
            {
                DestroyState(state.Value);
            }

            RenderingDebuggerState.instance.widgetStates.Clear();
        }

        void DestroyState(DebugState s)
        {
            Undo.ClearUndo(s); // Don't leave dangling states in the global undo/redo stack
            DestroyImmediate(s);
        }

        void DestroyInvalidWidgetStates()
        {
            if (RenderingDebuggerState.instance.widgetStates == null)
                return;

            foreach (var (queryPath, state) in RenderingDebuggerState.instance.widgetStates)
            {
                // Check if the widget is again on the debug manager, meaning that the UI has been recreated with the same one
                var widget = DebugManager.instance.GetItem(queryPath);

                if (widget == null)
                {
                    DestroyState(state);
                }
                else
                {
                    // The type of the widget is still the same?
                    var stateType = s_WidgetStateMap[widget.GetType()];
                    var currentStateType = state.GetType();
                    if (stateType != currentStateType)
                        DestroyState(state);
                }
            }
        }

        bool AreWidgetStatesValid()
        {
            foreach (var state in RenderingDebuggerState.instance.widgetStates)
            {
                if (state.Value == null)
                {
                    return false;
                }
            }
            return true;
        }

        void MarkDirty()
        {
            m_IsDirty = true;
        }

        // We use item states to keep a cached value of each serializable debug items in order to
        // handle domain reloads, play mode entering/exiting and undo/redo
        // Note: no removal of orphan states
        void UpdateWidgetStates()
        {
            foreach (var panel in DebugManager.instance.panels)
                UpdateWidgetStates(panel);
        }

        void UpdateWidgetStates(DebugUI.IContainer container)
        {
            // Skip runtime only containers, we won't draw them so no need to serialize them either
            if (container is DebugUI.Widget actualWidget && actualWidget.isInactiveInEditor)
                return;

            // Recursively update widget states
            foreach (var widget in container.children)
            {
                // Skip non-serializable widgets but still traverse them in case one of their
                // children needs serialization support
                if (widget is DebugUI.IValueField valueField)
                {
                    // Skip runtime & readonly only items
                    if (widget.isInactiveInEditor)
                        return;

                    var widgetType = widget.GetType();
                    string guid = widget.queryPath;

                    // Create missing states & recreate the ones that are null
                    if (s_WidgetStateMap.TryGetValue(widgetType, out Type stateType) && stateType != null)
                    {
                        if (!RenderingDebuggerState.instance.widgetStates.TryGetValue(guid, out var state) || state == null)
                        {
                            var inst = (DebugState)CreateInstance(stateType);
                            inst.queryPath = guid;
                            inst.SetValue(valueField.GetValue(), valueField);
                            RenderingDebuggerState.instance.widgetStates[guid] = inst;
                        }
                    }
                }

                // Recurse if the widget is a container
                if (widget is DebugUI.IContainer containerField)
                    UpdateWidgetStates(containerField);
            }
        }

        void ApplyWidgetStates()
        {
            if (DebugState.m_CurrentDirtyState != null)
            {
                var queryPath = DebugState.m_CurrentDirtyState.queryPath;
                if (DebugManager.instance.GetItem(queryPath) is DebugUI.IValueField widget)
                {
                    widget.SetValue(DebugState.m_CurrentDirtyState.GetValue());
                }

                DebugState.m_CurrentDirtyState = null;
            }
            else
            {
                foreach (var panel in DebugManager.instance.panels)
                    ApplyContainerState(panel);
            }

            DebugState.m_CurrentDirtyState = null;
        }

        void ApplyContainerState(DebugUI.IContainer container)
        {
            // Skip runtime only containers, we won't draw them so no need to serialize them either
            if (container is DebugUI.Widget actualWidget && actualWidget.isInactiveInEditor)
                return;

            // Recursively update widget states
            foreach (var widget in container.children)
            {
                // Skip non-serializable widgets but still traverse them in case one of their
                // children needs serialization support
                if (widget is DebugUI.IValueField valueField)
                {
                    // Skip runtime & readonly only items
                    if (widget.isInactiveInEditor)
                        return;

                    if (RenderingDebuggerState.instance.widgetStates.TryGetValue(widget.queryPath, out var state) && state != null)
                    {
                        valueField.SetValue(state.GetValue());
                    }
                }

                // Recurse if the widget is a container
                if (widget is DebugUI.IContainer containerField)
                    ApplyContainerState(containerField);
            }
        }

        void OnUndoRedoPerformed()
        {
            int stateHash = ComputeStateHash();

            // Something has been undone / redone, re-apply states to the debug tree
            if (stateHash != RenderingDebuggerState.instance.settings.currentStateHash)
            {
                ApplyWidgetStates();
                RenderingDebuggerState.instance.settings.currentStateHash = stateHash;
            }

            Repaint();
        }

        int ComputeStateHash()
        {
            unchecked
            {
                int hash = 13;

                foreach (var state in RenderingDebuggerState.instance.widgetStates)
                    hash = hash * 23 + state.Value.GetHashCode();

                return hash;
            }
        }

        void SyncWidgetsAndStates()
        {
            DestroyInvalidWidgetStates();
            UpdateWidgetStates();
            //ApplyWidgetStates();
        }

        void Update()
        {
            int? requestedPanelIndex = DebugManager.instance.GetRequestedEditorWindowPanelIndex();
            if (requestedPanelIndex != null)
            {
                RenderingDebuggerState.instance.settings.selectedPanel = requestedPanelIndex.Value;
            }

            bool performStateSync = false;

            // If the render pipeline asset has been reloaded we force-refresh widget states in case
            // some debug values need to be refresh/recreated as well (e.g. frame settings on HD)
            if (DebugManager.instance.refreshEditorRequested)
            {
                DebugManager.instance.refreshEditorRequested = false;
                performStateSync = true;
            }

            // if the tree state has changed
            int treeState = DebugManager.instance.GetState();
            if (RenderingDebuggerState.instance.previousTreeState != treeState)
            {
                RenderingDebuggerState.instance.previousTreeState = treeState;
                performStateSync = true;
            }

            // Something is dirty
            if (m_IsDirty)
            {
                m_IsDirty = false;
                performStateSync = true;
            }

            if (performStateSync)
                SyncWidgetsAndStates();
        }

        void OnGUI()
        {
            if (s_Styles == null)
            {
                s_Styles = new Styles();
                s_SplitterLeft = new GUIStyle();
            }

            var panels = DebugManager.instance.panels;
            int itemCount = panels.Count(x => !x.isInactiveInEditor && x.children.Count(w => !w.isInactiveInEditor) > 0);

            if (itemCount == 0)
            {
                EditorGUILayout.HelpBox("No debug item found.", MessageType.Info);
                return;
            }

            // Background color
            var wrect = position;
            wrect.x = 0;
            wrect.y = 0;
            var oldColor = GUI.color;
            GUI.color = s_Styles.skinBackgroundColor;
            GUI.DrawTexture(wrect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;


            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Styles.resetButtonContent, EditorStyles.toolbarButton))
            {
                DebugManager.instance.Reset();
                DestroyWidgetStates();
                UpdateWidgetStates();
                InternalEditorUtility.RepaintAllViews();
            }

            GUILayout.EndHorizontal();

            using (new EditorGUILayout.HorizontalScope())
            {
                // Side bar
                using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_PanelScroll, s_Styles.sectionScrollView, GUILayout.Width(splitterPos)))
                {
                    if (RenderingDebuggerState.instance.settings.selectedPanel >= panels.Count)
                        RenderingDebuggerState.instance.settings.selectedPanel = 0;

                    // Validate container id
                    while (panels[RenderingDebuggerState.instance.settings.selectedPanel].isInactiveInEditor || panels[RenderingDebuggerState.instance.settings.selectedPanel].children.Count(x => !x.isInactiveInEditor) == 0)
                    {
                        RenderingDebuggerState.instance.settings.selectedPanel++;

                        if (RenderingDebuggerState.instance.settings.selectedPanel >= panels.Count)
                            RenderingDebuggerState.instance.settings.selectedPanel = 0;
                    }

                    // Root children are containers
                    for (int i = 0; i < panels.Count; i++)
                    {
                        var panel = panels[i];

                        if (panel.isInactiveInEditor)
                            continue;

                        if (panel.children.Count(x => !x.isInactiveInEditor) == 0)
                            continue;

                        var elementRect = GUILayoutUtility.GetRect(EditorGUIUtility.TrTextContent(panel.displayName), s_Styles.sectionElement, GUILayout.ExpandWidth(true));

                        if (RenderingDebuggerState.instance.settings.selectedPanel == i && Event.current.type == EventType.Repaint)
                            s_Styles.selected.Draw(elementRect, false, false, false, false);

                        EditorGUI.BeginChangeCheck();
                        GUI.Toggle(elementRect, RenderingDebuggerState.instance.settings.selectedPanel == i, panel.displayName, s_Styles.sectionElement);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RegisterCompleteObjectUndo(RenderingDebuggerState.instance.settings, $"Debug Panel '{panel.displayName}' Selection");
                            var previousPanel = RenderingDebuggerState.instance.settings.selectedPanel >= 0 && RenderingDebuggerState.instance.settings.selectedPanel < panels.Count
                                ? panels[RenderingDebuggerState.instance.settings.selectedPanel]
                                : null;
                            if (previousPanel != null && previousPanel.editorForceUpdate && !panel.editorForceUpdate)
                                EditorApplication.update -= Repaint;
                            else if ((previousPanel == null || !previousPanel.editorForceUpdate) && panel.editorForceUpdate)
                                EditorApplication.update += Repaint;
                            RenderingDebuggerState.instance.settings.selectedPanel = i;
                        }
                    }

                    m_PanelScroll = scrollScope.scrollPosition;
                }

                Rect splitterRect = new Rect(splitterPos - 3, 0, 6, Screen.height);
                GUI.Box(splitterRect, "", s_SplitterLeft);

                const float topMargin = 2f;
                GUILayout.Space(topMargin);

                // Main section - traverse current container
                using (var changedScope = new EditorGUI.ChangeCheckScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        const float leftMargin = 4f;
                        GUILayout.Space(leftMargin);
                        var selectedPanel = panels[RenderingDebuggerState.instance.settings.selectedPanel];

                        using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_ContentScroll))
                        {
                            TraverseContainerGUI(selectedPanel);
                            m_ContentScroll = scrollScope.scrollPosition;
                        }
                    }

                    if (changedScope.changed)
                    {
                        RenderingDebuggerState.instance.settings.currentStateHash = ComputeStateHash();
                        DebugManager.instance.ReDrawOnScreenDebug();
                    }
                }

                // Splitter events
                if (Event.current != null)
                {
                    switch (Event.current.rawType)
                    {
                        case EventType.MouseDown:
                            if (splitterRect.Contains(Event.current.mousePosition))
                            {
                                dragging = true;
                            }
                            break;
                        case EventType.MouseDrag:
                            if (dragging)
                            {
                                splitterPos += Event.current.delta.x;
                                splitterPos = Mathf.Clamp(splitterPos, minSideBarWidth, Screen.width - minContentWidth);
                                Repaint();
                            }
                            break;
                        case EventType.MouseUp:
                            if (dragging)
                            {
                                dragging = false;
                            }
                            break;
                    }
                }
                EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            }
        }

        void OnWidgetGUI(DebugUI.Widget widget)
        {
            if (widget.isInactiveInEditor || widget.isHidden)
                return;

            // State will be null for stateless widget
            RenderingDebuggerState.instance.widgetStates.TryGetValue(widget.queryPath, out DebugState state);

            GUILayout.Space(4);

            if (!s_WidgetDrawerMap.TryGetValue(widget.GetType(), out DebugUIDrawer drawer))
            {
                EditorGUILayout.LabelField("Drawer not found (" + widget.GetType() + ").");
            }
            else
            {
                drawer.Begin(widget, state);

                /*if (widget is DebugUI.IValueField valueField)
                    valueField.SetValue(state.GetValue());*/

                if (drawer.OnGUI(widget, state))
                {
                    var container = widget as DebugUI.IContainer;

                    if (container != null)
                        TraverseContainerGUI(container);
                }

                drawer.End(widget, state);
            }
        }

        void TraverseContainerGUI(DebugUI.IContainer container)
        {
            // /!\ SHAAAAAAAME ALERT /!\
            // A container can change at runtime because of the way IMGUI works and how we handle
            // onValueChanged on widget so we have to take this into account while iterating
            try
            {
                foreach (var widget in container.children)
                    OnWidgetGUI(widget);
            }
            catch (InvalidOperationException)
            {
                Repaint();
            }
        }

        public class Styles
        {
            public static float s_DefaultLabelWidth = 0.5f;

            public static GUIContent windowTitle { get; } = EditorGUIUtility.TrTextContent("Rendering Debugger");

            public static GUIContent resetButtonContent { get; } = EditorGUIUtility.TrTextContent("Reset");

            public static GUIStyle foldoutHeaderStyle { get; } = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fixedHeight = 20,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 0, 0)
            };

            public readonly GUIStyle sectionScrollView = "PreferencesSectionBox";
            public readonly GUIStyle sectionElement = new GUIStyle("PreferencesSection");
            public readonly GUIStyle selected = "OL SelectedRow";
            public readonly GUIStyle sectionHeader = new GUIStyle(EditorStyles.largeLabel);
            public readonly Color skinBackgroundColor;

            public static GUIStyle centeredLeft = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            public static float singleRowHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            public static int foldoutColumnWidth = 70;

            public Styles()
            {
                Color textColorDarkSkin = new Color32(210, 210, 210, 255);
                Color textColorLightSkin = new Color32(102, 102, 102, 255);
                Color backgroundColorDarkSkin = new Color32(38, 38, 38, 128);
                Color backgroundColorLightSkin = new Color32(128, 128, 128, 96);

                sectionScrollView = new GUIStyle(sectionScrollView);
                sectionScrollView.overflow.bottom += 1;

                sectionElement.alignment = TextAnchor.MiddleLeft;

                sectionHeader.fontStyle = FontStyle.Bold;
                sectionHeader.fontSize = 18;
                sectionHeader.margin.top = 10;
                sectionHeader.margin.left += 1;
                sectionHeader.normal.textColor = EditorGUIUtility.isProSkin ? textColorDarkSkin : textColorLightSkin;
                skinBackgroundColor = EditorGUIUtility.isProSkin ? backgroundColorDarkSkin : backgroundColorLightSkin;
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Expand All"), false, () => SetExpanded(true));
            menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All"), false, () => SetExpanded(false));
        }

        void SetExpanded(bool value)
        {
            var panels = DebugManager.instance.panels;
            foreach (var p in panels)
            {
                foreach (var w in p.children)
                {
                    if (w.GetType() == typeof(DebugUI.Foldout))
                    {
                        if (RenderingDebuggerState.instance.widgetStates.TryGetValue(w.queryPath, out DebugState state))
                        {
                            var foldout = (DebugUI.Foldout)w;
                            state.SetValue(value, foldout);
                            foldout.SetValue(value);
                        }
                    }
                }
            }
        }
    }

#pragma warning restore 414
}
