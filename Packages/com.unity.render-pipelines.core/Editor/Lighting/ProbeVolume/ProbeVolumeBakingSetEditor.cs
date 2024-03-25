using System;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering
{
    [CustomPropertyDrawer(typeof(LogarithmicAttribute))]
    class LogarithmicDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // First get the attribute since it contains the range for the slider
            var range = attribute as LogarithmicAttribute;

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.LogarithmicIntSlider(position, label, property.intValue, range.min, range.max, 2, 1, 1 << 30);
            if (EditorGUI.EndChangeCheck())
                property.intValue = Mathf.ClosestPowerOfTwo(newValue);

            EditorGUI.EndProperty();
            EditorGUI.showMixedValue = false;
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeVolumeBakingSet))]
    internal class ProbeVolumeBakingSetEditor : Editor
    {
        SerializedProperty m_MinDistanceBetweenProbes;
        SerializedProperty m_SimplificationLevels;
        SerializedProperty m_MinRendererVolumeSize;
        SerializedProperty m_RenderersLayerMask;
        SerializedProperty m_FreezePlacement;
        SerializedProperty m_ProbeOffset;
        SerializedProperty m_ProbeVolumeBakingSettings;
        SerializedProperty m_LightingScenarios;
        SerializedProperty m_SkyOcclusion;
        SerializedProperty m_SkyOcclusionBakingSamples;
        SerializedProperty m_SkyOcclusionBakingBounces;
        SerializedProperty m_SkyOcclusionAverageAlbedo;
        SerializedProperty m_SkyOcclusionShadingDirection;

        ProbeVolumeBakingSet bakingSet => target as ProbeVolumeBakingSet;

        static class Styles
        {
            public static readonly GUIContent scenariosTitle = new GUIContent("Lighting Scenarios");
            public static readonly GUIContent placementTitle = new GUIContent("Probe Placement");
            public static readonly GUIContent invaliditySettingsTitle = new GUIContent("Probe Invalidity Settings");
            public static readonly GUIContent skyOcclusionSettingsTitle = new GUIContent("Sky Occlusion Settings");

            public static readonly GUIContent keepSamePlacement = new GUIContent("Probe Positions", "If set to Don't Recalculate, probe positions are not recalculated when baking. Allows baking multiple Scenarios that include small differences in Scene geometry.");
            public static readonly string[] placementOptions = new string[] { "Recalculate", "Don't Recalculate" };

            // Scenario section
            public static readonly GUIContent emptyLabel = new GUIContent("", CoreEditorStyles.GetMessageTypeIcon(MessageType.Info), "This scenario doesn't have any baked data. Set it as active scenario and click generate lighting to bake the lighting data.");

            // Probe Placement section
            public static readonly string msgProbeFreeze = "Some scene(s) in this Baking Set are not currently loaded in the Hierarchy. Set Probe Positions to Don't Recalculate to not break compatibility with already baked scenarios.";
            public static readonly GUIContent probeOffset = new GUIContent("Probe Offset", "Offset on world origin used during baking. Can be used to have cells on positions that are not multiples of the probe spacing.");
            public static readonly GUIContent maxDistanceBetweenProbes = new GUIContent("Max Probe Spacing", "Maximum distance between probes, in meters. Determines the number of Bricks in a streamable unit.");
            public static readonly GUIContent minDistanceBetweenProbes = new GUIContent("Min Probe Spacing", "Minimum distance between probes, in meters.");
            public static readonly string simplificationLevelsHighWarning = " Using this many brick sizes will result in high memory usage and can cause instabilities.";
            public static readonly GUIContent indexDimensions = new GUIContent("Index Dimensions", "The dimensions of the index buffer.");
            public static readonly GUIContent minRendererVolumeSize = new GUIContent("Min Renderer Size", "The smallest Renderer size to consider when placing probes.");
            public static readonly GUIContent renderersLayerMask = new GUIContent("Layer Mask", "Specify Layers to use when generating probe positions.");
            public static readonly GUIContent rendererFilterSettings = new GUIContent("Renderer Filter Settings");

            public static readonly GUIContent skyOcclusion = new GUIContent("Sky Occlusion", "Choose whether to generate Sky Occlusion data for probes within this Probe Volume. When enabled, Scenes can be dynamically re-lit when the sky is changed. This feature increases memory usage.");
            public static readonly GUIContent skyOcclusionBakingSamples = new GUIContent("Samples", "The number of samples used to calculate the influence of the sky when baking probes. Increasing this value improves the accuracy of Sky Occlusion data, but increases the time required to generate baked lighting.");
            public static readonly GUIContent skyOcclusionBakingBounces = new GUIContent("Bounces", "The maximum number of bounces allowed for each Sky Occlusion sample. Increasing this value particularly improves the accuracy of occlusion data in areas of the Scene with complicated routes to the sky.");
            public static readonly GUIContent skyOcclusionAverageAlbedo = new GUIContent("Albedo Override", "Sky Occlusion does not consider the albedo of materials in the Scene when calculating bounced light from the sky. Albedo Override determines the value used instead. Lower values darken and higher values will brighten the Scene.");
            public static readonly GUIContent skyOcclusionShadingDirection = new GUIContent("Sky Direction", "For each probe, additionally bake the most suitable direction to use for sampling the Scene’s Ambient Probe. When disabled, surface normals are used instead. Sky Direction improves visual quality at the expense of memory.");
            public static readonly GUIContent cpuLightmapperNotSupportedWarning = new GUIContent("Sky Occlusion is not supported by the current lightmapper. Ensure that Progressive GPU is selected in Lightmapper Settings.");


            // Probe Settings section
            public static readonly GUIContent resetDilation = new GUIContent("Reset Dilation Settings");
            public static readonly GUIContent resetVirtualOffset = new GUIContent("Reset Virtual Offset Settings");
        }

        static readonly string s_RenameScenarioUndoName = "Rename Baking Set Scenario";

        void OnEnable()
        {
            m_MinDistanceBetweenProbes = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.minDistanceBetweenProbes));
            m_SimplificationLevels = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.simplificationLevels));
            m_MinRendererVolumeSize = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.minRendererVolumeSize));
            m_RenderersLayerMask = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.renderersLayerMask));
            m_FreezePlacement = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.freezePlacement));
            m_ProbeOffset = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.probeOffset));
            m_ProbeVolumeBakingSettings = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.settings));
            m_LightingScenarios = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.m_LightingScenarios));
			m_SkyOcclusion = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.skyOcclusion));
            m_SkyOcclusionBakingSamples = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.skyOcclusionBakingSamples));
            m_SkyOcclusionBakingBounces = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.skyOcclusionBakingBounces));
            m_SkyOcclusionAverageAlbedo = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.skyOcclusionAverageAlbedo));
            m_SkyOcclusionShadingDirection = serializedObject.FindProperty(nameof(ProbeVolumeBakingSet.skyOcclusionShadingDirection));

            if (ProbeReferenceVolume.instance.supportScenarioBlending)
                InitializeScenarioList();

            Undo.undoRedoEvent += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoEvent -= OnUndoRedo;
        }

        void OnUndoRedo(in UndoRedoInfo info)
        {
            if (bakingSet != null && info.undoName == s_RenameScenarioUndoName)
                bakingSet.EnsureScenarioAssetNameConsistencyForUndo();
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
            bool canFreezePlacement = AdaptiveProbeVolumes.CanFreezePlacement();
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

                AdaptiveProbeVolumes.isFreezingPlacement = canFreezePlacement && m_FreezePlacement.boolValue;

                if (canFreezePlacement && !AdaptiveProbeVolumes.isFreezingPlacement && m_LightingScenarios.arraySize > 1)
                {
                    foreach (var guid in bakingSet.sceneGUIDs)
                    {
                        Scene scene = SceneManager.GetSceneByPath(AssetDatabase.GUIDToAssetPath(guid));
                        if (scene.isLoaded) continue;

                        if (bakingSet.HasBeenBaked())
                        {
                            EditorGUILayout.HelpBox(Styles.msgProbeFreeze, MessageType.Warning);
                            EditorGUILayout.Space();
                        }
                        break;
                    }
                }
            }

            using (new EditorGUI.DisabledScope(Lightmapping.isRunning || (canFreezePlacement && AdaptiveProbeVolumes.isFreezingPlacement)))
            {
                // Display vector3 ourselves otherwise display is messed up
                {
                    var rect = EditorGUILayout.GetControlRect();
                    EditorGUI.BeginProperty(rect, Styles.probeOffset, m_ProbeOffset);

                    rect = EditorGUI.PrefixLabel(rect, Styles.probeOffset);
                    rect.xMin -= 10 * EditorGUIUtility.pixelsPerPoint;

                    EditorGUI.BeginChangeCheck();
                    var value = EditorGUI.Vector3Field(rect, GUIContent.none, m_ProbeOffset.vector3Value);
                    if (EditorGUI.EndChangeCheck())
                        m_ProbeOffset.vector3Value = value;

                    EditorGUI.EndProperty();
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_MinDistanceBetweenProbes, Styles.minDistanceBetweenProbes);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    // Clamp to make sure minimum we set for dilation distance is min probe distance
                    bakingSet.settings.dilationSettings.dilationDistance = Mathf.Max(bakingSet.minDistanceBetweenProbes, bakingSet.settings.dilationSettings.dilationDistance);
                    serializedObject.Update();
                }

                SimplificationLevelsSlider();

                int levels = ProbeVolumeBakingSet.GetMaxSubdivision(m_SimplificationLevels.intValue);
                MessageType helpBoxType = MessageType.Info;
                string helpBoxText = $"Baked Probe Volume data will contain up-to {levels} different sizes of Brick.";
                if (levels == 6)
                {
                    helpBoxType = MessageType.Warning;
                    helpBoxText += Styles.simplificationLevelsHighWarning;
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

        void ProbeInvaliditySettingsGUI()
        {
            if (!ProbeVolumeLightingTab.Foldout(Styles.invaliditySettingsTitle, ProbeVolumeLightingTab.Expandable.InvaliditySettings, true, ResetProbeSettings))
                return;

            using (new EditorGUI.IndentLevelScope())
                EditorGUILayout.PropertyField(m_ProbeVolumeBakingSettings);

            EditorGUILayout.Space();
        }

        void SkyOcclusionSettingsGUI()
        {
            if (!SupportedRenderingFeatures.active.skyOcclusion)
                return;
            if (!ProbeVolumeLightingTab.Foldout(Styles.skyOcclusionSettingsTitle, ProbeVolumeLightingTab.Expandable.SettingsSkyOcclusion, true))
                return;

            using var scope = new EditorGUI.IndentLevelScope();

            var lightmapper = ProbeVolumeLightingTab.GetLightingSettings().lightmapper;
            bool cpuLightmapperSelected = lightmapper == LightingSettings.Lightmapper.ProgressiveCPU;
            if (cpuLightmapperSelected)
            {
                EditorGUILayout.HelpBox(Styles.cpuLightmapperNotSupportedWarning.text, MessageType.Warning);
            }
            using (new EditorGUI.DisabledScope(cpuLightmapperSelected))
            {
                EditorGUILayout.PropertyField(m_SkyOcclusion, Styles.skyOcclusion);

                if (m_SkyOcclusion.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_SkyOcclusionBakingSamples, Styles.skyOcclusionBakingSamples);
                    EditorGUILayout.PropertyField(m_SkyOcclusionBakingBounces, Styles.skyOcclusionBakingBounces);
                    EditorGUILayout.PropertyField(m_SkyOcclusionAverageAlbedo, Styles.skyOcclusionAverageAlbedo);
                    EditorGUILayout.PropertyField(m_SkyOcclusionShadingDirection, Styles.skyOcclusionShadingDirection);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ProbePlacementGUI();
            LightingScenariosGUI();
            SkyOcclusionSettingsGUI();
            ProbeInvaliditySettingsGUI();

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
                    bakingSet.settings.dilationSettings.SetDefaults();
                else
                    bakingSet.settings.virtualOffsetSettings.SetDefaults();
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

            ProbeVolumeLightingTab.DrawSimplificationLevelsMarkers(rect, bakingSet.minDistanceBetweenProbes, 2, highestSimplification, value, value);
            EditorGUI.EndProperty();
        }
        #endregion

        #region Lighting Scenarios

        const string k_RenameFocusKey = "Probe Volume Rename Field";

        ReorderableList m_Scenarios = null;

        bool renameSelectedScenario;

        void SetActiveScenario(string scenario)
        {
            if (scenario == ProbeReferenceVolume.instance.lightingScenario)
                return;

            Undo.RegisterCompleteObjectUndo(bakingSet, "Change active scenario");
            bakingSet.SetActiveScenario(scenario, false);
            EditorUtility.SetDirty(bakingSet);
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
                }
            };

            m_Scenarios.drawElementCallback = (rect, index, active, focused) =>
            {
                ProbeVolumeLightingTab.SplitRectInThree(rect, out var left, out var middle, out var right, 70);
                var scenarioName = bakingSet.m_LightingScenarios[index];

                // Status
                {
                    right.xMin += 8f;
                    right.width = right.height;

                    bool baked = bakingSet.scenarios.TryGetValue(scenarioName, out var stateData) && stateData.ComputeHasValidData(ProbeReferenceVolume.instance.shBands);

                    using (new EditorGUI.DisabledScope(true))
                        GUI.Label(right, baked ? GUIContent.none : Styles.emptyLabel);
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
                        try
                        {
                            AssetDatabase.StartAssetEditing();
                            Undo.RegisterCompleteObjectUndo(bakingSet, s_RenameScenarioUndoName);
                            name = bakingSet.RenameScenario(scenarioName, name);
                        }
                        finally
                        {
                            AssetDatabase.StopAssetEditing();
                            m_LightingScenarios.GetArrayElementAtIndex(index).stringValue = name;
                            serializedObject.Update();
                            serializedObject.ApplyModifiedProperties();

                            SetActiveScenario(name);
                        }
                    }
                }
            };

            m_Scenarios.onSelectCallback = (ReorderableList list) =>
            {
                SetActiveScenario(bakingSet.m_LightingScenarios[list.index]);
                SceneView.RepaintAll();
                Repaint();
            };

            m_Scenarios.onAddCallback = (list) =>
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RegisterCompleteObjectUndo(bakingSet, "Added new lighting scenario");
                var scenario = bakingSet.CreateScenario("New Lighting Scenario");
                serializedObject.Update();
            };

            m_Scenarios.onRemoveCallback = (list) =>
            {
                if (m_Scenarios.count == 1)
                {
                    EditorUtility.DisplayDialog("Can't delete scenario", "You can't delete the last scenario. You need to have at least one.", "Ok");
                    return;
                }
                var scenario = bakingSet.m_LightingScenarios[list.index];
                if (!EditorUtility.DisplayDialog("Delete the selected scenario?", $"Deleting the scenario will also delete corresponding baked data on disk.\nDo you really want to delete the scenario '{scenario}'?\n\nYou cannot undo the delete assets action.", "Yes", "Cancel"))
                    return;
                serializedObject.ApplyModifiedProperties();
                if (!bakingSet.RemoveScenario(scenario))
                    return;
                serializedObject.Update();

                try
                {
                    AssetDatabase.StartAssetEditing();
                    bakingSet.RemoveScenario(scenario);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    SetActiveScenario(bakingSet.m_LightingScenarios[0]);
                }
            };
        }
        #endregion
    }
}
