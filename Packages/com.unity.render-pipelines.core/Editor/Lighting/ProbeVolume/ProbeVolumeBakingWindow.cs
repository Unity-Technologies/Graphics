using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    class ProbeVolumeBakingWindow : EditorWindow
    {
        const int k_LeftPanelSize = 250; // TODO: resizable panel
        const int k_RightPanelLabelWidth = 200;
        const int k_ProbeVolumeIconSize = 30;
        const int k_TitleTextHeight = 30;
        const string k_SelectedBakingSetKey = "Selected Baking Set";
        const string k_RenameFocusKey = "Baking Set Rename Field";

        struct SceneData
        {
            public SceneAsset asset;
            public string guid;

            public string GetPath()
            {
                return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        static class Styles
        {
            public static readonly Texture sceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon").image;
            public static readonly Texture probeVolumeIcon = EditorGUIUtility.IconContent("LightProbeGroup Icon").image; // For now it's not the correct icon, we need to request it
            public static readonly Texture debugIcon = EditorGUIUtility.IconContent("DebuggerEnabled").image;

            public static readonly GUIContent sceneLightingSettings = new GUIContent("Light Settings In Use", EditorGUIUtility.IconContent("LightingSettings Icon").image);
            public static readonly GUIContent activeScenarioLabel = new GUIContent("Active Scenario", EditorGUIUtility.IconContent("FilterSelectedOnly").image);
            public static readonly GUIContent sceneNotFound = new GUIContent("Scene Not Found!", Styles.sceneIcon);
            public static readonly GUIContent bakingSetsTitle = new GUIContent("Baking Sets");
            public static readonly GUIContent debugButton = new GUIContent(Styles.debugIcon);
            public static readonly GUIContent stats = new GUIContent("Stats");
            public static readonly GUIContent scenarioCostStat = new GUIContent("Active Scenario Size On Disk", "Size on disk used by the baked data of the currently selected lighting scenario.");
            public static readonly GUIContent totalCostStat = new GUIContent("Baking Set Total Size On Disk", "Size on disk used by baked data of all lighting scenarios of the set.");

            public static readonly GUIContent invalidLabel = new GUIContent("Out of Date");
            public static readonly GUIContent emptyLabel = new GUIContent("Not Baked");
            public static readonly GUIContent notLoadedLabel = new GUIContent("Set is not Loaded");
            public static readonly GUIContent[] scenariosStatusLabel = new GUIContent[] { GUIContent.none, notLoadedLabel, invalidLabel, emptyLabel };

            public static readonly GUIStyle labelRed = "CN StatusError";

            public static readonly GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
        }

        enum ScenariosStatus
        {
            Valid,
            NotLoaded,
            OutOfDate,
            NotBaked
        }

        SearchField m_SearchField;
        string m_SearchString = "";
        MethodInfo m_DrawHorizontalSplitter;
        [NonSerialized] ReorderableList m_BakingSets = null;
        [NonSerialized] ReorderableList m_Scenarios = null;
        ScenariosStatus[] scenariosStatuses = new ScenariosStatus[0];
        Vector2 m_LeftScrollPosition;
        Vector2 m_RightScrollPosition;
        ReorderableList m_ScenesInSet;
        GUIStyle m_SubtitleStyle;
        Editor m_ProbeVolumeProfileEditor;
        SerializedObject m_SerializedObject;
        SerializedProperty m_ProbeSceneData;
        bool m_RenameSelectedBakingSet;
        bool m_RenameSelectedScenario;
        [System.NonSerialized]
        bool m_Initialized;
        float infoLabelX;

        bool hasPendingScenarioUpdate = false;

        List<SceneData> m_ScenesInProject = new List<SceneData>();

        internal enum Expandable
        {
            RendererFilterSettings = 1 << 0,
            Dilation = 1 << 1,
            VirtualOffset = 1 << 2,
        };

        static readonly Expandable k_ExpandableDefault = 0;
        static ExpandedState<Expandable, ProbeVolumeBakingProcessSettings> k_Foldouts;

        internal static bool Foldout(GUIContent label, Expandable expandable, GUIStyle style = null)
        {
            k_Foldouts.SetExpandedAreas(expandable, EditorGUILayout.Foldout(k_Foldouts[expandable], label, true, style ?? Styles.boldFoldout));
            return k_Foldouts[expandable];
        }

        ProbeVolumeSceneData sceneData => ProbeReferenceVolume.instance.sceneData;

        [MenuItem("Window/Rendering/Probe Volume Settings (Experimental)", priority = 2)]
        static void OpenWindow()
        {
            // Get existing open window or if none, make a new one:
            ProbeVolumeBakingWindow window = (ProbeVolumeBakingWindow)EditorWindow.GetWindow(typeof(ProbeVolumeBakingWindow));
            window.Show();
        }

        void OnEnable()
        {
            k_Foldouts = new(k_ExpandableDefault, "APV");

            m_SearchField = new SearchField();
            titleContent = new GUIContent("Probe Volume Settings (Experimental)");

            RefreshSceneAssets();
            m_DrawHorizontalSplitter = typeof(EditorGUIUtility).GetMethod("DrawHorizontalSplitter", BindingFlags.NonPublic | BindingFlags.Static);

            Undo.undoRedoPerformed -= RefreshAfterUndo;
            Undo.undoRedoPerformed += RefreshAfterUndo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= RefreshAfterUndo;
            if (m_ProbeVolumeProfileEditor != null)
                Object.DestroyImmediate(m_ProbeVolumeProfileEditor);

            Lightmapping.lightingDataCleared -= UpdateScenariosStatuses;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        void UpdateSceneData()
        {
            // Should not be needed on top of the Update call.
            EditorUtility.SetDirty(sceneData.parentAsset);
            m_SerializedObject.Update();
        }

        void Initialize()
        {
            if (m_Initialized)
                return;

            m_SubtitleStyle = new GUIStyle(EditorStyles.boldLabel);
            m_SubtitleStyle.fontSize = 20;

            m_SerializedObject = new SerializedObject(sceneData.parentAsset);
            m_ProbeSceneData = m_SerializedObject.FindProperty(sceneData.parentSceneDataPropertyName);

            InitializeBakingSetList();
            InitializeScenarioList();
            UpdateScenariosStatuses();

            Lightmapping.lightingDataCleared += UpdateScenariosStatuses;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            m_Initialized = true;
        }

        void InitializeBakingSetList()
        {
            m_BakingSets = new ReorderableList(sceneData.bakingSets, typeof(ProbeVolumeSceneData.BakingSet), false, false, true, true);
            m_BakingSets.multiSelect = false;
            m_BakingSets.drawElementCallback = (rect, index, active, focused) =>
            {
                // Draw the renamable label for the baking set name
                string key = k_RenameFocusKey + index;
                if (active)
                {
                    if (Event.current.type == EventType.MouseDown && GUI.GetNameOfFocusedControl() != key)
                        m_RenameSelectedBakingSet = false;
                    if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
                    {
                        if (rect.Contains(Event.current.mousePosition))
                            m_RenameSelectedBakingSet = true;
                    }
                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                        m_RenameSelectedBakingSet = false;
                }

                var set = sceneData.bakingSets[index];

                if (m_RenameSelectedBakingSet)
                {
                    EditorGUI.BeginChangeCheck();
                    GUI.SetNextControlName(key);
                    set.name = EditorGUI.DelayedTextField(rect, set.name, EditorStyles.boldLabel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_RenameSelectedBakingSet = false;

                        // Rename profile asset to match name:
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(set.profile), set.name);
                        set.profile.name = set.name;
                    }
                }
                else
                    EditorGUI.LabelField(rect, set.name, EditorStyles.boldLabel);
            };
            m_BakingSets.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight;
            m_BakingSets.onSelectCallback = OnBakingSetSelected;

            m_BakingSets.onAddCallback = (list) =>
            {
                Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Added new baking set");
                sceneData.CreateNewBakingSet("New Baking Set");
                UpdateSceneData();
                OnBakingSetSelected(list);
            };

            m_BakingSets.onRemoveCallback = (list) =>
            {
                if (m_BakingSets.count == 1)
                {
                    EditorUtility.DisplayDialog("Can't delete baking set", "You can't delete the last Baking set. You need to have at least one.", "Ok");
                    return;
                }
                if (EditorUtility.DisplayDialog("Delete the selected baking set?", $"Deleting the baking set will also delete it's profile asset on disk.\nDo you really want to delete the baking set '{sceneData.bakingSets[list.index].name}'?\n\nYou cannot undo the delete assets action.", "Yes", "Cancel"))
                {
                    var pathToDelete = AssetDatabase.GetAssetPath(sceneData.bakingSets[list.index].profile);
                    if (!String.IsNullOrEmpty(pathToDelete))
                        AssetDatabase.DeleteAsset(pathToDelete);
                    Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Deleted baking set");
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    UpdateSceneData();
                    // A new set will be selected automatically, so we perform the same operations as if we did the selection explicitly.
                    OnBakingSetSelected(m_BakingSets);

                }
            };

            m_BakingSets.index = Mathf.Clamp(EditorPrefs.GetInt(k_SelectedBakingSetKey, 0), 0, m_BakingSets.count - 1);

            OnBakingSetSelected(m_BakingSets);
        }

        void SetActiveScenario(string scenario)
        {
            if (scenario == ProbeReferenceVolume.instance.lightingScenario)
                return;
            ProbeReferenceVolume.instance.lightingScenario = scenario;
            EditorUtility.SetDirty(sceneData.parentAsset);
        }

        void InitializeScenarioList()
        {
            m_Scenarios = new ReorderableList(GetCurrentBakingSet().lightingScenarios, typeof(string), true, true, true, true);
            m_Scenarios.multiSelect = false;
            m_Scenarios.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Lighting Scenarios", EditorStyles.largeLabel);
            m_Scenarios.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight;
            m_Scenarios.drawElementCallback = (rect, index, active, focused) =>
            {
                var bakingSet = GetCurrentBakingSet();

                // Status
                if (index < scenariosStatuses.Length && scenariosStatuses[index] != ScenariosStatus.Valid)
                {
                    var status = scenariosStatuses[index];
                    var label = Styles.scenariosStatusLabel[(int)status];
                    var style = status == ScenariosStatus.OutOfDate ? Styles.labelRed : EditorStyles.label;
                    Rect invalidRect = new Rect(rect) { xMin = rect.xMax - style.CalcSize(label).x - 3 };
                    rect.xMax = invalidRect.xMin;

                    using (new EditorGUI.DisabledScope(status != ScenariosStatus.OutOfDate))
                        EditorGUI.LabelField(invalidRect, label, style);
                }

                // Label for active scene
                if (active && bakingSet.sceneGUIDs.Count != 0)
                {
                    Rect labelRect = new Rect(rect) { xMin = infoLabelX };
                    EditorGUI.LabelField(labelRect, Styles.activeScenarioLabel);
                    rect.xMax = labelRect.xMin;
                }

                // Event
                string key = k_RenameFocusKey + index;
                if (active)
                {
                    if (Event.current.type == EventType.MouseDown && GUI.GetNameOfFocusedControl() != key)
                        m_RenameSelectedScenario = false;
                    if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
                    {
                        if (rect.Contains(Event.current.mousePosition))
                            m_RenameSelectedScenario = true;
                    }
                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                        m_RenameSelectedScenario = false;
                }

                // Name
                var scenarioName = bakingSet.lightingScenarios[index];
                if (!m_RenameSelectedScenario || !active)
                    EditorGUI.LabelField(rect, scenarioName);
                else
                {
                    // Renaming
                    EditorGUI.BeginChangeCheck();
                    GUI.SetNextControlName(key);
                    var name = EditorGUI.DelayedTextField(rect, scenarioName, EditorStyles.boldLabel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_RenameSelectedScenario = false;
                        if (AllSetScenesAreLoaded() || EditorUtility.DisplayDialog("Rename Lighting Scenario", "Some scenes in the baking set contain probe volumes but are not loaded.\nRenaming the lighting scenario may require you to rebake the scene.", "Rename", "Cancel"))
                        {
                            try
                            {
                                AssetDatabase.StartAssetEditing();

                                foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                                {
                                    if (bakingSet.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)))
                                        data.RenameScenario(scenarioName, name);
                                }
                                bakingSet.lightingScenarios[index] = name;
                                SetActiveScenario(name);
                            }
                            finally
                            {
                                AssetDatabase.StopAssetEditing();
                                foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                                    data.ResolveCells();
                            }
                        }
                    }
                }
            };

            m_Scenarios.onSelectCallback = (ReorderableList list) =>
            {
                SetActiveScenario(GetCurrentBakingSet().lightingScenarios[list.index]);
                SceneView.RepaintAll();
                Repaint();
            };

            m_Scenarios.onReorderCallback = (ReorderableList list) => UpdateScenariosStatuses();

            m_Scenarios.onAddCallback = (list) =>
            {
                Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Added new lighting scenario");
                var state = GetCurrentBakingSet().CreateScenario("New Lighting Scenario");
                m_Scenarios.index = GetCurrentBakingSet().lightingScenarios.IndexOf(state);
                m_Scenarios.onSelectCallback(m_Scenarios);
                UpdateScenariosStatuses();
            };

            m_Scenarios.onRemoveCallback = (list) =>
            {
                if (m_Scenarios.count == 1)
                {
                    EditorUtility.DisplayDialog("Can't delete scenario", "You can't delete the last scenario. You need to have at least one.", "Ok");
                    return;
                }
                if (!EditorUtility.DisplayDialog("Delete the selected scenario?", $"Deleting the scenario will also delete corresponding baked data on disk.\nDo you really want to delete the scenario '{GetCurrentBakingSet().lightingScenarios[list.index]}'?\n\nYou cannot undo the delete assets action.", "Yes", "Cancel"))
                    return;
                var set = GetCurrentBakingSet();
                var state = set.lightingScenarios[list.index];
                if (!set.RemoveScenario(state))
                    return;
                try
                {
                    AssetDatabase.StartAssetEditing();
                    foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                    {
                        if (set.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)))
                            data.RemoveScenario(state);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    SetActiveScenario(set.lightingScenarios[0]);
                    UpdateScenariosStatuses();
                }
            };

            UpdateScenariosStatuses();
        }

        internal void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene == SceneManager.GetActiveScene())
            {
                // Find the set in which the new active scene belongs
                // If the active baking state does not exist for this set, load the default state of the set
                string sceneGUID = ProbeVolumeSceneData.GetSceneGUID(scene);
                var set = sceneData.bakingSets.FirstOrDefault(s => s.sceneGUIDs.Contains(sceneGUID));
                if (set != null && !set.lightingScenarios.Contains(ProbeReferenceVolume.instance.lightingScenario))
                    SetActiveScenario(set.lightingScenarios[0]);
            }
            UpdateScenariosStatuses();
        }

        internal void UpdateScenariosStatuses()
        {
            if (!m_Initialized)
            {
                hasPendingScenarioUpdate = true;
                return;
            }
            hasPendingScenarioUpdate = false;

            var bakingSet = GetCurrentBakingSet();
            if (bakingSet.sceneGUIDs.Count == 0)
                return;

            DateTime? refTime = null;
            string mostRecentState = null;
            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
            {
                if (!bakingSet.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)))
                    continue;

                foreach (var state in bakingSet.lightingScenarios)
                {
                    if (data.scenarios.TryGetValue(state, out var stateData) && stateData.cellDataAsset != null)
                    {
                        var dataPath = AssetDatabase.GetAssetPath(stateData.cellDataAsset);
                        var time = System.IO.File.GetLastWriteTime(dataPath);
                        if (refTime == null || time > refTime)
                        {
                            refTime = time;
                            mostRecentState = state;
                        }
                    }
                }
            }

            UpdateScenariosStatuses(mostRecentState);
        }

        internal void UpdateScenariosStatuses(string mostRecentState)
        {
            if (!m_Initialized)
            {
                hasPendingScenarioUpdate = true;
                return;
            }
            hasPendingScenarioUpdate = false;

            var initialStatus = AllSetScenesAreLoaded() ? ScenariosStatus.Valid : ScenariosStatus.NotLoaded;

            var bakingSet = GetCurrentBakingSet();
            scenariosStatuses = new ScenariosStatus[bakingSet.lightingScenarios.Count];

            for (int i = 0; i < scenariosStatuses.Length; i++)
            {
                scenariosStatuses[i] = initialStatus;
                if (initialStatus == ScenariosStatus.NotLoaded)
                    continue;

                foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                {
                    if (!bakingSet.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)) || !sceneData.SceneHasProbeVolumes(data.gameObject.scene))
                        continue;

                    if (!data.scenarios.TryGetValue(bakingSet.lightingScenarios[i], out var stateData) || stateData.cellDataAsset == null)
                    {
                        scenariosStatuses[i] = ScenariosStatus.NotBaked;
                        break;
                    }
                    else if (scenariosStatuses[i] != ScenariosStatus.OutOfDate && data.scenarios.TryGetValue(mostRecentState, out var mostRecentData) &&
                        mostRecentData.cellDataAsset != null && stateData.sceneHash != mostRecentData.sceneHash)
                    {
                        scenariosStatuses[i] = ScenariosStatus.OutOfDate;
                    }
                }
            }
        }

        void RefreshAfterUndo()
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
            {
                // Feature not enabled, nothing to do.
                return;
            }

            InitializeBakingSetList();
            InitializeScenarioList();
            UpdateScenariosStatuses();

            OnBakingSetSelected(m_BakingSets);

            Repaint();
        }

        void RefreshSceneAssets()
        {
            var sceneAssets = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets/" });

            m_ScenesInProject = sceneAssets.Select(s =>
            {
                var path = AssetDatabase.GUIDToAssetPath(s);
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                return new SceneData
                {
                    asset = asset,
                    guid = s
                };
            }).ToList();
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

        void OnBakingSetSelected(ReorderableList list)
        {
            // Update left panel data
            EditorPrefs.SetInt(k_SelectedBakingSetKey, list.index);
            var set = GetCurrentBakingSet();

            m_ScenesInSet = new ReorderableList(set.sceneGUIDs, typeof(string), true, true, true, true);
            m_ScenesInSet.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Scenes", EditorStyles.largeLabel);
            m_ScenesInSet.multiSelect = true;
            m_ScenesInSet.drawElementCallback = (rect, index, active, focused) =>
            {
                float CalcLabelWidth(GUIContent c, GUIStyle s) => c.image ? s.CalcSize(c).x - c.image.width + rect.height : s.CalcSize(c).x;

                var guid = set.sceneGUIDs[index];
                // Find scene name from GUID:
                var scene = FindSceneData(guid);

                var sceneLabel = (scene.asset != null) ? new GUIContent(scene.asset.name, Styles.sceneIcon) : Styles.sceneNotFound;
                Rect sceneLabelRect = new Rect(rect) { width = CalcLabelWidth(sceneLabel, EditorStyles.boldLabel) };
                EditorGUI.LabelField(sceneLabelRect, sceneLabel, EditorStyles.boldLabel);
                if (Event.current.type == EventType.MouseDown && sceneLabelRect.Contains(Event.current.mousePosition))
                    EditorGUIUtility.PingObject(scene.asset);

                // display the probe volume icon in the scene if it have one
                Rect probeVolumeIconRect = rect;
                probeVolumeIconRect.xMin = rect.xMax - k_ProbeVolumeIconSize;
                if (sceneData.SceneHasProbeVolumes(scene.guid))
                    EditorGUI.LabelField(probeVolumeIconRect, new GUIContent(Styles.probeVolumeIcon));

                // Display the lighting settings of the first scene (it will be used for baking)
                if (index == 0)
                {
                    var lightingLabel = Styles.sceneLightingSettings;
                    float middle = (sceneLabelRect.xMax + probeVolumeIconRect.xMin) * 0.5f;
                    Rect lightingSettingsRect = new Rect(rect) { xMin = middle - CalcLabelWidth(lightingLabel, EditorStyles.label) * 0.5f };
                    EditorGUI.LabelField(lightingSettingsRect, lightingLabel);
                    infoLabelX = lightingSettingsRect.xMin;
                }
            };
            m_ScenesInSet.onAddCallback = (list) =>
            {
                // TODO: replace this generic menu by a mini-window with a search bar
                var menu = new GenericMenu();

                RefreshSceneAssets();
                foreach (var scene in m_ScenesInProject)
                {
                    if (set.sceneGUIDs.Contains(scene.guid))
                        continue;

                    menu.AddItem(new GUIContent(scene.asset.name), false, () =>
                    {
                        TryAddScene(scene);
                    });
                }

                if (menu.GetItemCount() == 0)
                    menu.AddDisabledItem(new GUIContent("No available scenes"));

                menu.ShowAsContext();
            };
            m_ScenesInSet.onRemoveCallback = (list) =>
            {
                Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Deleted scene in baking set");
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                UpdateSceneData(); // Should not be needed on top of the Update call.
                UpdateScenariosStatuses();
            };

            void TryAddScene(SceneData scene)
            {
                // Don't allow the same scene in two different sets
                Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Added scene in baking set");
                var setWithScene = sceneData.bakingSets.FirstOrDefault(s => s.sceneGUIDs.Contains(scene.guid));
                if (setWithScene != null)
                {
                    if (EditorUtility.DisplayDialog("Move Scene to baking set", $"The scene '{scene.asset.name}' was already added in the baking set '{setWithScene.name}'. Do you want to move it to the current set?", "Yes", "Cancel"))
                    {
                        setWithScene.sceneGUIDs.Remove(scene.guid);
                        set.sceneGUIDs.Add(scene.guid);
                    }
                }
                else
                    set.sceneGUIDs.Add(scene.guid);

                sceneData.SyncBakingSetSettings();
                UpdateSceneData();
                UpdateScenariosStatuses();
            }

            InitializeScenarioList();
        }

        ProbeVolumeSceneData.BakingSet GetCurrentBakingSet()
        {
            int index = Mathf.Clamp(m_BakingSets.index, 0, sceneData.bakingSets.Count - 1);
            return sceneData.bakingSets[index];
        }

        bool AllSetScenesAreLoaded()
        {
            if (!m_Initialized)
            {
                return false;
            }

            var set = GetCurrentBakingSet();
            var dataList = ProbeReferenceVolume.instance.perSceneDataList;

            foreach (var guid in set.sceneGUIDs)
            {
                if (!sceneData.SceneHasProbeVolumes(guid))
                    continue;
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (dataList.All(data => data.gameObject.scene.path != scenePath))
                    return false;
            }

            return true;
        }

        void OnGUI()
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
            {
                ProbeVolumeEditor.APVDisabledHelpBox();
                return;
            }

            if (ProbeReferenceVolume.instance.sceneData?.bakingSets == null)
            {
                EditorGUILayout.HelpBox("Probe Volume Data Not Loaded!", MessageType.Error);
                return;
            }

            // The window can load before the APV system
            Initialize();

            if (hasPendingScenarioUpdate)
                UpdateScenariosStatuses();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawSeparator();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                sceneData.SyncBakingSetSettings();
        }

        void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(k_LeftPanelSize));
            m_LeftScrollPosition = EditorGUILayout.BeginScrollView(m_LeftScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            var titleRect = EditorGUILayout.GetControlRect(true, k_TitleTextHeight);
            EditorGUI.LabelField(titleRect, Styles.bakingSetsTitle, m_SubtitleStyle);
            EditorGUILayout.Space();
            m_BakingSets.DoLayoutList();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawSeparator()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            m_DrawHorizontalSplitter?.Invoke(null, new object[] { new Rect(k_LeftPanelSize, 0, 2, position.height) });
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
        }

        void SanitizeScenes()
        {
            // Remove entries in the list pointing to deleted scenes
            foreach (var set in sceneData.bakingSets)
                set.sceneGUIDs.RemoveAll(guid => FindSceneData(guid).asset == null);
        }

        void DrawRightPanel()
        {
            EditorGUIUtility.labelWidth = k_RightPanelLabelWidth;
            EditorGUILayout.BeginVertical();
            m_RightScrollPosition = EditorGUILayout.BeginScrollView(m_RightScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            using (new EditorGUILayout.HorizontalScope())
            {
                var titleRect = EditorGUILayout.GetControlRect(true, k_TitleTextHeight);
                EditorGUI.LabelField(titleRect, "Probe Volume Settings", m_SubtitleStyle);
                var debugButtonRect = EditorGUILayout.GetControlRect(true, k_TitleTextHeight, GUILayout.Width(k_TitleTextHeight));
                if (GUI.Button(debugButtonRect, Styles.debugButton))
                    OpenProbeVolumeDebugPanel();
            }

            EditorGUILayout.Space();
            SanitizeScenes();
            m_ScenesInSet.DoLayoutList();

            EditorGUILayout.Space();
            m_Scenarios.Select(GetCurrentBakingSet().lightingScenarios.IndexOf(ProbeReferenceVolume.instance.lightingScenario));
            m_Scenarios.DoLayoutList();

            var set = GetCurrentBakingSet();
            var sceneGUID = sceneData.GetFirstProbeVolumeSceneGUID(set);
            if (sceneGUID != null)
            {
                EditorGUILayout.Space();

                // Show only the profile from the first scene of the set (they all should be the same)
                if (set.profile == null)
                {
                    EditorUtility.DisplayDialog("Missing Probe Volume Profile Asset!", $"We couldn't find the asset profile associated with the Baking Set '{set.name}'.\nDo you want to create a new one?", "Yes");
                    set.profile = ScriptableObject.CreateInstance<ProbeReferenceVolumeProfile>();

                    // Delay asset creation, workaround to avoid creating assets while importing another one (SRP can be called from asset import).
                    EditorApplication.update += DelayCreateAsset;
                    void DelayCreateAsset()
                    {
                        EditorApplication.update -= DelayCreateAsset;
                        ProjectWindowUtil.CreateAsset(set.profile, set.name + ".asset");
                    }
                }

                if (m_ProbeVolumeProfileEditor == null)
                    m_ProbeVolumeProfileEditor = Editor.CreateEditor(set.profile);
                if (m_ProbeVolumeProfileEditor.target != set.profile)
                    Editor.CreateCachedEditor(set.profile, m_ProbeVolumeProfileEditor.GetType(), ref m_ProbeVolumeProfileEditor);

                var serializedSets = m_ProbeSceneData.FindPropertyRelative("serializedBakingSets");
                var serializedSet = serializedSets.GetArrayElementAtIndex(m_BakingSets.index);
                var probeVolumeBakingSettings = serializedSet.FindPropertyRelative("settings");

                EditorGUILayout.LabelField("Probe Placement", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(Lightmapping.isRunning))
                    m_ProbeVolumeProfileEditor.OnInspectorGUI();
                EditorGUILayout.Space(3, true);
                EditorGUILayout.PropertyField(probeVolumeBakingSettings);
                EditorGUI.indentLevel--;

                // Clamp to make sure minimum we set for dilation distance is min probe distance
                set.settings.dilationSettings.dilationDistance = Mathf.Max(set.profile.minDistanceBetweenProbes, set.settings.dilationSettings.dilationDistance);

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Styles.stats, EditorStyles.boldLabel);
                {
                    EditorGUI.indentLevel++;
                    if (AllSetScenesAreLoaded())
                    {
                        long sharedCost = 0, scenarioCost = 0;
                        foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                        {
                            if (!set.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)))
                                continue;
                            scenarioCost += data.GetDiskSizeOfScenarioData(ProbeReferenceVolume.instance.lightingScenario);

                            sharedCost += data.GetDiskSizeOfSharedData();
                            foreach (var scenario in set.lightingScenarios)
                                sharedCost += data.GetDiskSizeOfScenarioData(scenario);
                        }

                        EditorGUILayout.LabelField(Styles.scenarioCostStat, EditorGUIUtility.TrTextContent((scenarioCost / (float)(1000 * 1000)).ToString("F1") + " MB"));
                        EditorGUILayout.LabelField(Styles.totalCostStat, EditorGUIUtility.TrTextContent((sharedCost / (float)(1000 * 1000)).ToString("F1") + " MB"));
                    }
                    else
                        EditorGUILayout.HelpBox("Somes scenes of the set are not currently loaded. Stats can't be displayed", MessageType.Info);
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("You need to assign at least one scene with probe volumes to configure the baking settings", MessageType.Error, true);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            DrawBakeButton();

            EditorGUILayout.EndVertical();
        }

        void OpenProbeVolumeDebugPanel()
        {
            var debugPanel = GetWindow<DebugWindow>();
            debugPanel.titleContent = DebugWindow.Styles.windowTitle;
            debugPanel.Show();
            var index = DebugManager.instance.FindPanelIndex(ProbeReferenceVolume.k_DebugPanelName);
            if (index != -1)
                DebugManager.instance.RequestEditorWindowPanelIndex(index);
        }

        static MethodInfo k_EditorGUI_ButtonWithDropdownList = typeof(EditorGUI).GetMethod("ButtonWithDropdownList", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new[] { typeof(GUIContent), typeof(string[]), typeof(GenericMenu.MenuFunction2), typeof(GUILayoutOption[]) }, new ParameterModifier[0]);
        static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            return (bool)k_EditorGUI_ButtonWithDropdownList.Invoke(null, new object[] { content, buttonNames, callback, options });
        }

        static readonly GUIContent k_GenerateLighting = new GUIContent("Generate Lighting");
        static readonly string[] k_BakeOptionsText = { "Bake the set", "Bake loaded scenes", "Bake active scene" };

        void BakeButtonCallback(object data)
        {
            // Order of options in k_BakeOptionsText
            int option = (int)data;
            if (option == 0) // Bake the set
            {
                // Make sure we don't have a partial list as we are loading and baking the whole set.
                ProbeGIBaking.partialBakeSceneList.Clear();
                BakeLightingForSet(GetCurrentBakingSet());
            }
            else if (option == 1) // Bake loaded scenes
            {
                // Make sure we don't have a partial list as we are baking all the loaded scenes.
                ProbeGIBaking.partialBakeSceneList.Clear();
                Lightmapping.BakeAsync();
            }
            else if (option == 2) // Bake active scene
            {
                ProbeGIBaking.partialBakeSceneList.Clear();
                // We are only baking the active scene, so we need the GUID for the active scene
                ProbeGIBaking.partialBakeSceneList.Add(ProbeVolumeSceneData.GetSceneGUID(SceneManager.GetActiveScene()));
                Lightmapping.BakeAsync();
            }
        }

        void DrawBakeButton()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(Lightmapping.isRunning);
            if (GUILayout.Button("Load All Scenes In Set", GUILayout.ExpandWidth(true)))
                LoadScenesInBakingSet(GetCurrentBakingSet());
            if (GUILayout.Button("Clear Loaded Scenes Data"))
                Lightmapping.Clear();
            EditorGUI.EndDisabledGroup();
            if (Lightmapping.isRunning)
            {
                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true)))
                    Lightmapping.Cancel();
            }
            else
            {
                if (ButtonWithDropdownList(k_GenerateLighting, k_BakeOptionsText, BakeButtonCallback))
                    BakeButtonCallback(0);
            }
            EditorGUILayout.EndHorizontal();
        }

        void BakeLightingForSet(ProbeVolumeSceneData.BakingSet set)
        {
            var loadedScenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    loadedScenes.Add(scene.path);
            }

            List<int> scenesToUnload = null;
            List<string> scenesToRestore = null;
            bool sceneSetChanged = loadedScenes.Count != set.sceneGUIDs.Count || loadedScenes.Any(scene => !set.sceneGUIDs.Contains(AssetDatabase.AssetPathToGUID(scene)));
            if (sceneSetChanged)
            {
                // Save current scenes:
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogError("Can't bake while a scene is dirty!");
                    return;
                }

                scenesToUnload = new List<int>();
                scenesToRestore = new List<string>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    scenesToRestore.Add(scene.path);
                    if (!scene.isLoaded)
                        scenesToUnload.Add(i);
                }

                // Load all the scenes
                LoadScenesInBakingSet(set);
            }

            // Then we wait 1 frame for HDRP to render and bake
            bool skipFirstFrame = true;
            EditorApplication.update += WaitRenderAndBake;
            void WaitRenderAndBake()
            {
                if (skipFirstFrame)
                {
                    skipFirstFrame = false;
                    return;
                }
                EditorApplication.update -= WaitRenderAndBake;

                UnityEditor.Lightmapping.BakeAsync();

                // Enqueue scene restore operation after bake is finished
                if (sceneSetChanged)
                    EditorApplication.update += RestoreScenesAfterBake;
            }

            void RestoreScenesAfterBake()
            {
                if (Lightmapping.isRunning)
                    return;

                EditorApplication.update -= RestoreScenesAfterBake;

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                LoadScenes(scenesToRestore);
                foreach (var sceneIndex in scenesToUnload)
                    EditorSceneManager.CloseScene(SceneManager.GetSceneAt(sceneIndex), false);
            }
        }

        void LoadScenesInBakingSet(ProbeVolumeSceneData.BakingSet set)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            LoadScenes(GetCurrentBakingSet().sceneGUIDs.Select(sceneGUID => m_ScenesInProject.FirstOrDefault(s => s.guid == sceneGUID).GetPath()));
        }

        void LoadScenes(IEnumerable<string> scenePathes)
        {
            bool loadFirst = true;
            foreach (var scenePath in scenePathes)
            {
                if (loadFirst)
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                else
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                loadFirst = false;
            }
        }

        void DrawToolbar()
        {
            // Gameobject popup dropdown
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.FlexibleSpace();

            //Search field GUI
            GUILayout.Space(6);
            var searchRect = EditorGUILayout.GetControlRect(false, GUILayout.MaxWidth(300));
            m_SearchString = m_SearchField.OnToolbarGUI(searchRect, m_SearchString);

            GUILayout.EndHorizontal();
        }



        [Overlay(typeof(SceneView), k_OverlayID)]
        [Icon("LightProbeGroup Icon")]
        class ProbeVolumeOverlay : Overlay, ITransientOverlay
        {
            const string k_OverlayID = "APV Overlay";

            Label[] m_Labels = null;

            int maxSubdiv;
            float minDistance;

            public bool visible => IsVisible();

            (int maxSubdiv, float minDistance) GetSettings()
            {
                if (ProbeReferenceVolume.instance.probeVolumeDebug.realtimeSubdivision && ProbeReferenceVolume.instance.sceneData != null)
                {
                    var probeVolume = GameObject.FindObjectOfType<ProbeVolume>();
                    if (probeVolume != null && probeVolume.isActiveAndEnabled)
                    {
                        var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(probeVolume.gameObject.scene);
                        if (profile != null)
                            return (profile.maxSubdivision, profile.minDistanceBetweenProbes);
                    }
                }

                return (ProbeReferenceVolume.instance.GetMaxSubdivision(), ProbeReferenceVolume.instance.MinDistanceBetweenProbes());
            }

            bool IsVisible()
            {
                // Include some state tracking here because it's the only function called at each repaint
                if (!ProbeReferenceVolume.instance.probeVolumeDebug.drawBricks)
                {
                    m_Labels = null;
                    return false;
                }
                if (m_Labels == null) return true;

                (int max, float min) = GetSettings();
                if (maxSubdiv != max)
                {
                    maxSubdiv = max;
                    for (int i = 0; i < m_Labels.Length; i++)
                        m_Labels[i].parent.EnableInClassList("unity-pbr-validation-hidden", i >= maxSubdiv);
                }
                if (minDistance != min)
                {
                    minDistance = min;
                    for (int i = 0; i < m_Labels.Length; i++)
                        m_Labels[i].text = (minDistance * ProbeReferenceVolume.CellSize(i)) + " meters";
                }
                return true;
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
                displayName = "Distance Between probes";
                maxSubdiv = 0;
                minDistance = -1;

                var root = new VisualElement();

                m_Labels = new Label[6];
                for (int i = 0; i < m_Labels.Length; i++)
                {
                    var row = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                    root.Add(row);

                    row.Add(CreateColorSwatch(ProbeReferenceVolume.instance.subdivisionDebugColors[i]));

                    m_Labels[i] = new Label() { name = "color-label" };
                    m_Labels[i].AddToClassList("unity-base-field__label");
                    row.Add(m_Labels[i]);
                }

                return root;
            }
        }
    }
}
