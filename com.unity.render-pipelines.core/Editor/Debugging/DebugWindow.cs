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

    sealed class DebugWindow : EditorWindow, IHasCustomMenu
    {
        static Styles s_Styles;
        static GUIStyle s_SplitterLeft;

        static float splitterPos = 150f;
        const float minSideBarWidth = 100;
        const float minContentWidth = 100;
        bool dragging = false;

        [SerializeField]
        int m_DebugTreeState;

        bool m_IsDirty;

        Vector2 m_PanelScroll;
        Vector2 m_ContentScroll;

        static bool s_TypeMapDirty;
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
            // Drawers
            var attrType = typeof(DebugUIDrawerAttribute);
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
            OnDebugWindowToggled ??= DebugManager.instance.ToggleEditorUI;

            open = true;

            DebugManager.instance.refreshEditorRequested = false;

            hideFlags = HideFlags.HideAndDontSave;
            autoRepaintOnSceneChange = true;

            if (s_WidgetDrawerMap == null || s_TypeMapDirty)
                RebuildTypeMaps();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            DebugManager.instance.onSetDirty += MarkDirty;

            // First init
            m_DebugTreeState = DebugManager.instance.GetState();
            DebugWindowGlobalState.instance.UpdateWidgetStates();

            EditorApplication.update -= Repaint;
            var panels = DebugManager.instance.panels;
            var selectedPanelIndex = DebugWindowGlobalState.instance.selectedPanel;
            if (selectedPanelIndex >= 0
                && selectedPanelIndex < panels.Count
                && panels[selectedPanelIndex].editorForceUpdate)
                EditorApplication.update += Repaint;
        }

        // Note: this won't get called if the window is opened when the editor itself is closed
        void OnDestroy()
        {
            open = false;
            DebugManager.instance.onSetDirty -= MarkDirty;
            Undo.ClearUndo(DebugWindowGlobalState.instance);

            DebugWindowGlobalState.instance.DestroyWidgetStates();
        }

        void MarkDirty()
        {
            m_IsDirty = true;
        }

        void OnUndoRedoPerformed()
        {
            int stateHash = DebugWindowGlobalState.instance.ComputeStateHash();

            // Something has been undone / redone, re-apply states to the debug tree
            if (stateHash != DebugWindowGlobalState.instance.currentStateHash)
            {
                DebugWindowGlobalState.instance.ApplyStates(true);
                DebugWindowGlobalState.instance.currentStateHash = stateHash;
            }

            Repaint();
        }

        void Update()
        {
            bool forceUpdateAndApply = false;

            // If the render pipeline asset has been reloaded we force-refresh widget states in case
            // some debug values need to be refresh/recreated as well (e.g. frame settings on HD)
            if (DebugManager.instance.refreshEditorRequested)
            {
                forceUpdateAndApply = true;
                DebugManager.instance.refreshEditorRequested = false;
            }

            int? requestedPanelIndex = DebugManager.instance.GetRequestedEditorWindowPanelIndex();
            if (requestedPanelIndex != null)
            {
                DebugWindowGlobalState.instance.selectedPanel = requestedPanelIndex.Value;
            }

            int treeState = DebugManager.instance.GetState();

            if (forceUpdateAndApply || m_DebugTreeState != treeState || m_IsDirty)
            {
                DebugWindowGlobalState.instance.UpdateWidgetStates();
                DebugWindowGlobalState.instance.ApplyStates(forceApplyAll:forceUpdateAndApply);
                m_DebugTreeState = treeState;
                m_IsDirty = false;
            }
        }

        void OnToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Styles.resetButtonContent, EditorStyles.toolbarButton))
            {
                EditorUtility.DisplayProgressBar(Styles.reset, $"{Styles.reset}...", 1);
                {
                    DebugManager.instance.Reset();
                    DebugWindowGlobalState.instance.DestroyWidgetStates();
                    DebugWindowGlobalState.instance.UpdateWidgetStates();
                    InternalEditorUtility.RepaintAllViews();
                }
                EditorUtility.ClearProgressBar();
            }
            GUILayout.EndHorizontal();
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

            OnToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                // Side bar
                using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_PanelScroll, s_Styles.sectionScrollView, GUILayout.Width(splitterPos)))
                {
                    if (DebugWindowGlobalState.instance.selectedPanel >= panels.Count)
                        DebugWindowGlobalState.instance.selectedPanel = 0;

                    // Validate container id
                    while (panels[DebugWindowGlobalState.instance.selectedPanel].isInactiveInEditor || panels[DebugWindowGlobalState.instance.selectedPanel].children.Count(x => !x.isInactiveInEditor) == 0)
                    {
                        DebugWindowGlobalState.instance.selectedPanel++;

                        if (DebugWindowGlobalState.instance.selectedPanel >= panels.Count)
                            DebugWindowGlobalState.instance.selectedPanel = 0;
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

                        if (DebugWindowGlobalState.instance.selectedPanel == i && Event.current.type == EventType.Repaint)
                            s_Styles.selected.Draw(elementRect, false, false, false, false);

                        EditorGUI.BeginChangeCheck();
                        GUI.Toggle(elementRect, DebugWindowGlobalState.instance.selectedPanel == i, panel.displayName, s_Styles.sectionElement);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RegisterCompleteObjectUndo(DebugWindowGlobalState.instance, $"Debug Panel '{panel.displayName}' Selection");
                            var previousPanel = DebugWindowGlobalState.instance.selectedPanel >= 0 && DebugWindowGlobalState.instance.selectedPanel < panels.Count
                                ? panels[DebugWindowGlobalState.instance.selectedPanel]
                                : null;
                            if (previousPanel != null && previousPanel.editorForceUpdate && !panel.editorForceUpdate)
                                EditorApplication.update -= Repaint;
                            else if ((previousPanel == null || !previousPanel.editorForceUpdate) && panel.editorForceUpdate)
                                EditorApplication.update += Repaint;
                            DebugWindowGlobalState.instance.selectedPanel = i;
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
                        var selectedPanel = panels[DebugWindowGlobalState.instance.selectedPanel];

                        using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_ContentScroll))
                        {
                            TraverseContainerGUI(selectedPanel);
                            m_ContentScroll = scrollScope.scrollPosition;
                        }
                    }

                    if (changedScope.changed)
                    {
                        DebugWindowGlobalState.instance.UpdateStateHash();
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

            if (!s_WidgetDrawerMap.TryGetValue(widget.GetType(), out DebugUIDrawer drawer))
            {
                EditorGUILayout.LabelField("Drawer not found (" + widget.GetType() + ").");
                return;
            }

            if (!DebugWindowGlobalState.instance.states.TryGetValue(widget.queryPath, out DebugState state))
            {
                if (widget is not DebugUI.IContainer)
                {
                    EditorGUILayout.LabelField($"State not found for {widget.queryPath} ({widget.GetType()}).");
                    return;
                }
            }

            EditorGUILayout.Space(2);
            drawer.Begin(widget, state);

            if (drawer.OnGUI(widget, state))
            {
                if (widget is DebugUI.IContainer container)
                    TraverseContainerGUI(container);
            }

            drawer.End(widget, state);
            EditorGUILayout.Space(2);
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

            public const string reset = "Reset";
            public static GUIContent resetButtonContent { get; } = EditorGUIUtility.TrTextContent(reset);

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
                    if (w.GetType() != typeof(DebugUI.Foldout))
                        continue;

                    if (!DebugWindowGlobalState.instance.states.TryGetValue(w.queryPath, out DebugState state))
                        continue;

                    var foldout = (DebugUI.Foldout)w;
                    state.SetValue(value, foldout);
                    foldout.SetValue(value);
                }
            }
        }
    }

#pragma warning restore 414
}
