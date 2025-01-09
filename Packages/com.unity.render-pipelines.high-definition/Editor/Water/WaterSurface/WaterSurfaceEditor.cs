using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    // Alias for the display
    using CED = CoreEditorDrawer<WaterSurfaceEditor>;

    sealed class WaterPropertyParameterDrawer
    {
        internal static readonly string[] swellModeNames = new string[] { "Inherit from Swell", "Custom" };
        internal static readonly string[] agitationModeNames = new string[] { "Inherit from Agitation", "Custom" };
        static readonly int doubleFieldMargin = 10;

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
            using (new EditorGUI.MixedValueScope(subContent0.hasMultipleDifferentValues))
            {
                EditorGUI.BeginChangeCheck();
                float value = EditorGUI.FloatField(speedRect, subLabel0, subContent0.floatValue);
                if (EditorGUI.EndChangeCheck())
                    subContent0.floatValue = value;
            }

            // Second field
            var orientationRect = prefixLabel;
            // Takes half the space left
            orientationRect.x += speedRect.width + 2 + doubleFieldMargin * 2;
            orientationRect.width = orientationRect.width / 2 - 2 - doubleFieldMargin;
            // Evaluate the space for the second label
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(subLabel1).x + 2;
            // Draw the second property
            using (new EditorGUI.MixedValueScope(subContent1.hasMultipleDifferentValues))
            {
                EditorGUI.BeginChangeCheck();
                float value = EditorGUI.FloatField(orientationRect, subLabel1, subContent1.floatValue);
                if (EditorGUI.EndChangeCheck())
                    subContent1.floatValue = value;
            }

            // Restore the previous label width
            EditorGUIUtility.labelWidth = previousLabelWith;

            // Restore the indent level before leaving
            EditorGUI.indentLevel = previousIndentLevel;
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaterSurface))]
    sealed partial class WaterSurfaceEditor : Editor
    {
        // General parameters
        SerializedProperty m_SurfaceType;
        SerializedProperty m_GeometryType;
        SerializedProperty m_MeshRenderers;
        SerializedProperty m_TimeMultiplier;

        // Tessellation parameters
        SerializedProperty m_Tessellation;
        SerializedProperty m_MaxTessellationFactor;
        SerializedProperty m_TessellationFactorFadeStart;
        SerializedProperty m_TessellationFactorFadeRange;

        // CPU Simulation
        SerializedProperty m_ScriptInteractions;
        SerializedProperty m_CPUEvaluateRipples;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterSurface>(serializedObject);

            // General parameters
            m_SurfaceType = o.Find(x => x.surfaceType);
            m_GeometryType = o.Find(x => x.geometryType);
            m_MeshRenderers = o.Find(x => x.meshRenderers);
            m_TimeMultiplier = o.Find(x => x.timeMultiplier);

            // Tessellation parameters
            m_Tessellation = o.Find(x => x.tessellation);
            m_MaxTessellationFactor = o.Find(x => x.maxTessellationFactor);
            m_TessellationFactorFadeStart = o.Find(x => x.tessellationFactorFadeStart);
            m_TessellationFactorFadeRange = o.Find(x => x.tessellationFactorFadeRange);

            // CPU Simulation
            m_ScriptInteractions = o.Find(x => x.scriptInteractions);
            m_CPUEvaluateRipples = o.Find(x => x.cpuEvaluateRipples);

            // Simulation
            OnEnableSimulation(o);

            // Deformation
            OnEnableDecals(o);

            // Appearance
            OnEnableAppearance(o);

            // Foam
            OnEnableFoam(o);

            // Misc
            OnEnableMiscellaneous(o);
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
                    }
                    break;

                    case WaterSurfaceType.River:
                    case WaterSurfaceType.Pool:
                    {
                        // If infinite was set, we need to force it to quad or instanced quads based on the new surface type
                        if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex == WaterGeometryType.Infinite)
                            serialized.m_GeometryType.enumValueIndex = currentSurfaceType == WaterSurfaceType.River ? (int)WaterGeometryType.InstancedQuads : (int)WaterGeometryType.Quad;

                        EditorGUI.BeginChangeCheck();

                        var rect = EditorGUILayout.GetControlRect();
                        EditorGUI.BeginProperty(rect, k_GeometryType, serialized.m_GeometryType);
                        var value = EditorGUI.Popup(rect, k_GeometryType, serialized.m_GeometryType.enumValueIndex, k_GeometryTypeEnum);
                        EditorGUI.EndProperty();

                        if (EditorGUI.EndChangeCheck())
                            serialized.m_GeometryType.enumValueIndex = value;
                    }
                    break;
                };

                using (new EditorGUI.IndentLevelScope())
                {
                    if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex == WaterGeometryType.Custom)
                        EditorGUILayout.PropertyField(serialized.m_MeshRenderers, k_MeshRenderers);
                }
            }

            EditorGUILayout.PropertyField(serialized.m_TimeMultiplier, k_TimeMultiplier);

            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_ScriptInteractions);

            using (new EditorGUI.IndentLevelScope())
            {
                if (serialized.m_ScriptInteractions.boolValue)
                {
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

            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Tessellation);
            if (serialized.m_Tessellation.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serialized.m_MaxTessellationFactor);
                    if (AdvancedProperties.BeginGroup())
                    {
                        EditorGUILayout.PropertyField(serialized.m_TessellationFactorFadeStart);
                        EditorGUILayout.PropertyField(serialized.m_TessellationFactorFadeRange);
                    }
                    AdvancedProperties.EndGroup();
                }
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

        static void MapWithExtent(SerializedProperty maskProp, GUIContent content, SerializedProperty extentProp)
        {
            var wasEmpty = maskProp.objectReferenceValue == null;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(maskProp, content);
            if (EditorGUI.EndChangeCheck() && wasEmpty && maskProp.objectReferenceValue != null)
            {
                var waterSurface = maskProp.serializedObject.targetObject as WaterSurface;
                if (!waterSurface.IsInfinite())
                {
                    var scale = waterSurface.transform.lossyScale;
                    extentProp.vector2Value = new Vector2(scale.x, scale.z);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // We need to do this, because in case of a domain reload, sometimes everything becomes bold.
            EditorStyles.label.fontStyle = FontStyle.Normal;

            serializedObject.Update();

            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (currentAsset == null || !currentAsset.currentPlatformRenderPipelineSettings.supportWater)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("Enable the 'Water' system in your HDRP Asset to simulate and render water surfaces in your HDRP project.",
                    MessageType.Info, HDRenderPipelineUI.ExpandableGroup.Rendering, HDRenderPipelineUI.ExpandableRendering.Water, "m_RenderPipelineSettings.supportWater");
                return;
            }

            HDEditorUtils.EnsureVolume((WaterRendering water) => !water.enable.value ? "Water Surface Rendering is not enabled in the Volume System." : null);
            HDEditorUtils.EnsureFrameSetting(FrameSettingsField.Water);

            if (target is WaterSurface surface && surface.surfaceIndex == -1)
            {
                EditorGUILayout.HelpBox("Only up to 16 water surfaces are supported simultaneously. This surface will not be rendered.", MessageType.Warning);
                EditorGUILayout.Space();
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
            if (prop.hasMultipleDifferentValues)
                return;

            Vector2 v2 = prop.vector2Value;
            v2.x = Mathf.Max(v2.x, 0.1f);
            v2.y = Mathf.Max(v2.y, 0.1f);
            prop.vector2Value = v2;
        }
    }

    class WaterSurfaceUI
    {
        public static readonly CED.IDrawer Inspector;

        public static readonly GUIContent generalHeader = EditorGUIUtility.TrTextContent("General");
        public static readonly GUIContent simulationHeader = EditorGUIUtility.TrTextContent("Simulation");
        public static readonly GUIContent decalHeader = EditorGUIUtility.TrTextContent("Water Decals");
        public static readonly GUIContent appearanceHeader = EditorGUIUtility.TrTextContent("Appearance");
        public static readonly GUIContent foamHeader = EditorGUIUtility.TrTextContent("Foam");
        public static readonly GUIContent miscellaneousHeader = EditorGUIUtility.TrTextContent("Miscellaneous");

        enum Expandable
        {
            General = 1 << 0,
            Simulation = 1 << 1,
            Decal = 1 << 2,
            Appearance = 1 << 3,
            Foam = 1 << 4,
            Miscellaneous = 1 << 5,
        }

        internal enum AdditionalProperties
        {
            General = 1 << 0,
            Simulation = 1 << 1,
            Appearance = 1 << 3,
        }

        readonly static ExpandedState<Expandable, WaterSurface> k_ExpandedState = new ExpandedState<Expandable, WaterSurface>(0, "HDRP");
        readonly internal static AdditionalPropertiesState<AdditionalProperties, WaterSurface> k_AdditionalPropertiesState = new AdditionalPropertiesState<AdditionalProperties, WaterSurface>(0, "HDRP");

        [MenuItem("CONTEXT/WaterSurface/Open Preferences > Graphics...", false, 100)]
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
            var emptyDrawer =
            CED.Group(
                (s, e) => { });

            Inspector = CED.Group(
                CED.AdditionalPropertiesFoldoutGroup(generalHeader, Expandable.General, k_ExpandedState,
                    AdditionalProperties.General, k_AdditionalPropertiesState, CED.Group(WaterSurfaceEditor.WaterSurfaceGeneralSection), emptyDrawer),
                CED.AdditionalPropertiesFoldoutGroup(simulationHeader, Expandable.Simulation, k_ExpandedState,
                    AdditionalProperties.Simulation, k_AdditionalPropertiesState, CED.Group(WaterSurfaceEditor.WaterSurfaceSimulationSection), emptyDrawer),
                CED.FoldoutGroup(decalHeader, Expandable.Decal, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceDecalSection),
                CED.AdditionalPropertiesFoldoutGroup(appearanceHeader, Expandable.Appearance, k_ExpandedState,
                    AdditionalProperties.Appearance, k_AdditionalPropertiesState, CED.Group(WaterSurfaceEditor.WaterSurfaceAppearanceSection), emptyDrawer),
                CED.FoldoutGroup(foamHeader, Expandable.Foam, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceFoamSection),
                CED.FoldoutGroup(miscellaneousHeader, Expandable.Miscellaneous, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceMiscellaneousSection)
                );
        }
    }
}
