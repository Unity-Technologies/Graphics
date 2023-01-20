using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    // Alias for the display
    using CED = CoreEditorDrawer<WaterSurfaceEditor>;

    sealed class WaterPropertyParameterDrawer
    {
        internal static readonly string[] swellModeNames = new string[] { "Inherit from Swell", "Custom" };
        internal static readonly string[] agitationModeNames = new string[] { "Inherit from Agitation", "Custom" };
        static readonly int popupWidth = 70;
        static readonly int doubleFieldMargin = 10;

        public static void Draw(GUIContent title, SerializedProperty mode, SerializedProperty parameter, string[] modeNames)
        {
            // Save and reset the indent level
            int previousIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect rect = EditorGUILayout.GetControlRect();
            rect.xMax -= popupWidth + 2;

            var popupRect = rect;
            popupRect.x = rect.xMax + 2;
            popupRect.width = popupWidth;
            mode.enumValueIndex = EditorGUI.Popup(popupRect, mode.enumValueIndex, modeNames);

            if (mode.enumValueIndex == (int)WaterPropertyOverrideMode.Inherit)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.showMixedValue = true;
            }

            // Restore the indent level before leaving
            EditorGUI.indentLevel = previousIndentLevel;

            parameter.floatValue = EditorGUI.FloatField(rect, title, parameter.floatValue);

            if (mode.intValue == (int)WaterPropertyOverrideMode.Inherit)
            {
                EditorGUI.showMixedValue = false;
                EditorGUI.EndDisabledGroup();
            }
        }

        public static void DrawMultiPropertiesGUI(GUIContent label, GUIContent subLabel0, SerializedProperty subContent0, GUIContent subLabel1, SerializedProperty subContent1)
        {
            // Draw the label for the whole line
            var prefixLabel = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), label);

            // Save and reset the indent level
            int previousIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Save the previous label with
            float previousLabelWith = EditorGUIUtility.labelWidth;

            // First field
            var speedRect = prefixLabel;
            // Takes half the space
            speedRect.width = speedRect.width / 2 - doubleFieldMargin;
            // Evaluate the space for the first label
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(subLabel0).x + 4;
            // Draw the first property
            subContent0.floatValue = EditorGUI.FloatField(speedRect, subLabel0, subContent0.floatValue);

            // Second field
            var orientationRect = prefixLabel;
            // Takes half the space left
            orientationRect.x += speedRect.width + 2 + doubleFieldMargin * 2;
            orientationRect.width = orientationRect.width / 2 - 2 - doubleFieldMargin;
            // Evaluate the space for the second label
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(subLabel1).x + 2;
            // Draw the second property
            subContent1.floatValue = EditorGUI.FloatField(orientationRect, subLabel1, subContent1.floatValue);

            // Restore the previous label width
            EditorGUIUtility.labelWidth = previousLabelWith;

            // Restore the indent level before leaving
            EditorGUI.indentLevel = previousIndentLevel;
        }

        public static void DrawMultiPropertiesGUI(GUIContent label, SerializedProperty mode, string[] modeNames, GUIContent sublabel,
                                                    GUIContent subLabel0, SerializedProperty subContent0, GUIContent subLabel1, SerializedProperty subContent1)
        {
            mode.enumValueIndex = EditorGUILayout.Popup(label, mode.enumValueIndex, modeNames);

            if (mode.enumValueIndex == (int)WaterPropertyOverrideMode.Inherit)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.showMixedValue = true;
            }

            DrawMultiPropertiesGUI(sublabel, subLabel0, subContent0, subLabel1, subContent1);

            if (mode.intValue == (int)WaterPropertyOverrideMode.Inherit)
            {
                EditorGUI.showMixedValue = false;
                EditorGUI.EndDisabledGroup();
            }
        }
    }

    [CustomEditor(typeof(WaterSurface))]
    sealed partial class WaterSurfaceEditor : Editor
    {
        // General parameters
        SerializedProperty m_SurfaceType;
        SerializedProperty m_GeometryType;
        SerializedProperty m_MeshRenderers;
        SerializedProperty m_TimeMultiplier;

        // CPU Simulation
        SerializedProperty m_CPUSimulation;
        SerializedProperty m_CPUFullResolution;
        SerializedProperty m_CPUEvaluateRipples;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterSurface>(serializedObject);

            // General parameters
            m_SurfaceType = o.Find(x => x.surfaceType);
            m_GeometryType = o.Find(x => x.geometryType);
            m_MeshRenderers = o.Find(x => x.meshRenderers);
            m_TimeMultiplier = o.Find(x => x.timeMultiplier);

            // CPU Simulation
            m_CPUSimulation = o.Find(x => x.cpuSimulation);
            m_CPUFullResolution = o.Find(x => x.cpuFullResolution);
            m_CPUEvaluateRipples = o.Find(x => x.cpuEvaluateRipples);

            // Simulation
            OnEnableSimulation(o);

            // Deformation
            OnEnableDeformation(o);

            // Appearance
            OnEnableAppearance(o);

            // Foam
            OnEnableFoam(o);

            // Misc
            OnEnableMiscellaneous(o);
        }

        static internal bool ValidInfiniteSurface(Transform transform)
        {
            return transform.eulerAngles.x == 0.0
                    && transform.eulerAngles.y == 0.0
                    && transform.eulerAngles.z == 0.0
                    && transform.localScale.x == 1.0
                    && transform.localScale.y == 1.0
                    && transform.localScale.z == 1.0;
        }

        static internal void WaterSurfaceGeneralSection(WaterSurfaceEditor serialized, Editor owner)
        {
            {
                WaterSurfaceType previousSurfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);
                EditorGUILayout.PropertyField(serialized.m_SurfaceType, k_SurfaceType);
                WaterSurfaceType currentSurfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);

                switch (currentSurfaceType)
                {
                    case WaterSurfaceType.OceanSeaLake:
                    {
                        // If we were on something else than an ocean and switched back to an ocean, we force the surface to be infinite again
                        if (previousSurfaceType != currentSurfaceType)
                            serialized.m_GeometryType.enumValueIndex = (int)WaterGeometryType.Infinite;

                        EditorGUILayout.PropertyField(serialized.m_GeometryType, k_GeometryType);
                        using (new IndentLevelScope())
                        {
                            if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex == WaterGeometryType.Custom)
                                EditorGUILayout.PropertyField(serialized.m_MeshRenderers, k_MeshRenderers);
                        }

                        if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex == WaterGeometryType.Infinite)
                        {
                            // Grab the water surface
                            WaterSurface surface = (WaterSurface)(serialized.serializedObject.targetObject);
                            if (!ValidInfiniteSurface(surface.transform))
                            {
                                CoreEditorUtils.DrawFixMeBox(k_FixTransform, MessageType.Info, "Fix", () =>
                                {
                                    WaterSurface ws = (serialized.target as WaterSurface);
                                    var menu = new GenericMenu();
                                    menu.AddItem(k_ResetTransformPopup, false, () => { ws.gameObject.transform.localScale = Vector3.one; ws.gameObject.transform.eulerAngles = Vector3.zero; });
                                    menu.ShowAsContext();
                                });
                            }
                        }
                    }
                    break;
                    case WaterSurfaceType.River:
                    case WaterSurfaceType.Pool:
                        {
                            // If infinite was set, we need to force it to quad or instanced quads based on the new surface type
                            if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex == WaterGeometryType.Infinite)
                                serialized.m_GeometryType.enumValueIndex = currentSurfaceType == WaterSurfaceType.River ? (int)WaterGeometryType.InstancedQuads : (int)WaterGeometryType.Quad;
                            serialized.m_GeometryType.enumValueIndex = EditorGUILayout.Popup(k_GeometryType, serialized.m_GeometryType.enumValueIndex, k_GeometryTypeEnum);
                            if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex == WaterGeometryType.Custom)
                                EditorGUILayout.PropertyField(serialized.m_MeshRenderers, k_MeshRenderers);
                        }
                        break;
                };
            }

            serialized.m_TimeMultiplier.floatValue = EditorGUILayout.Slider(k_TimeMultiplier, serialized.m_TimeMultiplier.floatValue, 0.0f, 10.0f);

            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            // Display the CPU simulation check box, but only make it available if the asset allows it
            bool cpuSimSupported = currentAsset.currentPlatformRenderPipelineSettings.waterCPUSimulation;
            using (new EditorGUI.DisabledScope(!cpuSimSupported))
            {
                using (new BoldLabelScope())
                    serialized.m_CPUSimulation.boolValue = EditorGUILayout.Toggle(k_CPUSimulation, serialized.m_CPUSimulation.boolValue);

                using (new IndentLevelScope())
                {
                    if (serialized.m_CPUSimulation.boolValue)
                    {
                        if (currentAsset.currentPlatformRenderPipelineSettings.waterSimulationResolution == WaterSimulationResolution.Low64)
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                // When in 64, we always show that we are running the CPU simulation at full res.
                                bool fakeToggle = true;
                                EditorGUILayout.Toggle(k_CPUFullResolution, fakeToggle);
                            }
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(serialized.m_CPUFullResolution, k_CPUFullResolution);
                        }

                        WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);

                        // Does the surface support ripples
                        bool canHaveRipples = (surfaceType == WaterSurfaceType.OceanSeaLake || surfaceType == WaterSurfaceType.River);

                        if (canHaveRipples)
                        {
                            using (new EditorGUI.DisabledScope(!serialized.m_Ripples.boolValue))
                            {
                                // When we only have 2 bands, we should evaluate all bands
                                EditorGUILayout.PropertyField(serialized.m_CPUEvaluateRipples, k_CPUEvaluateRipples);
                            }
                        }
                        else
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                // When we only have 2 bands, we should evaluate all bands
                                bool fakeToggle = true;
                                EditorGUILayout.Toggle(k_CPUEvaluateRipples, fakeToggle);
                            }
                        }
                    }
                }
            }

            // Redirect to the asset if disabled
            if (!cpuSimSupported)
            {
                HDEditorUtils.QualitySettingsHelpBox("Enable 'Script Interactions' in your HDRP Asset if you want to replicate the water simulation on CPU. There is a performance cost of enabling this option.",
                    MessageType.Info,
                    HDRenderPipelineUI.ExpandableGroup.Rendering,
                    HDRenderPipelineUI.ExpandableRendering.Water, "m_RenderPipelineSettings.waterCPUSimulation");
                EditorGUILayout.Space();
            }
        }

        class WaterGizmoProperties
        {
            public Texture2D oceanIconL = null;
            public Texture2D oceanIconD = null;
            public Texture2D riverIconL = null;
            public Texture2D riverIconD = null;
            public Texture2D poolIconL = null;
            public Texture2D poolIconD = null;
            public System.Reflection.MethodInfo setIconForObject = null;
        }
        static WaterGizmoProperties waterGizmo = null;

        static WaterGizmoProperties RequestGizmoProperties()
        {
            if (waterGizmo == null)
            {
                waterGizmo = new WaterGizmoProperties();
                waterGizmo.oceanIconL = (Texture2D)Resources.Load("OceanIcon_L");
                waterGizmo.oceanIconD = (Texture2D)Resources.Load("OceanIcon_D");
                waterGizmo.riverIconL = (Texture2D)Resources.Load("RiverIcon_L");
                waterGizmo.riverIconD = (Texture2D)Resources.Load("RiverIcon_D");
                waterGizmo.poolIconL = (Texture2D)Resources.Load("PoolIcon_L");
                waterGizmo.poolIconD = (Texture2D)Resources.Load("PoolIcon_D");
                waterGizmo.setIconForObject = typeof(EditorGUIUtility).GetMethod("SetIconForObject");
            }
            return waterGizmo;
        }

        static Texture2D PickWaterIcon(WaterSurface surface, WaterGizmoProperties wgp)
        {
            switch (surface.surfaceType)
            {
                case WaterSurfaceType.OceanSeaLake:
                    return EditorGUIUtility.isProSkin ? wgp.oceanIconD : wgp.oceanIconL;
                case WaterSurfaceType.River:
                    return EditorGUIUtility.isProSkin ? wgp.riverIconD : wgp.riverIconL;
                case WaterSurfaceType.Pool:
                    return EditorGUIUtility.isProSkin ? wgp.poolIconD : wgp.poolIconL;
            }
            return wgp.oceanIconL;
        }

        public override void OnInspectorGUI()
        {
            // We need to do this, because in case of a domain reload, sometimes everything becomes bold.
            EditorStyles.label.fontStyle = FontStyle.Normal;

            serializedObject.Update();

            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWater ?? false)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("Enable the 'Water' system in your HDRP Asset to simulate and render water surfaces in your HDRP project.",
                    MessageType.Info,
                    HDRenderPipelineUI.ExpandableGroup.Rendering,
                    HDRenderPipelineUI.ExpandableRendering.Water, "m_RenderPipelineSettings.supportWater");
                return;
            }

            // Draw UI
            WaterSurfaceUI.Inspector.Draw(this, this);

            // Apply the properties
            serializedObject.ApplyModifiedProperties();
        }

        // Anis 11/09/21: Currently, there is a bug that makes the icon disappear after the first selection
        // if we do not have this. Given that the geometry is procedural, we need this to be able to
        // select the water surfaces.
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(WaterSurface waterSurface, GizmoType gizmoType)
        {
        }

        static void SanitizeExtentsVector2(SerializedProperty prop)
        {
            Vector2 v2 = prop.vector2Value;
            v2.x = Mathf.Max(v2.x, 0.1f);
            v2.y = Mathf.Max(v2.y, 0.1f);
            prop.vector2Value = v2;
        }
    }

    class WaterSurfaceUI
    {
        public static readonly CED.IDrawer Inspector;

        public static readonly string generalHeader = "General";
        public static readonly string simulationHeader = "Simulation";
        public static readonly string deformationHeader = "Deformation";
        public static readonly string appearanceHeader = "Appearance";
        public static readonly string foamHeader = "Foam";
        public static readonly string miscellaneousHeader = "Miscellaneous";

        enum Expandable
        {
            General = 1 << 0,
            Simulation = 1 << 1,
            Deformation = 1 << 2,
            Appearance = 1 << 3,
            Foam = 1 << 4,
            Miscellaneous = 1 << 5,
        }

        internal enum AdditionalProperties
        {
            Global = 1 << 0,
        }

        readonly static ExpandedState<Expandable, WaterSurface> k_ExpandedState = new ExpandedState<Expandable, WaterSurface>(0, "HDRP");
        readonly internal static AdditionalPropertiesState<AdditionalProperties, WaterSurface> k_AdditionalPropertiesState = new AdditionalPropertiesState<AdditionalProperties, WaterSurface>(0, "HDRP");

        internal static void RegisterEditor(HDLightEditor editor)
        {
            k_AdditionalPropertiesState.RegisterEditor(editor);
        }

        internal static void UnregisterEditor(HDLightEditor editor)
        {
            k_AdditionalPropertiesState.UnregisterEditor(editor);
        }

        [SetAdditionalPropertiesVisibility]
        internal static void SetAdditionalPropertiesVisibility(bool value)
        {
            if (value)
                k_AdditionalPropertiesState.ShowAll();
            else
                k_AdditionalPropertiesState.HideAll();
        }

        internal static bool ShowAdditionalProperties()
        {
            return k_AdditionalPropertiesState[WaterSurfaceUI.AdditionalProperties.Global];
        }

        [MenuItem("CONTEXT/WaterSurface/Show All Additional Properties...", false, 100)]
        static void ShowAllAdditionalProperties(MenuCommand menuCommand)
        {
            CoreRenderPipelinePreferences.Open();
        }

        [MenuItem("CONTEXT/WaterSurface/Reset", false, 0)]
        static void ResetWaterSurface(MenuCommand menuCommand)
        {
            GameObject go = ((WaterSurface)menuCommand.context).gameObject;
            Assert.IsNotNull(go);

            WaterSurface waterSurface = go.GetComponent<WaterSurface>();
            Undo.RecordObject(waterSurface, "Reset Water Surface");

            switch (waterSurface.surfaceType)
            {
                case WaterSurfaceType.OceanSeaLake:
                    WaterSurfacePresets.ApplyWaterOceanPreset(waterSurface);
                    break;
                case WaterSurfaceType.River:
                    WaterSurfacePresets.ApplyWaterRiverPreset(waterSurface);
                    break;
                case WaterSurfaceType.Pool:
                    WaterSurfacePresets.ApplyWaterPoolPreset(waterSurface);
                    break;
                default:
                    break;
            }
        }

        static WaterSurfaceUI()
        {
            Inspector = CED.Group(
                CED.FoldoutGroup(generalHeader, Expandable.General, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceGeneralSection),
                CED.FoldoutGroup(simulationHeader, Expandable.Simulation, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceSimulationSection),
                CED.FoldoutGroup(deformationHeader, Expandable.Deformation, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceDeformationSection),
                CED.FoldoutGroup(appearanceHeader, Expandable.Appearance, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceAppearanceSection),
                CED.FoldoutGroup(foamHeader, Expandable.Foam, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceFoamSection),
                CED.FoldoutGroup(miscellaneousHeader, Expandable.Miscellaneous, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceMiscellaneousSection)
            );
        }
    }
}
