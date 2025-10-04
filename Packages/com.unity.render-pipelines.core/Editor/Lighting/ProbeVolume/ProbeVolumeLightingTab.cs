using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Button = UnityEngine.UIElements.Button;

namespace UnityEngine.Rendering
{
    class ProbeVolumeLightingTab : LightingWindowTab
    {
        const int k_AddSceneID = 654423;

        static string documentationURL = "probevolumes";

        static class Styles
        {
            public static readonly GUIContent helpIcon = EditorGUIUtility.IconContent("_Help");
            public static readonly GUIContent settingsIcon = EditorGUIUtility.IconContent("_Popup");
            public static readonly GUIContent debugIcon = EditorGUIUtility.IconContent("d_debug");

            public static readonly GUIContent lightingSettings = new GUIContent("Lighting Settings Asset");
            public static readonly GUIContent bakingTitle = new GUIContent("Baking");

            public static readonly GUIContent bakingMode = new GUIContent("Baking Mode", "In Single Scene mode, only the Active Scene will be baked. Adaptive Probe Volumes in other Scenes will be ignored.");
            public static readonly GUIContent currentBakingSet = new GUIContent("Current Baking Set");
            public static readonly GUIContent scenesInSet = new GUIContent("Scenes in Baking Set");
            public static readonly GUIContent addLoadedScenes = new GUIContent("Add Loaded Scenes");
            public static readonly GUIContent toggleBakeAll = new GUIContent("Toggle All");
            public static readonly GUIContent toggleBakeNone = new GUIContent("Toggle None");
            public static readonly GUIContent status = new GUIContent("Status", "Unloaded scenes will not be considered when generating lighting data.");
            public static readonly GUIContent bake = new GUIContent("Bake", "Scenes loaded but not selected for Baking will contribute to lighting but baked data will not be regenerated for these scenes.");
            public static readonly GUIContent bakeBox = new GUIContent("", "Controls if Adaptive Probe Volumes in this scene are baked when Generating Lighting.");
            public static readonly GUIContent warnings = new GUIContent("Warnings");

            public static readonly string[] bakingModeOptions = new string[] { "Single Scene", "Baking Set" };

            public static readonly GUIContent iconEnableAll = new GUIContent("", CoreEditorStyles.GetMessageTypeIcon(MessageType.Info), "The Scene is loaded but is currently not enabled for Baking. It will therefore not be considered when generating lighting data.");
            public static readonly GUIContent iconLoadForBake = new GUIContent("", CoreEditorStyles.GetMessageTypeIcon(MessageType.Warning), "The Scene is currently enabled for baking but is unloaded in the Hierarchy. This may result in incomplete lighting data being generated.\nLoad the Scene in the Hierarchy, or use the shortcuts below to fix the issue.");

            public static readonly string msgEnableAll = "Some loaded Scenes are disabled by this Baking Set. These Scenes will not contribute to the generation of probe data.";
            public static readonly string msgUnloadOther = "Scene(s) not belonging to this Baking Set are currently loaded in the Hierarchy. This might result in incorrect lighting.";
            public static readonly string msgLoadForBake = "Some scene(s) in this Baking Set are not currently loaded in the Hierarchy. This might result in missing or incomplete lighting.";

            public const float statusLabelWidth = 80;

            // Summary
            public static readonly GUIContent scenarioCostStat = new GUIContent("Scenario Size", "Size of the current Scenario's lighting data.");
            public static readonly GUIContent totalCostStat = new GUIContent("Baking Set Size", "Size of the lighting data for all Scenarios in this Baking Set.");

            // Bake Button
            public static readonly GUIContent generateLighting = new GUIContent("Generate Lighting");
            public static readonly GUIContent generateAPV = new GUIContent("Bake Probe Volumes", "Calculate probe positions and generate lighting data for Adaptive Probe Volumes.");
            public static readonly GUIContent cancelBake = new GUIContent("Cancel", "Cancel current Adaptive Probe Volumes baking task.");
            public static readonly string[] bakeOptionsText = { "Bake Probe Volumes", "Bake Reflection Probes", "Clear Baked Data" };

            public static readonly GUIStyle buttonStyle = "LargeButton";
            public const float lightingButtonWidth = 170;

            // Font styles
            public static readonly GUIStyle inspectorTitle = "IN Title";
            public static readonly GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            public static readonly GUIStyle labelFont = new GUIStyle(EditorStyles.label);
        }

        internal enum Expandable
        {
            Baking = 1 << 0,
            BakingWarnings = 1 << 1,
            Scenarios = 1 << 2,
            Placement = 1 << 3,
            PlacementFilters = 1 << 4,
            InvaliditySettings = 1 << 5,
            SettingsSkyOcclusion = 1 << 8,
            SettingsRenderingLayers = 1 << 9,
        };

        static readonly Expandable k_ExpandableDefault = Expandable.Baking | Expandable.BakingWarnings | Expandable.Scenarios | Expandable.Placement | Expandable.InvaliditySettings;
        static ExpandedState<Expandable, ProbeVolumeBakingProcessSettings> k_Foldouts;

        // This set is used to draw a read only inspector in case no other set has been created
        // It's content must not be changed
        static ProbeVolumeBakingSet s_DefaultSet;
        static ProbeVolumeBakingSet defaultSet
        {
            get
            {
                if (s_DefaultSet == null)
                {
                    s_DefaultSet = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
                    s_DefaultSet.hideFlags = HideFlags.NotEditable | HideFlags.HideAndDontSave;
                    s_DefaultSet.SetDefaults();
                }
                return s_DefaultSet;
            }
        }

        public static ProbeVolumeLightingTab instance = new();

        public static bool singleSceneMode => instance?.m_SingleSceneMode ?? true;


        Vector2 m_ScrollPosition = Vector2.zero;
        bool m_SingleSceneMode = true;
        bool m_TempBakingSet = false;
        bool m_Initialized = false;

        ProbeVolumeBakingSetWeakReference m_ActiveSet = new();
        ProbeVolumeBakingSet activeSet
        {
            get => m_ActiveSet.Get();
            set
            {
                if (ReferenceEquals(m_ActiveSet.Get(), value)) return;
                if (m_TempBakingSet) Object.DestroyImmediate(m_ActiveSet.Get());
                m_ActiveSet.Set(value);
                m_TempBakingSet = false;
                if (m_ActiveSet.Get() == null) return;
                m_SingleSceneMode = m_ActiveSet.Get().singleSceneMode;
                InitializeSceneList();
            }
        }

        Editor m_ActiveSetEditor;
        ProbeVolumeBakingSetEditor activeSetEditor
        {
            get
            {
                var set = activeSet != null ? activeSet : defaultSet;
                Editor.CreateCachedEditor(set, typeof(ProbeVolumeBakingSetEditor), ref m_ActiveSetEditor);
                return (ProbeVolumeBakingSetEditor)m_ActiveSetEditor;
            }
        }

        /*
        SerializedObject m_LightmapSettings;
        SerializedProperty m_LightingSettingsAsset;
        SerializedObject lightmapSettings
        {
            get
            {
                // if we set a new scene as the active scene, we need to make sure to respond to those changes
                if (m_LightmapSettings == null || m_LightmapSettings.targetObject != GetLightmapSettings())
                {
                    m_LightmapSettings = new SerializedObject(GetLightmapSettings());
                    m_LightingSettingsAsset = m_LightmapSettings.FindProperty("m_LightingSettings");
                }

                return m_LightmapSettings;
            }
        }
        */

        public override void OnEnable()
        {
            instance = this;
            titleContent = new GUIContent("Adaptive Probe Volumes");
            priority = 1;

            RefreshSceneAssets();
        }

        bool FindActiveSet()
        {
            if (m_ActiveSet.Get() == null)
            {
                activeSet = ProbeVolumeBakingSet.GetBakingSetForScene(SceneManager.GetActiveScene());
                for (int i = 0; activeSet == null && i < SceneManager.sceneCount; i++)
                    activeSet = ProbeVolumeBakingSet.GetBakingSetForScene(SceneManager.GetSceneAt(i));
            }

            return m_ActiveSet.Get() != null;
        }

        void Initialize()
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
            {
                ProbeVolumeEditor.APVDisabledHelpBox();
                EditorGUILayout.Space();
                return;
            }

            ProbeVolumeEditor.FrameSettingDisabledHelpBox();

            if (m_Initialized)
                return;

            FindActiveSet();

            EditorSceneManager.sceneOpened += OnSceneOpened;

            m_Initialized = true;
        }

        public override void OnDisable()
        {
            if (m_ActiveSetEditor != null)
                Object.DestroyImmediate(m_ActiveSetEditor);

            EditorSceneManager.sceneOpened -= OnSceneOpened;

            // We keep allocated acceleration structures while the Lighting window is open in order to make subsequent bakes faster, but when the window closes we dispose of them
            // Unless a bake is running, in which case we leave disposing to CleanBakeData()
            if (!AdaptiveProbeVolumes.isRunning && !Lightmapping.isRunning)
                AdaptiveProbeVolumes.Dispose();
        }

        #region On GUI
        public override void OnGUI()
        {
            EditorGUIUtility.hierarchyMode = true;

            Initialize();

            var prv = ProbeReferenceVolume.instance;

            // In single scene mode, user can't control active set, so we automatically create a new one
            // in case the active scene doesn't have a baking set so that we can display baking settings
            // Clone the current activeSet if possible so that it's seamless when eg. duplicating a scene
            if (activeSet != null && m_SingleSceneMode)
            {
                var activeScene = SceneManager.GetActiveScene();
                var set = ProbeVolumeBakingSet.GetBakingSetForScene(activeScene);
                if (set == null)
                    UseTemporaryBakingSet(activeScene.GetGUID(), activeSet ? activeSet.Clone() : null);
            }

            // Not sure how we can get to that state but we can
            if (m_TempBakingSet && activeSet == null)
                FindActiveSet();

            using (new EditorGUI.DisabledScope(!prv.isInitialized || !prv.enabledBySRP))
            {
                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

                //lightmapSettings.Update();
                //using (new EditorGUI.IndentLevelScope())
                //    EditorGUILayout.PropertyField(m_LightingSettingsAsset, Styles.lightingSettings);
                //lightmapSettings.ApplyModifiedProperties();
                //EditorGUILayout.Space();

                if (Foldout(Styles.bakingTitle, Expandable.Baking, true))
                {
                    EditorGUI.indentLevel++;
                    BakingGUI();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }

                using (new EditorGUI.DisabledScope(activeSet == null && !m_TempBakingSet))
                    activeSetEditor.OnInspectorGUI();

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();
        }

        public override bool HasHelpGUI()
        {
            return true;
        }

        public override void OnHeaderSettingsGUI()
        {
            var iconSize = EditorStyles.iconButton.CalcSize(Styles.helpIcon);
            if (GUI.Button(GUILayoutUtility.GetRect(iconSize.x, iconSize.y), Styles.helpIcon, EditorStyles.iconButton))
                Help.BrowseURL(DocumentationInfo.GetPageLink("com.unity.render-pipelines.high-definition", documentationURL));

            iconSize = EditorStyles.iconButton.CalcSize(Styles.settingsIcon);
            var rect = GUILayoutUtility.GetRect(iconSize.x, iconSize.y);
            if (EditorGUI.DropdownButton(rect, Styles.settingsIcon, FocusType.Passive, EditorStyles.iconButton))
                EditorUtility.DisplayCustomMenu(rect, new[] { EditorGUIUtility.TrTextContent("Open Rendering Debugger") }, -1, OpenProbeVolumeDebugPanel, null);

            //var style = new GUIStyle(EditorStyles.iconButton);
            //style.padding = new RectOffset(1, 1, 1, 1);
            //if (GUI.Button(rect, Styles.debugIcon, style))
            //    OpenProbeVolumeDebugPanel(null, null, 0);
        }

        internal static void OpenProbeVolumeDebugPanel(object userData, string[] options, int selected)
        {
            var debugPanel = EditorWindow.GetWindow<DebugWindow>();
            debugPanel.titleContent = DebugWindow.Styles.windowTitle;
            debugPanel.Show();
            var index = DebugManager.instance.FindPanelIndex(ProbeReferenceVolume.k_DebugPanelName);
            if (index != -1)
                DebugManager.instance.RequestEditorWindowPanelIndex(index);
        }

        // Need to have this only clear probes when we properly split lightmap and probe baking.
        static void ClearBakedData()
        {
            Lightmapping.ClearLightingDataAsset();
            Lightmapping.Clear();
        }

        public override void OnBakeButtonGUI()
        {
            void BakeButtonCallback(object data)
            {
                // Order of options defined by Styles.bakeOptionsText
                int option = (int)data;
                switch (option)
                {
                    case 0: AdaptiveProbeVolumes.BakeAsync(); break;
                    case 1: BakeAllReflectionProbes(); break;
                    case 2: ClearBakedData(); break;
                    default: Debug.Log("invalid option in BakeButtonCallback"); break;
                }
            }

            if (AdaptiveProbeVolumes.isRunning)
            {
                if (GUILayout.Button(Styles.cancelBake, Styles.buttonStyle))
                    AdaptiveProbeVolumes.Cancel();
                return;
            }

            if (EditorGUI.LargeSplitButtonWithDropdownList(Styles.generateLighting, Styles.bakeOptionsText, BakeButtonCallback))
                Lightmapping.BakeAsync();
        }
        #endregion

        #region Baking
        ReorderableList m_ScenesInSet;

        List<Scene> scenesToUnload = new();
        List<SceneData> scenesForBake = new();
        List<SceneData> scenesToEnable = new();

        void BakingGUI()
        {
            EditorGUI.BeginChangeCheck();
            m_SingleSceneMode = EditorGUILayout.Popup(Styles.bakingMode, m_SingleSceneMode ? 0 : 1, Styles.bakingModeOptions) == 0;
            if (EditorGUI.EndChangeCheck() && !m_SingleSceneMode)
            {
                if (activeSet != null) { EditorUtility.SetDirty(activeSet); activeSet.singleSceneMode = false; }
                SaveTempBakingSetIfNeeded();
            }

            if (m_SingleSceneMode)
            {
                SingleSceneUI();
                return;
            }

            EditorGUI.BeginChangeCheck();
            var newSet = ObjectFieldWithNew(Styles.currentBakingSet, activeSet, CreateBakingSet);
            if (EditorGUI.EndChangeCheck())
            {
                if (newSet != null) { EditorUtility.SetDirty(newSet); newSet.singleSceneMode = false; }
                activeSet = newSet;

                ProbeReferenceVolume.instance.Clear();
            }

            if (activeSet != null)
            {
                if (HasSelectedSceneToAdd())
                {
                    var newScene = EditorGUIUtility.GetObjectPickerObject() as SceneAsset;
                    if (newScene != null)
                    {
                        Event.current.Use();
                        TryAddSceneToSet(newScene);
                    }
                }

                activeSet.SanitizeScenes();
                DrawListWithIndent(m_ScenesInSet);

                using (new EditorGUI.IndentLevelScope())
                    ShowWarnings();
            }
        }

        void SingleSceneUI()
        {
            if (m_Initialized)
            {
                var activeScene = SceneManager.GetActiveScene();
                var activeSceneGUID = ProbeReferenceVolume.GetSceneGUID(activeScene);
                var activeSceneSet = ProbeVolumeBakingSet.GetBakingSetForScene(activeSceneGUID);
                if (activeSceneSet && activeSceneSet.sceneGUIDs.Count == 1)
                {
                    if (!activeSceneSet.singleSceneMode)
                    {
                        EditorUtility.SetDirty(activeSceneSet);
                        activeSceneSet.singleSceneMode = true;
                        activeSet = activeSceneSet;
                    }
                }
                else if (activeSceneSet && (activeSceneSet.sceneGUIDs.Any(s => s != activeSceneGUID) && activeSceneSet.sceneGUIDs.Count > 1))
                {
                    if (EditorUtility.DisplayDialog("Move Scene to new baking set", $"The scene '{activeScene.name}' is part of the Baking Set '{activeSceneSet.name}' with other scenes." +
                        " This is not compatible with the Single Scene mode.\nDo you want to move the scene to a new set?", "Yes", "Cancel"))
                    {
                        var tmpSet = activeSceneSet.Clone();
                        activeSceneSet.RemoveScene(activeSceneGUID);
                        UseTemporaryBakingSet(activeSceneGUID, tmpSet);
                    }
                    else
                        m_SingleSceneMode = false;
                }
                else if (activeSet == null || (activeSet.sceneGUIDs.Any(s => s != activeSceneGUID) && activeSet.sceneGUIDs.Count > 1))
                    UseTemporaryBakingSet(activeSceneGUID);
            }

            var firstInvalid = GetFirstProbeVolumeInNonActiveScene();
            if (firstInvalid != null)
            {
                EditorGUILayout.HelpBox($"In Single Scene mode, only the active scene will be baked. Scene '{firstInvalid.gameObject.scene.name}' contains Probe Volumes but will not be baked.\nUse Baking Sets to bake multiple scenes together.", MessageType.Warning);
                if (RightAlignedButton("Create a Baking Set"))
                    ConvertTempBakingSet();
            }
        }

        void ShowWarnings()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && ProbeVolumeBakingSet.GetBakingSetForScene(scene) != activeSet)
                    scenesToUnload.Add(scene);
            }

            bool hasWarnings = scenesForBake.Count + scenesToUnload.Count + scenesToEnable.Count > 0;
            if (hasWarnings && Foldout(Styles.warnings, Expandable.BakingWarnings, false))
            {
                if (scenesForBake.Count > 0)
                {
                    EditorGUILayout.HelpBox(Styles.msgLoadForBake, MessageType.Warning);
                    if (RightAlignedButton("Load Baking Set"))
                    {
                        foreach (var scene in scenesForBake)
                            EditorSceneManager.OpenScene(scene.GetPath(), OpenSceneMode.Additive);
                        if (scenesToUnload.All(s => !s.isDirty) || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            foreach (var scene in scenesToUnload)
                                EditorSceneManager.CloseScene(scene, true);
                        }
                    }
                }
                if (scenesToUnload.Count > 0)
                {
                    EditorGUILayout.HelpBox(Styles.msgUnloadOther, MessageType.Warning);
                    switch (RightAlignedButton("Add to Baking Set", "Unload These Scenes", disable2: scenesToUnload.Count == SceneManager.loadedSceneCount))
                    {
                        case 1:
                            for (int i = 0; i < SceneManager.sceneCount; i++)
                            {
                                var scene = SceneManager.GetSceneAt(i);
                                if (scene.isLoaded)
                                    TryAddSceneToSet(scene);
                            }
                            break;
                        case 2:
                            if (scenesToUnload.All(s => !s.isDirty) || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                foreach (var scene in scenesToUnload)
                                    EditorSceneManager.CloseScene(scene, string.IsNullOrEmpty(scene.path)); // Remove the scene from the hierarchy iff it has never been saved.
                            }
                            break;
                    }
                }
                if (scenesToEnable.Count > 0)
                {
                    EditorGUILayout.HelpBox(Styles.msgEnableAll, MessageType.Info);
                    if (RightAlignedButton("Enable All Scenes"))
                    {
                        foreach (var scene in scenesToEnable)
                            activeSet.SetSceneBaking(scene.guid, true);
                    }
                }
            }

            scenesForBake.Clear();
            scenesToUnload.Clear();
            scenesToEnable.Clear();
        }

        void UseTemporaryBakingSet(string sceneGUID, ProbeVolumeBakingSet set = null)
        {
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
                set.SetDefaults();

                ProbeReferenceVolume.instance.Clear();
            }

            EditorUtility.SetDirty(set);
            var sceneData = FindSceneData(sceneGUID);
            set.name = (sceneData.asset == null ? "Untitled" : sceneData.asset.name) + " Baking Set";
            set.singleSceneMode = true;
            set.AddScene(sceneGUID);
            activeSet = set;
            m_TempBakingSet = true;
        }

        void SaveTempBakingSetIfNeeded()
        {
            var scene = SceneManager.GetActiveScene();
            if (!m_TempBakingSet || scene == null) return;
            string path = string.IsNullOrEmpty(scene.path) ?
                ProbeVolumeBakingSet.GetDirectory("Assets/", "Untitled") :
                ProbeVolumeBakingSet.GetDirectory(scene.path, scene.name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, activeSet.name + ".asset");
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(activeSet, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ProbeVolumeBakingSet.SyncBakingSets();
            m_TempBakingSet = false;
        }

        void ConvertTempBakingSet()
        {
            m_SingleSceneMode = activeSet.singleSceneMode = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                TryAddSceneToSet(SceneManager.GetSceneAt(i));
            SaveTempBakingSetIfNeeded();
        }

        ProbeVolumeBakingSet CreateBakingSet()
        {
            var scene = SceneManager.GetActiveScene();
            string path = string.IsNullOrEmpty(scene.path) ?
                ProbeVolumeBakingSet.GetDirectory("Assets/", "Untitled") :
                ProbeVolumeBakingSet.GetDirectory(scene.path, scene.name);

            var newSet = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
            newSet.name = "New Baking Set";
            newSet.singleSceneMode = false;
            newSet.SetDefaults();
            ProjectWindowUtil.CreateAsset(newSet, System.IO.Path.Combine(path, "New Baking Set.asset").Replace('\\', '/'));
            return newSet;
        }

        void CreateProbeVolume()
        {
            var probeVolume = CoreEditorUtils.CreateGameObject(null, "Adaptive Probe Volume");
            var pv = probeVolume.AddComponent<ProbeVolume>();
            pv.mode = ProbeVolume.Mode.Scene;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        bool HasSelectedSceneToAdd()
        {
            var evt = Event.current;
            return evt.type == EventType.ExecuteCommand && evt.commandName == "ObjectSelectorSelectionDone" && GUIUtility.keyboardControl == k_AddSceneID;
        }

        bool TryMoveSceneToActiveSet(string sceneName, string sceneGUID, ProbeVolumeBakingSet oldSet, int index = -1)
        {
            if (!EditorUtility.DisplayDialog("Move Scene to baking set", $"The scene '{sceneName}' was already added in the baking set '{oldSet.name}'. Do you want to move it to the current set?", "Yes", "Cancel"))
                return false;

            Undo.RegisterCompleteObjectUndo(new Object[] { activeSet, oldSet }, "Moved scene to baking set");
            activeSet.MoveSceneToBakingSet(sceneGUID, index);

            return true;
        }

        void TrySetSceneInSet(SceneData scene, int index)
        {
            var sceneSet = ProbeVolumeBakingSet.GetBakingSetForScene(scene.guid);
            if (scene.guid == null || sceneSet == activeSet)
                return;
            if (sceneSet != null)
            {
                if (!TryMoveSceneToActiveSet(scene.asset.name, scene.guid, sceneSet, index))
                    return;
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(new Object[] { activeSet }, "Updated scene in baking set");
                activeSet.SetScene(scene.guid, index);
            }
        }

        void TryAddSceneToSet(SceneAsset scene) { TryAddSceneToSet(scene.name, FindSceneData(scene).guid); }
        void TryAddSceneToSet(Scene scene) { TryAddSceneToSet(scene.name, scene.GetGUID()); }

        void TryAddSceneToSet(string sceneName, string sceneGUID)
        {
            // Don't allow the same scene in two different sets
            var sceneSet = ProbeVolumeBakingSet.GetBakingSetForScene(sceneGUID);
            if (sceneSet == activeSet)
                return;
            if (sceneSet != null)
            {
                if (!TryMoveSceneToActiveSet(sceneName, sceneGUID, sceneSet))
                    return;
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(new Object[] { activeSet }, "Added scene in baking set");
                activeSet.AddScene(sceneGUID);
            }
        }

        void InitializeSceneList()
        {
            m_ScenesInSet = new ReorderableList((System.Collections.IList)activeSet.sceneGUIDs, typeof(string), true, true, true, true)
            {
                multiSelect = true,
                elementHeight = EditorGUIUtility.singleLineHeight + 2,

                onAddCallback = (list) =>
                {
                    GUIUtility.keyboardControl = k_AddSceneID;
                    EditorGUIUtility.ShowObjectPicker<SceneAsset>(null, false, null, k_AddSceneID);
                },

                onRemoveCallback = (list) =>
                {
                    var guid = (string)list.list[list.index];
                    activeSet.RemoveScene(guid);
                    Undo.RegisterCompleteObjectUndo(new Object[] { activeSet }, "Deleted scene in baking set");
                    EditorUtility.SetDirty(activeSet);
                },

                drawHeaderCallback = (rect) =>
                {
                    SplitRectInThree(rect, out var sceneRect, out var statusRect, out var bakeRect);
                    EditorGUI.LabelField(sceneRect, Styles.scenesInSet);
                    EditorGUI.LabelField(statusRect, Styles.status);
                    EditorGUI.LabelField(bakeRect, Styles.bake);

                    var contextMenuRect = new Rect(bakeRect.xMax - 13f, bakeRect.y + 1f, 16, 16);
                    if (GUI.Button(contextMenuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(Styles.addLoadedScenes, false, () => {
                            for (int i = 0; i < SceneManager.sceneCount; i++)
                            {
                                if (SceneManager.GetSceneAt(i).isLoaded)
                                    TryAddSceneToSet(SceneManager.GetSceneAt(i));
                            }
                        });
                        menu.AddSeparator(string.Empty);
                        menu.AddItem(Styles.toggleBakeAll, false, () => activeSet.SetAllSceneBaking(true));
                        menu.AddItem(Styles.toggleBakeNone, false, () => activeSet.SetAllSceneBaking(false));

                        menu.DropDown(contextMenuRect);
                    }
                },

                drawElementCallback = (rect, index, active, focused) =>
                {
                    rect.yMin++;
                    rect.yMax--;
                    SplitRectInThree(rect, out var sceneRect, out var statusRect, out var bakeRect);

                    var scene = index < activeSet.sceneGUIDs.Count ? FindSceneData(activeSet.sceneGUIDs[index]) : default;
                    Scene scene2 = SceneManager.GetSceneByPath(scene.GetPath());
                    bool isLoaded = scene2.isLoaded;
                    bool isActive = SceneManager.GetActiveScene() == scene2;

                    using (new EditorGUI.DisabledScope(!isLoaded))
                    {
                        EditorGUI.BeginChangeCheck();
                        var newScene = EditorGUI.ObjectField(sceneRect, scene.asset, typeof(SceneAsset), false) as SceneAsset;
                        if (EditorGUI.EndChangeCheck() && newScene != null)
                            TrySetSceneInSet(FindSceneData(newScene), index);

                        string label = isLoaded ? (isActive ? "Active" : "Loaded") : "Unloaded";
                        EditorGUI.LabelField(statusRect, label, isActive ? EditorStyles.boldLabel : EditorStyles.label);

                        bakeRect.xMin += 5f;
                        bakeRect.width = 21;

                        bool bake = true;
                        if (scene.guid != null)
                        {
                            var bakeData = activeSet.GetSceneBakeData(scene.guid);
                            if (bakeData.hasProbeVolume)
                            {
                                EditorGUI.BeginChangeCheck();
                                EditorGUI.LabelField(bakeRect, Styles.bakeBox); // Show a tooltip on the checkbox
                                bake = EditorGUI.Toggle(bakeRect, bakeData.bakeScene && isLoaded);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RegisterCompleteObjectUndo(activeSet, "Set scene bake status");
                                    EditorUtility.SetDirty(activeSet);
                                    bakeData.bakeScene = bake;
                                }
                            }
                        }

                        GUIContent content = null;
                        if (isLoaded && !bake)
                        {
                            content = Styles.iconEnableAll;
                            scenesToEnable.Add(scene);
                        }
                        if (!isLoaded && scene.guid != null)
                        {
                            content = Styles.iconLoadForBake;
                            scenesForBake.Add(scene);
                        }

                        if (content != null)
                        {
                            bakeRect.x += 20f;
                            bakeRect.width = bakeRect.height;
                            GUI.Label(bakeRect, content);
                        }
                    }
                }
            };
        }
        #endregion

        #region Summary
        public override void OnSummaryGUI()
        {
            Func<long, string> FormatToMB = (bytes) => (bytes / (float)(1000 * 1000)).ToString("F1") + " MB";

            if (!m_Initialized || activeSet == null)
            {
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                GUILayout.Label(Styles.totalCostStat, EditorStyles.wordWrappedMiniLabel);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Label(FormatToMB(0), EditorStyles.wordWrappedMiniLabel);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                return;
            }

            long scenarioCost = activeSet.GetDiskSizeOfScenarioData(ProbeReferenceVolume.instance.lightingScenario);

            long sharedCost = activeSet.GetDiskSizeOfSharedData();
            foreach (var scenario in activeSet.m_LightingScenarios)
                sharedCost += activeSet.GetDiskSizeOfScenarioData(scenario);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(Styles.scenarioCostStat, EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label(Styles.totalCostStat, EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label(FormatToMB(scenarioCost), EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label(FormatToMB(sharedCost), EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
        #endregion

        #region Scene Management
        struct SceneData
        {
            public SceneAsset asset;
            public string guid;

            public string GetPath()
            {
                return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        List<SceneData> m_ScenesInProject = new List<SceneData>();

        internal void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene == SceneManager.GetActiveScene())
            {
                var prv = ProbeReferenceVolume.instance;
                // Find the set in which the new active scene belongs
                var set = ProbeVolumeBakingSet.GetBakingSetForScene(scene);

                activeSet = set;

                if (set == null)
                {
                    m_SingleSceneMode = true;
                }
                else
                {
                    // If we load a new scene that doesn't have the current scenario, change it
                    if (!set.m_LightingScenarios.Contains(prv.lightingScenario))
                        prv.SetActiveScenario(set.m_LightingScenarios[0], false);
                }
            }
        }

        bool NoSceneHasProbeVolume() => ProbeVolume.instances.Count == 0;
        bool ActiveSceneHasProbeVolume() => ProbeVolume.instances.Any(d => d.gameObject.scene == SceneManager.GetActiveScene());
        ProbeVolume GetFirstProbeVolumeInNonActiveScene() => ProbeVolume.instances.FirstOrDefault(d => d.gameObject.scene != SceneManager.GetActiveScene());

        void RefreshSceneAssets()
        {
            var sceneAssets = AssetDatabase.FindAssets("t:Scene");

            m_ScenesInProject = sceneAssets.Select(s =>
            {
                var path = AssetDatabase.GUIDToAssetPath(s);
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                return new SceneData
                {
                    asset = asset,
                    guid = s,
                };
            }).ToList();
        }

        SceneData FindSceneData(SceneAsset asset)
        {
            var data = m_ScenesInProject.FirstOrDefault(s => s.asset == asset);

            if (data.asset == null)
            {
                RefreshSceneAssets();
                data = m_ScenesInProject.FirstOrDefault(s => s.asset == asset);
            }

            return data;
        }

        SceneData FindSceneData(string guid)
        {
            var data = m_ScenesInProject.FirstOrDefault(s => s.guid == guid);

            if (data.asset == null)
            {
                RefreshSceneAssets();
                data = m_ScenesInProject.FirstOrDefault(s => s.guid == guid);
            }

            return data;
        }

        internal static ProbeVolumeBakingSet GetSceneBakingSetForUI(Scene scene)
        {
            // If the set is available, return it
            var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(scene);
            if (bakingSet != null)
                return bakingSet;

            // Otherwise, a baking set might be created in the UI but not registered yet in the system
            if (instance == null || instance.activeSet == null)
                return null;
            if (!singleSceneMode || !instance.activeSet.singleSceneMode)
                return null;
            if (!instance.activeSet.sceneGUIDs.Contains(scene.GetGUID()))
                return null;

            return instance.activeSet;
        }
        #endregion

        #region Async Bake
        internal static void BakeAPVButton()
        {
            if (AdaptiveProbeVolumes.isRunning)
            {
                if (GUILayout.Button(Styles.cancelBake))
                    AdaptiveProbeVolumes.Cancel();
            }
            else
            {
                if (GUILayout.Button(Styles.generateAPV))
                {
                    EditorApplication.delayCall += () => AdaptiveProbeVolumes.BakeAsync();
                }
            }
        }
        #endregion

        #region UI Helpers
        internal static void OpenBakingSet(ProbeVolumeBakingSet bakingSet)
        {
            var lightingWindow = Type.GetType("UnityEditor.LightingWindow,UnityEditor");
            EditorWindow.GetWindow(lightingWindow, utility: false, title: null, focus: true);
            if (instance == null)
                return;

            instance.FocusTab();
            instance.activeSet = bakingSet;
        }

        static MethodInfo k_FoldoutTitlebar;
        internal static bool Foldout(GUIContent label, Expandable expandable, bool title, Action<Rect> contextMenuCallback = null)
        {
            if (k_FoldoutTitlebar == null)
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Static;
                Type[] args = new Type[] { typeof(Rect), typeof(GUIContent), typeof(bool), typeof(bool) };
                k_FoldoutTitlebar = typeof(EditorGUI).GetMethod("FoldoutTitlebar", flags, null, args, null);
            }

            var labelRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.inspectorTitle, GUILayout.ExpandWidth(true));
            var contextMenuRect = new Rect(labelRect.xMax - 17f, labelRect.y + 3f, 16, 16);

            if (contextMenuCallback != null)
            {
                if (GUI.Button(contextMenuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle))
                    contextMenuCallback(contextMenuRect);
            }

            if (k_Foldouts == null)
                k_Foldouts = new(k_ExpandableDefault, "APV");

            bool foldout = title ?
                (bool)k_FoldoutTitlebar.Invoke(null, new object[] { labelRect, label, k_Foldouts[expandable], true }) :
                EditorGUI.Foldout(labelRect, k_Foldouts[expandable], label, true);
            k_Foldouts.SetExpandedAreas(expandable, foldout);

            // Shameful hack warning: we have to display the button before the foldout to take precedence on click
            // Therefore we have to draw it again afterwards so that we can see it on top.
            if (contextMenuCallback != null)
                GUI.Button(contextMenuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle);

            return foldout;
        }

        static MethodInfo k_GetLightmapSettings;
        internal static Object GetLightmapSettings()
        {
            if (k_GetLightmapSettings == null)
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Static;
                k_GetLightmapSettings = typeof(LightmapEditorSettings).GetMethod("GetLightmapSettings", flags);
            }

            return (Object)k_GetLightmapSettings.Invoke(null, null);
        }

        static MethodInfo k_GetLightingSettingsOrDefaultsFallback = typeof(Lightmapping).GetMethod("GetLightingSettingsOrDefaultsFallback", BindingFlags.Static | BindingFlags.NonPublic);
        internal static LightingSettings GetLightingSettings()
        {
            return k_GetLightingSettingsOrDefaultsFallback.Invoke(null, null) as LightingSettings;
        }

        static MethodInfo k_Lightmapping_BakeAllReflectionProbesSnapshots = typeof(Lightmapping).GetMethod("BakeAllReflectionProbesSnapshots", BindingFlags.Static | BindingFlags.NonPublic);
        static bool BakeAllReflectionProbes()
        {
            return (bool)k_Lightmapping_BakeAllReflectionProbesSnapshots.Invoke(null, null);
        }

        internal bool PrepareAPVBake(ProbeReferenceVolume prv)
        {
            if (!prv.isInitialized || !prv.enabledBySRP)
                return false;

            // Always baking with a fresh activeSet
            activeSet = null;

            // In case UI was never opened we have to setup some stuff
            FindActiveSet();

            if (activeSet == null)
            {
                // APV was never setup by the user, try to do it for him by creating a default baking set
                var activeScene = SceneManager.GetActiveScene();
                var activeSceneGUID = ProbeReferenceVolume.GetSceneGUID(activeScene);
                UseTemporaryBakingSet(activeSceneGUID);
            }

            bool createPV = m_SingleSceneMode ? !ActiveSceneHasProbeVolume() : NoSceneHasProbeVolume();
            if (createPV)
            {
                if(!activeSet.DialogNoProbeVolumeInSetShown())
                {
                    if (!Application.isBatchMode)
                        if (EditorUtility.DisplayDialog("No Adaptive Probe Volume in Scene",
                                "Adaptive Probe Volumes are enabled for this Project, but none exist in the Scene.\n\n" +
                                "Do you wish to add an Adaptive Probe Volume to the Active Scene?", "Yes", "No"))
                            CreateProbeVolume();
                    activeSet.SetDialogNoProbeVolumeInSetShown(true);
                }
            }
            if (m_SingleSceneMode)
            {
                if (GetFirstProbeVolumeInNonActiveScene() != null)
                {
                    const string warning = "You are using the Single Scene Baking Mode and have more than one Scene loaded. It is not possible to generate lighting.";
                    if (Application.isBatchMode)
                    {
                        Debug.LogWarning(warning + " Consider creating a Baking Set.");
                        return false;
                    }
                    int res = EditorUtility.DisplayDialogComplex("Create Baking Set?", warning + "\n\n" +
                        "Do you want to create a Baking Set instead?", "Yes", "Cancel", "Bake anyway");
                    if (res == 0)
                        ConvertTempBakingSet();
                    if (res == 1)
                        return false;
                }
            }

            SaveTempBakingSetIfNeeded();

            if (!FindActiveSet())
                return false;

            // Exclude scenes unchecked from the UI and scenes from other baking sets
            AdaptiveProbeVolumes.partialBakeSceneList = new();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var guid = ProbeReferenceVolume.GetSceneGUID(scene);
                var sceneBakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(guid);
                if (!scene.isLoaded || sceneBakingSet != activeSet) continue;
                var sceneBakeData = sceneBakingSet.GetSceneBakeData(guid);
                if (sceneBakeData.hasProbeVolume && !sceneBakeData.bakeScene) continue;

                AdaptiveProbeVolumes.partialBakeSceneList.Add(guid);
            }

            if (AdaptiveProbeVolumes.partialBakeSceneList.Count == activeSet.sceneGUIDs.Count)
                AdaptiveProbeVolumes.partialBakeSceneList = null;

            if (prv.supportLightingScenarios && !activeSet.m_LightingScenarios.Contains(activeSet.lightingScenario) && activeSet.m_LightingScenarios.Count > 0)
                activeSet.SetActiveScenario(activeSet.m_LightingScenarios[0], false);

            // Layout has changed and is incompatible.
            if (activeSet.HasValidSharedData() && !activeSet.freezePlacement && !activeSet.CheckCompatibleCellLayout())
            {
                if (AdaptiveProbeVolumes.partialBakeSceneList != null)
                {
                    const string warning = "You are partially baking the set with an incompatible cell layout.";
                    if (Application.isBatchMode)
                    {
                        Debug.LogWarning(warning);
                        return false;
                    }
                    if (EditorUtility.DisplayDialog("Incompatible Layout", warning + " Proceeding will invalidate all previously bake data.\n\n" + "Do you wish to continue?", "Yes", "No"))
                        ClearBakedData();
                    else
                        return false;
                }
                else if (prv.supportLightingScenarios && activeSet.scenarios.Count != (activeSet.scenarios.ContainsKey(activeSet.lightingScenario) ? 1 : 0))
                {
                    const string warning = "You are baking scenarios with incompatible cell layouts.";
                    if (Application.isBatchMode)
                    {
                        Debug.LogWarning(warning);
                        return false;
                    }
                    if (EditorUtility.DisplayDialog("Incompatible Layout", warning + " Proceeding will invalidate all previously bake data.\n\n" + "Do you wish to continue?", "Yes", "No"))
                        ClearBakedData();
                    else
                        return false;
                }
            }

            return true;
        }

        static T ObjectFieldWithNew<T>(GUIContent label, T obj, Func<T> onClick) where T : Object
        {
            const int k_NewFieldWidth = 70;

            var rect = EditorGUILayout.GetControlRect();
            rect.xMax -= k_NewFieldWidth + 2;

            var newFieldRect = rect;
            newFieldRect.x = rect.xMax + 2;
            newFieldRect.width = k_NewFieldWidth;
            if (GUI.Button(newFieldRect, "New"))
                return onClick();

            return EditorGUI.ObjectField(rect, label, obj, typeof(T), false) as T;
        }

        internal static void SplitRectInThree(Rect rect, out Rect left, out Rect middle, out Rect right, float middleWith = Styles.statusLabelWidth, float rightWidth = 50)
        {
            right = rect;
            right.xMin = rect.xMax - rightWidth;
            middle = rect;
            middle.xMin = right.xMin - middleWith;
            middle.xMax = right.xMin - 2;
            left = rect;
            left.xMax = middle.xMin - 10;
        }

        static bool RightAlignedButton(string text, float width = Styles.lightingButtonWidth)
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width));
            rect.y -= 8;
            bool click = GUI.Button(rect, text);
            GUILayout.EndHorizontal();
            return click;
        }

        static int RightAlignedButton(string text1, string text2, bool disable1 = false, bool disable2 = false, float width = Styles.lightingButtonWidth)
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var rect1 = EditorGUILayout.GetControlRect(GUILayout.Width(width));
            var rect2 = EditorGUILayout.GetControlRect(GUILayout.Width(width));
            rect1.y -= 8;
            rect2.y -= 8;
            bool click1, click2;
            using (new EditorGUI.DisabledScope(disable1))
                click1 = GUI.Button(rect1, text1);
            using (new EditorGUI.DisabledScope(disable2))
                click2 = GUI.Button(rect2, text2);
            GUILayout.EndHorizontal();
            return click1 ? 1 : click2 ? 2 : 0;
        }

        internal static void DrawListWithIndent(ReorderableList list)
        {
            EditorGUILayout.Space();

            int level = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GUILayout.BeginHorizontal();
            EditorGUILayout.Space(15 * level, false);
            {
                list.DoLayoutList();
            }
            EditorGUILayout.Space(3, false);
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel = level;
        }

        private static Color k_DarkThemeColor = new Color32(153, 153, 153, 255);
        private static Color k_LiteThemeColor = new Color32(97, 97, 97, 255);
        static Color GetMarkerColor() => EditorGUIUtility.isProSkin ? k_DarkThemeColor : k_LiteThemeColor;

        internal static void DrawSimplificationLevelsMarkers(Rect rect, float minDistanceBetweenProbes, int lowestSimplification, int highestSimplification, int hideStart, int hideEnd)
        {
            var markerRect = new Rect(rect) { width = 2, height = 2 };
            markerRect.y += (EditorGUIUtility.singleLineHeight / 2f) - 1;
            float indent = 15 * EditorGUI.indentLevel;
            GUILayout.Space(18);

            for (int i = lowestSimplification; i <= highestSimplification; i++)
            {
                float position = (float)(i - lowestSimplification) / (highestSimplification - lowestSimplification);

                float knobSize = (i == lowestSimplification || i == highestSimplification) ? 0 : 10;
                float start = rect.x + knobSize / 2f;
                float range = rect.width - knobSize;
                markerRect.x = start + range * position - 0.5f * markerRect.width;

                float min = rect.x;
                float max = (rect.x + rect.width) - markerRect.width;
                markerRect.x = Mathf.Clamp(markerRect.x, min, max);

                float maxTextWidth = 200;
                string text = (minDistanceBetweenProbes * ProbeReferenceVolume.CellSize(i)) + "m";
                float textX = markerRect.x + 1 - (maxTextWidth + indent) * 0.5f;
                Styles.labelFont.alignment = TextAnchor.UpperCenter;
                if (i == highestSimplification)
                {
                    textX = rect.xMax - maxTextWidth;
                    Styles.labelFont.alignment = TextAnchor.UpperRight;
                }
                else if (i == lowestSimplification)
                {
                    textX = markerRect.x - indent;
                    Styles.labelFont.alignment = TextAnchor.UpperLeft;
                }

                var label = new Rect(rect) { x = textX, width = maxTextWidth, y = rect.y + 18 };
                EditorGUI.LabelField(label, text, Styles.labelFont);
                if (i < hideStart || i > hideEnd)
                    EditorGUI.DrawRect(markerRect, GetMarkerColor());
            }
        }

        #endregion

        #region Probe Volume Scene Overlay

        [Overlay(typeof(SceneView), k_OverlayID)]
        [Icon("Packages/com.unity.render-pipelines.core/Editor/Resources/Gizmos/ProbeVolume.png")]
        internal class ProbeVolumeOverlay : Overlay, ITransientOverlay
        {
            const string k_OverlayID = "APV Overlay";

            VisualElement m_Disabled = null;
            Toggle[] m_LayerToggles = null;
            Label[] m_BrickLabels = null;
            GroupBox layerMasksGroupBox = null;
            GroupBox probeDistanceGroupBox = null;
            GroupBox probeSamplingGroupBox = null;
            TextElement vertexSamplingWarning = null;

            int maxSubdiv;
            float minDistance;

            public bool visible => IsVisible();

            (int maxSubdiv, float minDistance) GetSettings()
            {
                if (ProbeReferenceVolume.instance.probeVolumeDebug.realtimeSubdivision)
                {
                    var probeVolume = GameObject.FindFirstObjectByType<ProbeVolume>();
                    if (probeVolume != null && probeVolume.isActiveAndEnabled)
                    {
                        var profile = ProbeVolumeBakingSet.GetBakingSetForScene(probeVolume.gameObject.scene);
                        if (profile != null)
                            return (profile.maxSubdivision, profile.minDistanceBetweenProbes);
                    }
                }

                return (ProbeReferenceVolume.instance.GetMaxSubdivision(), ProbeReferenceVolume.instance.MinDistanceBetweenProbes());
            }

            bool IsVisible()
            {
                // Include some state tracking here because it's the only function called at each repaint
                var debug = ProbeReferenceVolume.instance.probeVolumeDebug;
                var bakingSet = ProbeReferenceVolume.instance.currentBakingSet;

                bool debugLayers = debug.drawProbes && debug.probeShading == DebugProbeShadingMode.RenderingLayerMasks && bakingSet != null;
                if (!debug.drawBricks && !debug.drawProbeSamplingDebug && !debugLayers)
                    return false;

                EnableGroupBox(layerMasksGroupBox, debugLayers);
                EnableGroupBox(probeDistanceGroupBox, debug.drawBricks);
                EnableGroupBox(probeSamplingGroupBox, debug.drawProbeSamplingDebug);
                EnableTextArea(vertexSamplingWarning, ProbeReferenceVolume.instance.vertexSampling);

                if (debugLayers && m_LayerToggles != null)
                {
                    if (bakingSet.bakedMaskCount != 1)
                    {
                        debug.visibleLayers = 0;
                        m_Disabled.style.display = DisplayStyle.None;
                        for (int i = 0; i < APVDefinitions.probeMaxRegionCount; i++)
                        {
                            int visibility = m_LayerToggles[i].value ? 1 << i : 0;
                            if (i >= bakingSet.renderingLayerMasks.Length)
                            {
                                debug.visibleLayers &= (byte)~visibility;
                                m_LayerToggles[i].parent.style.display = DisplayStyle.None;
                            }
                            else
                            {
                                debug.visibleLayers |= (byte)visibility;
                                m_LayerToggles[i].parent.style.display = DisplayStyle.Flex;
                                m_LayerToggles[i].label = bakingSet.renderingLayerMasks[i].name;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < APVDefinitions.probeMaxRegionCount; i++)
                            m_LayerToggles[i].parent.style.display = DisplayStyle.None;
                        m_Disabled.style.display = DisplayStyle.Flex;
                    }
                }
                if (debug.drawBricks && m_BrickLabels != null)
                {
                    (int max, float min) = GetSettings();
                    if (maxSubdiv != max)
                    {
                        maxSubdiv = max;
                        for (int i = 0; i < m_BrickLabels.Length; i++)
                            m_BrickLabels[i].parent.EnableInClassList("unity-pbr-validation-hidden", i >= maxSubdiv);
                    }
                    if (minDistance != min)
                    {
                        minDistance = min;
                        for (int i = 0; i < m_BrickLabels.Length; i++)
                            m_BrickLabels[i].text = (minDistance * ProbeReferenceVolume.CellSize(i)) + " meters";
                    }
                }

                return true;
            }

            void EnableGroupBox(GroupBox groupBox, bool b)
            {
                if (groupBox == null)
                    return;

                groupBox.style.display = b ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void EnableTextArea(TextElement text, bool b)
            {
                if (text == null)
                    return;

                text.style.display = b ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public override void OnCreated()
            {
                if (containerWindow is not SceneView)
                    throw new Exception("APV Overlay is only valid in the Scene View");
            }

            VisualElement CreateColorSwatch(Color color)
            {
                var swatchContainer = new VisualElement();
                swatchContainer.AddToClassList("unity-base-field__label");
                swatchContainer.AddToClassList("unity-pbr-validation-color-swatch");

                var colorContent = new VisualElement() { name = "color-content" };
                colorContent.style.backgroundColor = new StyleColor(color);
                swatchContainer.Add(colorContent);

                return swatchContainer;
            }

            public override VisualElement CreatePanelContent()
            {
                displayName = "Adaptive Probe Volumes";
                maxSubdiv = 0;
                minDistance = -1;

                var root = new VisualElement();

                // Layer mask
                layerMasksGroupBox = new GroupBox();
                layerMasksGroupBox.text = "Rendering Layer Masks";

                m_LayerToggles = new Toggle[APVDefinitions.probeMaxRegionCount];
                for (int i = 0; i < APVDefinitions.probeMaxRegionCount; i++)
                {
                    var row = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                    layerMasksGroupBox.Add(row);

                    row.Add(CreateColorSwatch(APVDefinitions.layerMaskColors[i]));

                    m_LayerToggles[i] = new Toggle("Mask " + i) { name = "color-label" };
                    m_LayerToggles[i].AddToClassList("unity-base-field__label");
                    m_LayerToggles[i].value = true;
                    row.Add(m_LayerToggles[i]);
                }
                {
                    m_Disabled = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                    layerMasksGroupBox.Add(m_Disabled);

                    m_Disabled.Add(CreateColorSwatch(APVDefinitions.debugEmptyColor));

                    var label = new Label("Disabled") { name = "color-label" };
                    label.AddToClassList("unity-base-field__label");
                    m_Disabled.Add(label);
                    m_Disabled.style.display = DisplayStyle.None;
                }

                // Distance Between Probes
                probeDistanceGroupBox = new GroupBox();
                probeDistanceGroupBox.text = "Distance Between probes";

                m_BrickLabels = new Label[6];
                for (int i = 0; i < m_BrickLabels.Length; i++)
                {
                    var row = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                    probeDistanceGroupBox.Add(row);

                    row.Add(CreateColorSwatch(ProbeReferenceVolume.instance.subdivisionDebugColors[i]));

                    m_BrickLabels[i] = new Label() { name = "color-label" };
                    m_BrickLabels[i].AddToClassList("unity-base-field__label");
                    row.Add(m_BrickLabels[i]);
                }

                // Probe Sampling Select Pixel
                probeSamplingGroupBox = new GroupBox();
                probeSamplingGroupBox.text = "Probe Sampling";

                var probeSampling_row = new VisualElement() { style = { flexDirection = FlexDirection.Column } };
                probeSamplingGroupBox.Add(probeSampling_row);

                var selectPixelButton = new Button();
                selectPixelButton.clickable.activators.Clear();
                selectPixelButton.text = "Select Pixel";
                selectPixelButton.tooltip = "Use this button or Ctrl+Click on the viewport to select which pixel to debug for APV sampling";

                Color buttonDefaultColor = new Color(0.250f, 0.250f, 0.250f);
                Color buttonHoverColor = new Color(0.404f, 0.404f, 0.404f);
                Color buttonPressedColor = new Color(0.133f, 0.133f, 0.133f);
                selectPixelButton.style.backgroundColor = buttonDefaultColor;
                selectPixelButton.RegisterCallback<MouseOverEvent>((type) => { selectPixelButton.style.backgroundColor = buttonHoverColor; });
                selectPixelButton.RegisterCallback<MouseOutEvent>((type) => { selectPixelButton.style.backgroundColor = buttonDefaultColor; });
                selectPixelButton.RegisterCallback<MouseDownEvent>((type) => { selectPixelButton.style.backgroundColor = buttonPressedColor; });
                selectPixelButton.RegisterCallback<MouseUpEvent>((type) => { selectPixelButton.style.backgroundColor = buttonDefaultColor; });

                selectPixelButton.RegisterCallback<MouseDownEvent>(e => ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Always);

                probeSampling_row.Add(selectPixelButton);

                vertexSamplingWarning = new TextElement();
                vertexSamplingWarning.text = "Warning: Probe Sampling is currently set to\n" +
                                             "per-vertex. This debug mode shows per-pixel\n" +
                                             "information.";
                vertexSamplingWarning.style.display = DisplayStyle.None;

                probeSampling_row.Add(vertexSamplingWarning);

                layerMasksGroupBox.style.display = DisplayStyle.None;
                probeDistanceGroupBox.style.display = DisplayStyle.None;
                probeSamplingGroupBox.style.display = DisplayStyle.None;

                root.Add(layerMasksGroupBox);
                root.Add(probeDistanceGroupBox);
                root.Add(probeSamplingGroupBox);

                return root;
            }
        }
        #endregion
    }
}
