using System;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeVolumeBakingSet))]
    internal class ProbeVolumeBakingSetEditor : Editor
    {
        SerializedProperty m_MinDistanceBetweenProbes;
        SerializedProperty m_SimplificationLevels;
        SerializedProperty m_MinRendererVolumeSize;
        SerializedProperty m_RenderersLayerMask;
        SerializedProperty m_FreezePlacement;
        SerializedProperty m_ProbeVolumeBakingSettings;
        SerializedProperty m_LightingScenarios;
        ProbeVolumeBakingSet profile => target as ProbeVolumeBakingSet;

        static class Styles
        {
            public static readonly GUIContent scenariosTitle = new GUIContent("Lighting Scenarios");
            public static readonly GUIContent placementTitle = new GUIContent("Probe Placement");
            public static readonly GUIContent settingsTitle = new GUIContent("Probe Invalidity Settings");

            public static readonly GUIContent keepSamePlacement = new GUIContent("Probe Positions", "If set to Don't Recalculate, probe positions are not recalculated when baking. Allows baking multiple Scenarios that include small differences in Scene geometry.");
            public static readonly string[] placementOptions = new string[] { "Recalculate", "Don't Recalculate" };

            // Scenario section
            public static readonly GUIContent invalidLabel = new GUIContent("", CoreEditorStyles.GetMessageTypeIcon(MessageType.Warning), "Lighting data for this scenario is invalid.\nThis can happen when probe positions have changed since lighting was last generated.");
            public static readonly GUIContent emptyLabel = new GUIContent("", CoreEditorStyles.GetMessageTypeIcon(MessageType.Info), "This scenario doesn't have any baked data. Set it as active scenario and click generate lighting to bake the lighting data.");
            public static readonly GUIContent notLoadedLabel = new GUIContent("", CoreEditorStyles.GetMessageTypeIcon(MessageType.Info), "Some scene(s) in the Baking Set are not currently loaded in the Hierarchy. Scenario status cannot be displayed");
            public static readonly GUIContent[] scenariosStatusLabel = new GUIContent[] { GUIContent.none, notLoadedLabel, invalidLabel, emptyLabel };

            // Probe Placement section
            public static readonly string msgProbeFreeze = "Some scene(s) in this Baking Set are not currently loaded in the Hierarchy. Set Probe Positions to Don't Recalculate to not break compatibility with already baked scenarios.";
            public static readonly GUIContent maxDistanceBetweenProbes = new GUIContent("Max Probe Spacing", "Maximum distance between probes, in meters. Determines the number of bricks in a streamable unit.");
            public static readonly GUIContent minDistanceBetweenProbes = new GUIContent("Min Probe Spacing", "Minimum distance between probes, in meters.");
            public static readonly string simplificationLevelsHighWarning = "High simplification levels have a big memory overhead, they are not recommended except for testing purposes.";
            public static readonly GUIContent indexDimensions = new GUIContent("Index Dimensions", "The dimensions of the index buffer.");
            public static readonly GUIContent minRendererVolumeSize = new GUIContent("Min Renderer Size", "The smallest Renderer size to consider when placing probes.");
            public static readonly GUIContent renderersLayerMask = new GUIContent("Layer Mask", "Specify Layers to use when generating probe positions.");
            public static readonly GUIContent rendererFilterSettings = new GUIContent("Renderer Filter Settings");

            // Probe Settings section
            public static readonly GUIContent resetDilation = new GUIContent("Reset Dilation Settings");
            public static readonly GUIContent resetVirtualOffset = new GUIContent("Reset Virtual Offset Settings");
        }

        void OnEnable()
        {
            m_MinDistanceBetweenProbes = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.minDistanceBetweenProbes));
            m_SimplificationLevels = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.simplificationLevels));
            m_MinRendererVolumeSize = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.minRendererVolumeSize));
            m_RenderersLayerMask = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.renderersLayerMask));
            m_FreezePlacement = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.freezePlacement));
            m_ProbeVolumeBakingSettings = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.settings));
            m_LightingScenarios = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.lightingScenarios));

            if (ProbeReferenceVolume.instance.enableScenarioBlending)
            {
                hasPendingScenarioUpdate = true;
                Lightmapping.lightingDataCleared += UpdateScenarioStatuses;
                InitializeScenarioList();
            }
        }

        private void OnDisable()
        {
            Lightmapping.lightingDataCleared -= UpdateScenarioStatuses;
        }

        void LightingScenariosGUI()
        {
            if (!ProbeReferenceVolume.instance.supportLightingScenarios)
                return;

            if (!ProbeVolumeLightingTab.Foldout(Styles.scenariosTitle, ProbeVolumeLightingTab.Expandable.Scenarios, true))
                return;

            EditorGUI.indentLevel++;

            if (m_Scenarios == null)
                InitializeScenarioList();
            if (hasPendingScenarioUpdate)
                UpdateScenarioStatuses();

            using (new EditorGUI.IndentLevelScope())
                ProbeVolumeLightingTab.DrawListWithIndent(m_Scenarios);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        void ProbePlacementGUI()
        {
            if (!ProbeVolumeLightingTab.Foldout(Styles.placementTitle, ProbeVolumeLightingTab.Expandable.Placement, true))
                return;

            EditorGUI.indentLevel++;
            bool canFreezePlacement = ProbeGIBaking.CanFreezePlacement();
            if (ProbeReferenceVolume.instance.supportLightingScenarios)
            {
                using (new EditorGUI.DisabledGroupScope(!canFreezePlacement))
                {
                    EditorGUI.BeginChangeCheck();
                    bool freeze = canFreezePlacement ? m_FreezePlacement.boolValue : false;
                    freeze = EditorGUILayout.Popup(Styles.keepSamePlacement, freeze ? 1 : 0, Styles.placementOptions) == 1;
                    if (EditorGUI.EndChangeCheck())
                        m_FreezePlacement.boolValue = freeze;
                }

                ProbeGIBaking.isFreezingPlacement = canFreezePlacement && m_FreezePlacement.boolValue;

                if (canFreezePlacement && !ProbeGIBaking.isFreezingPlacement && m_LightingScenarios.arraySize > 1)
                {
                    foreach (var guid in profile.sceneGUIDs)
                    {
                        Scene scene = SceneManager.GetSceneByPath(AssetDatabase.GUIDToAssetPath(guid));
                        if (scene.isLoaded) continue;

                        foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                        {
                            if (profile.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)))
                            {
                                if (data.asset != null)
                                {
                                    EditorGUILayout.HelpBox(Styles.msgProbeFreeze, MessageType.Warning);
                                    EditorGUILayout.Space();
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            using (new EditorGUI.DisabledScope(Lightmapping.isRunning || (canFreezePlacement && ProbeGIBaking.isFreezingPlacement)))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_MinDistanceBetweenProbes, Styles.minDistanceBetweenProbes);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    // Clamp to make sure minimum we set for dilation distance is min probe distance
                    profile.settings.dilationSettings.dilationDistance = Mathf.Max(profile.minDistanceBetweenProbes, profile.settings.dilationSettings.dilationDistance);
                    serializedObject.Update();
                }

                SimplificationLevelsSlider();

                int levels = m_SimplificationLevels.intValue + 1;
                MessageType helpBoxType = MessageType.Info;
                string helpBoxText = $"Probe Volumes can use a maximum of {levels} subdivision levels.";
                if (levels == 5)
                {
                    helpBoxType = MessageType.Warning;
                    helpBoxText += "\n" + Styles.simplificationLevelsHighWarning;
                }
                EditorGUILayout.HelpBox(helpBoxText, helpBoxType);

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUI.indentLevel++;
                if (ProbeVolumeLightingTab.Foldout(Styles.rendererFilterSettings, ProbeVolumeLightingTab.Expandable.PlacementFilters, false))
                {
                    EditorGUILayout.PropertyField(m_RenderersLayerMask, Styles.renderersLayerMask);
                    EditorGUILayout.PropertyField(m_MinRendererVolumeSize, Styles.minRendererVolumeSize);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        void ProbeSettingsGUI()
        {
            if (!ProbeVolumeLightingTab.Foldout(Styles.settingsTitle, ProbeVolumeLightingTab.Expandable.Settings, true, ResetProbeSettings))
                return;

            using (new EditorGUI.IndentLevelScope())
                EditorGUILayout.PropertyField(m_ProbeVolumeBakingSettings);

            EditorGUILayout.Space();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ProbePlacementGUI();
            LightingScenariosGUI();
            ProbeSettingsGUI();

            serializedObject.ApplyModifiedProperties();
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        void ResetProbeSettings(Rect rect)
        {
            EditorUtility.DisplayCustomMenu(rect, new[] { Styles.resetDilation, Styles.resetVirtualOffset }, -1, (object userData, string[] options, int selected) => {
                if (selected == 0)
                    profile.settings.dilationSettings.SetDefaults();
                else
                    profile.settings.virtualOffsetSettings.SetDefaults();
            }, null);
        }

        #region Probe Placement

        static int s_SimplificationSliderID = "SimplificationLevelSlider".GetHashCode();

        void SimplificationLevelsSlider()
        {
            const int highestSimplification = 5;

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.maxDistanceBetweenProbes, m_SimplificationLevels);

            int id = GUIUtility.GetControlID(s_SimplificationSliderID, FocusType.Keyboard, rect);
            rect = EditorGUI.PrefixLabel(rect, id, Styles.maxDistanceBetweenProbes);

            int value = m_SimplificationLevels.intValue;
            EditorGUI.BeginChangeCheck();
            value = Mathf.RoundToInt(GUI.Slider(rect, value, 0, 2, highestSimplification, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, true, id, "horizontalsliderthumbextent"));
            if (GUIUtility.hotControl == id)
                GUIUtility.keyboardControl = id;
            if (EditorGUI.EndChangeCheck())
                m_SimplificationLevels.intValue = value;

            ProbeVolumeLightingTab.DrawSimplificationLevelsMarkers(rect, profile.minDistanceBetweenProbes, 2, highestSimplification, value, value);
            EditorGUI.EndProperty();
        }
        #endregion

        #region Lighting Scenarios

        enum ScenariosStatus
        {
            Valid,
            NotLoaded,
            OutOfDate,
            NotBaked
        }

        const string k_RenameFocusKey = "Probe Volume Rename Field";

        ReorderableList m_Scenarios = null;

        bool renameSelectedScenario;
        bool hasPendingScenarioUpdate = false;
        ScenariosStatus[] scenariosStatuses = new ScenariosStatus[0];

        void SetActiveScenario(string scenario)
        {
            if (scenario == ProbeReferenceVolume.instance.lightingScenario)
                return;

            Undo.RegisterCompleteObjectUndo(ProbeReferenceVolume.instance.sceneData.parentAsset, "Change active scenario");
            ProbeReferenceVolume.instance.lightingScenario = scenario;
            EditorUtility.SetDirty(ProbeReferenceVolume.instance.sceneData.parentAsset);
            SceneView.RepaintAll();
        }

        void InitializeScenarioList()
        {
            m_Scenarios = new ReorderableList(serializedObject, m_LightingScenarios, true, true, true, true)
            {
                multiSelect = false,
                elementHeightCallback = _ => EditorGUIUtility.singleLineHeight,

                drawHeaderCallback = (rect) =>
                {
                    ProbeVolumeLightingTab.SplitRectInThree(rect, out var left, out var middle, out var right, 70);

                    EditorGUI.LabelField(left, "Scenario");
                    EditorGUI.LabelField(middle, "Active");
                    EditorGUI.LabelField(right, "Status");
                },

                onReorderCallback = (ReorderableList list) =>
                {
                    serializedObject.ApplyModifiedProperties();
                    UpdateScenarioStatuses();
                }
            };

            m_Scenarios.drawElementCallback = (rect, index, active, focused) =>
            {
                ProbeVolumeLightingTab.SplitRectInThree(rect, out var left, out var middle, out var right, 70);
                var scenarioName = profile.lightingScenarios[index];

                // Status
                if (index < scenariosStatuses.Length && scenariosStatuses[index] != ScenariosStatus.Valid)
                {
                    right.xMin += 8f;
                    right.width = right.height;

                    var status = scenariosStatuses[index];
                    var label = Styles.scenariosStatusLabel[(int)status];

                    using (new EditorGUI.DisabledScope(status != ScenariosStatus.OutOfDate))
                        GUI.Label(right, label);
                }

                // Label for active scene
                middle.xMin += 10;
                middle.yMin += 2;
                middle.width = 19;
                EditorGUI.BeginChangeCheck();
                bool toggled = EditorGUI.Toggle(middle, ProbeReferenceVolume.instance.lightingScenario == scenarioName, EditorStyles.radioButton);
                if (EditorGUI.EndChangeCheck() && toggled)
                    SetActiveScenario(scenarioName);

                // Event
                string key = k_RenameFocusKey + index;
                if (active)
                {
                    if (Event.current.type == EventType.MouseDown && GUI.GetNameOfFocusedControl() != key)
                        renameSelectedScenario = false;
                    if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
                    {
                        if (left.Contains(Event.current.mousePosition))
                            renameSelectedScenario = true;
                    }
                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                        renameSelectedScenario = false;
                }

                // Name
                if (!renameSelectedScenario || !active)
                    EditorGUI.LabelField(left, scenarioName);
                else
                {
                    // Renaming
                    EditorGUI.BeginChangeCheck();
                    GUI.SetNextControlName(key);
                    var name = EditorGUI.DelayedTextField(left, scenarioName, EditorStyles.boldLabel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        renameSelectedScenario = false;
                        if (ProbeVolumeLightingTab.AllSetScenesAreLoaded(profile) || EditorUtility.DisplayDialog("Rename Lighting Scenario", "Some scenes in the baking set contain probe volumes but are not loaded.\nRenaming the lighting scenario may require you to rebake the scene.", "Rename", "Cancel"))
                        {
                            try
                            {
                                AssetDatabase.StartAssetEditing();

                                foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                                {
                                    if (profile.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)))
                                        data.RenameScenario(scenarioName, name);
                                }
                            }
                            finally
                            {
                                AssetDatabase.StopAssetEditing();
                                m_LightingScenarios.GetArrayElementAtIndex(index).stringValue = name;
                                serializedObject.ApplyModifiedProperties();

                                SetActiveScenario(name);
                                UpdateScenarioStatuses();

                                foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                                    data.ResolveCells();
                            }
                        }
                    }
                }
            };

            m_Scenarios.onSelectCallback = (ReorderableList list) =>
            {
                SetActiveScenario(profile.lightingScenarios[list.index]);
                SceneView.RepaintAll();
                Repaint();
            };

            m_Scenarios.onAddCallback = (list) =>
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RegisterCompleteObjectUndo(profile, "Added new lighting scenario");
                var scenario = profile.CreateScenario("New Lighting Scenario");
                EditorUtility.SetDirty(profile);
                serializedObject.Update();

                UpdateScenarioStatuses();
            };

            m_Scenarios.onRemoveCallback = (list) =>
            {
                if (m_Scenarios.count == 1)
                {
                    EditorUtility.DisplayDialog("Can't delete scenario", "You can't delete the last scenario. You need to have at least one.", "Ok");
                    return;
                }
                var scenario = profile.lightingScenarios[list.index];
                if (!EditorUtility.DisplayDialog("Delete the selected scenario?", $"Deleting the scenario will also delete corresponding baked data on disk.\nDo you really want to delete the scenario '{scenario}'?\n\nYou cannot undo the delete assets action.", "Yes", "Cancel"))
                    return;
                serializedObject.ApplyModifiedProperties();
                if (!profile.RemoveScenario(scenario))
                    return;
                serializedObject.Update();

                try
                {
                    AssetDatabase.StartAssetEditing();
                    foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                    {
                        if (profile.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)))
                            data.RemoveScenario(scenario);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    SetActiveScenario(profile.lightingScenarios[0]);
                    UpdateScenarioStatuses();
                }
            };

            hasPendingScenarioUpdate = true;
        }

        internal void UpdateScenarioStatuses()
        {
            hasPendingScenarioUpdate = false;

            if (profile.sceneGUIDs.Count == 0)
                return;

            DateTime? refTime = null;
            string mostRecentState = null;
            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
            {
                if (!profile.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)))
                    continue;

                foreach (var state in profile.lightingScenarios)
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

            UpdateScenarioStatuses(mostRecentState);
        }

        internal void UpdateScenarioStatuses(string mostRecentState)
        {
            hasPendingScenarioUpdate = false;

            var initialStatus = ProbeVolumeLightingTab.AllSetScenesAreLoaded(profile) ? ScenariosStatus.Valid : ScenariosStatus.NotLoaded;

            scenariosStatuses = new ScenariosStatus[profile.lightingScenarios.Count];

            for (int i = 0; i < scenariosStatuses.Length; i++)
            {
                scenariosStatuses[i] = initialStatus;
                if (initialStatus == ScenariosStatus.NotLoaded)
                    continue;

                foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                {
                    if (!profile.sceneGUIDs.Contains(ProbeVolumeSceneData.GetSceneGUID(data.gameObject.scene)) || !ProbeReferenceVolume.instance.sceneData.SceneHasProbeVolumes(data.gameObject.scene))
                        continue;

                    if (!data.scenarios.TryGetValue(profile.lightingScenarios[i], out var stateData) || stateData.cellDataAsset == null)
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

        #endregion
    }
}
