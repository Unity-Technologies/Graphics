using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;
using static UnityEditor.Rendering.HighDefinition.HDProbeUI;

namespace UnityEditor.Rendering.HighDefinition
{
    // Alias for the display
    using CED = CoreEditorDrawer<WaterSurfaceEditor>;

    sealed class WaterPropertyParameterDrawer
    {
        internal static readonly string[] swellModeNames = new string[] { "Swell", "Custom" };
        internal static readonly string[] agitationModeNames = new string[] { "Agitation", "Custom" };
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
        // Geometry parameters
        SerializedProperty m_SurfaceType;
        SerializedProperty m_GeometryType;
        SerializedProperty m_Mesh;

        // CPU Simulation
        SerializedProperty m_CPUSimulation;
        SerializedProperty m_CPUFullResolution;
        SerializedProperty m_CPUEvaluateRipples;

        // Simulation parameters
        SerializedProperty m_TimeMultiplier;

        // Large
        SerializedProperty m_RepetitionSize;
        SerializedProperty m_LargeWindSpeed;
        SerializedProperty m_LargeWindOrientationValue;
        SerializedProperty m_LargeCurrentSpeedValue;
        SerializedProperty m_LargeCurrentOrientationValue;
        SerializedProperty m_LargeChaos;

        // Band0
        SerializedProperty m_LargeBand0Multiplier;
        SerializedProperty m_LargeBand0FadeToggle;
        SerializedProperty m_LargeBand0FadeStart;
        SerializedProperty m_LargeBand0FadeDistance;
        // Band1
        SerializedProperty m_LargeBand1Multiplier;
        SerializedProperty m_LargeBand1FadeToggle;
        SerializedProperty m_LargeBand1FadeStart;
        SerializedProperty m_LargeBand1FadeDistance;

        // Wind
        SerializedProperty m_Ripples;
        SerializedProperty m_RipplesWindSpeed;
        SerializedProperty m_RipplesWindOrientationMode;
        SerializedProperty m_RipplesWindOrientationValue;
        SerializedProperty m_RipplesCurrentMode;
        SerializedProperty m_RipplesCurrentSpeedValue;
        SerializedProperty m_RipplesCurrentOrientationMode;
        SerializedProperty m_RipplesCurrentOrientationValue;
        SerializedProperty m_RipplesChaos;
        SerializedProperty m_RipplesFadeToggle;
        SerializedProperty m_RipplesFadeStart;
        SerializedProperty m_RipplesFadeDistance;

        // Rendering parameters
        SerializedProperty m_CustomMaterial;
        SerializedProperty m_StartSmoothness;
        SerializedProperty m_EndSmoothness;
        SerializedProperty m_SmoothnessFadeStart;
        SerializedProperty m_SmoothnessFadeDistance;

        // Refraction parameters
        SerializedProperty m_MaxRefractionDistance;
        SerializedProperty m_AbsorptionDistance;
        SerializedProperty m_RefractionColor;

        // Scattering parameters
        SerializedProperty m_ScatteringColor;
        SerializedProperty m_AmbientScattering;
        SerializedProperty m_HeightScattering;
        SerializedProperty m_DisplacementScattering;
        SerializedProperty m_DirectLightTipScattering;
        SerializedProperty m_DirectLightBodyScattering;

        // Caustic parameters (Common)
        SerializedProperty m_Caustics;
        SerializedProperty m_CausticsBand;
        SerializedProperty m_CausticsVirtualPlaneDistance;
        SerializedProperty m_CausticsIntensity;
        SerializedProperty m_CausticsResolution;
        SerializedProperty m_CausticsPlaneBlendDistance;

        // Water masking
        SerializedProperty m_WaterMask;
        SerializedProperty m_WaterMaskExtent;
        SerializedProperty m_WaterMaskOffset;

        // Foam
        SerializedProperty m_Foam;
        SerializedProperty m_SimulationFoamAmount;
        SerializedProperty m_SimulationFoamDrag;
        SerializedProperty m_SimulationFoamSmoothness;
        SerializedProperty m_FoamTextureTiling;
        SerializedProperty m_FoamTexture;
        SerializedProperty m_FoamMask;
        SerializedProperty m_FoamMaskExtent;
        SerializedProperty m_FoamMaskOffset;
        SerializedProperty m_WindFoamCurve;

        // Rendering
        SerializedProperty m_DecalLayerMask;
        SerializedProperty m_LightLayerMask;

        // Underwater
        SerializedProperty m_UnderWater;
        SerializedProperty m_VolumeBounds;
        SerializedProperty m_VolumeDepth;
        SerializedProperty m_VolumePriority;
        SerializedProperty m_TransitionSize;
        SerializedProperty m_AbsorbtionDistanceMultiplier;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterSurface>(serializedObject);

            // Geometry parameters
            m_SurfaceType = o.Find(x => x.surfaceType);
            m_GeometryType = o.Find(x => x.geometryType);
            m_Mesh = o.Find(x => x.mesh);

            // CPU Simulation
            m_CPUSimulation = o.Find(x => x.cpuSimulation);
            m_CPUFullResolution = o.Find(x => x.cpuFullResolution);
            m_CPUEvaluateRipples = o.Find(x => x.cpuEvaluateRipples);

            // Band definition parameters
            m_TimeMultiplier = o.Find(x => x.timeMultiplier);

            // Swell
            m_RepetitionSize = o.Find(x => x.repetitionSize);
            m_LargeWindSpeed = o.Find(x => x.largeWindSpeed);
            m_LargeWindOrientationValue = o.Find(x => x.largeWindOrientationValue);
            m_LargeCurrentSpeedValue = o.Find(x => x.largeCurrentSpeedValue);
            m_LargeCurrentOrientationValue = o.Find(x => x.largeCurrentOrientationValue);
            m_LargeChaos = o.Find(x => x.largeChaos);

            // Band0
            m_LargeBand0Multiplier = o.Find(x => x.largeBand0Multiplier);
            m_LargeBand0FadeToggle = o.Find(x => x.largeBand0FadeToggle);
            m_LargeBand0FadeStart = o.Find(x => x.largeBand0FadeStart);
            m_LargeBand0FadeDistance = o.Find(x => x.largeBand0FadeDistance);

            // Band1
            m_LargeBand1Multiplier = o.Find(x => x.largeBand1Multiplier);
            m_LargeBand1FadeToggle = o.Find(x => x.largeBand1FadeToggle);
            m_LargeBand1FadeStart = o.Find(x => x.largeBand1FadeStart);
            m_LargeBand1FadeDistance = o.Find(x => x.largeBand1FadeDistance);

            // Wind parameters
            m_Ripples = o.Find(x => x.ripples);
            m_RipplesWindSpeed = o.Find(x => x.ripplesWindSpeed);
            m_RipplesWindOrientationValue = o.Find(x => x.ripplesWindOrientationValue);
            m_RipplesWindOrientationMode = o.Find(x => x.ripplesWindOrientationMode);
            m_RipplesCurrentMode = o.Find(x => x.ripplesCurrentMode);
            m_RipplesCurrentSpeedValue = o.Find(x => x.ripplesCurrentSpeedValue);
            m_RipplesCurrentOrientationValue = o.Find(x => x.ripplesCurrentOrientationValue);
            m_RipplesChaos = o.Find(x => x.ripplesChaos);
            m_RipplesFadeToggle = o.Find(x => x.ripplesFadeToggle);
            m_RipplesFadeStart = o.Find(x => x.ripplesFadeStart);
            m_RipplesFadeDistance = o.Find(x => x.ripplesFadeDistance);

            // Rendering parameters
            m_CustomMaterial = o.Find(x => x.customMaterial);
            m_StartSmoothness = o.Find(x => x.startSmoothness);
            m_EndSmoothness = o.Find(x => x.endSmoothness);
            m_SmoothnessFadeStart = o.Find(x => x.smoothnessFadeStart);
            m_SmoothnessFadeDistance = o.Find(x => x.smoothnessFadeDistance);

            // Refraction parameters
            m_AbsorptionDistance = o.Find(x => x.absorptionDistance);
            m_MaxRefractionDistance = o.Find(x => x.maxRefractionDistance);
            m_RefractionColor = o.Find(x => x.refractionColor);

            // Scattering parameters
            m_ScatteringColor = o.Find(x => x.scatteringColor);
            m_AmbientScattering = o.Find(x => x.ambientScattering);
            m_HeightScattering = o.Find(x => x.heightScattering);
            m_DisplacementScattering = o.Find(x => x.displacementScattering);
            m_DirectLightTipScattering = o.Find(x => x.directLightTipScattering);
            m_DirectLightBodyScattering = o.Find(x => x.directLightBodyScattering);

            // Caustic parameters
            m_Caustics = o.Find(x => x.caustics);
            m_CausticsBand = o.Find(x => x.causticsBand);
            m_CausticsVirtualPlaneDistance = o.Find(x => x.virtualPlaneDistance);
            m_CausticsIntensity = o.Find(x => x.causticsIntensity);
            m_CausticsResolution = o.Find(x => x.causticsResolution);
            m_CausticsPlaneBlendDistance = o.Find(x => x.causticsPlaneBlendDistance);

            // Foam
            m_Foam = o.Find(x => x.foam);
            m_SimulationFoamAmount = o.Find(x => x.simulationFoamAmount);
            m_SimulationFoamDrag = o.Find(x => x.simulationFoamDrag);
            m_SimulationFoamSmoothness = o.Find(x => x.simulationFoamSmoothness);
            m_FoamTextureTiling = o.Find(x => x.foamTextureTiling);
            m_FoamTexture = o.Find(x => x.foamTexture);

            m_FoamMask = o.Find(x => x.foamMask);
            m_FoamMaskExtent = o.Find(x => x.foamMaskExtent);
            m_FoamMaskOffset = o.Find(x => x.foamMaskOffset);
            m_WindFoamCurve = o.Find(x => x.windFoamCurve);

            // Water masking
            m_WaterMask = o.Find(x => x.waterMask);
            m_WaterMaskExtent = o.Find(x => x.waterMaskExtent);
            m_WaterMaskOffset = o.Find(x => x.waterMaskOffset);

            // Rendering
            m_DecalLayerMask = o.Find(x => x.decalLayerMask);
            m_LightLayerMask = o.Find(x => x.lightLayerMask);

            // Underwater
            m_UnderWater = o.Find(x => x.underWater);
            m_VolumeBounds = o.Find(x => x.volumeBounds);
            m_VolumeDepth = o.Find(x => x.volumeDepth);
            m_VolumePriority = o.Find(x => x.volumePrority);
            m_TransitionSize = o.Find(x => x.transitionSize);
            m_AbsorbtionDistanceMultiplier = o.Find(x => x.absorbtionDistanceMultiplier);
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
                            // If we were on something else than an ocean and swiched back to an ocean, we force the surface to be infite again
                            if (previousSurfaceType != currentSurfaceType)
                            serialized.m_GeometryType.enumValueIndex = (int)WaterGeometryType.Infinite;

                            EditorGUILayout.PropertyField(serialized.m_GeometryType, k_GeometryType);
                            using (new IndentLevelScope())
                            {
                                if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex == WaterGeometryType.CustomMesh)
                                    EditorGUILayout.PropertyField(serialized.m_Mesh, k_Mesh);
                            }
                        }
                        break;
                    case WaterSurfaceType.River:
                    case WaterSurfaceType.Pool:
                        {
                            // If infinite was set, we need to force it to quad
                            if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex == WaterGeometryType.Infinite)
                                serialized.m_GeometryType.enumValueIndex = (int)WaterGeometryType.Quad;
                            serialized.m_GeometryType.enumValueIndex = EditorGUILayout.Popup(k_GeometryType, serialized.m_GeometryType.enumValueIndex, k_GeometryTypeEnum);
                            if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex == WaterGeometryType.CustomMesh)
                                EditorGUILayout.PropertyField(serialized.m_Mesh, k_Mesh);
                        }
                        break;
                };
            }

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
                    MessageType.Info, HDRenderPipelineUI.Expandable.Water, "m_RenderPipelineSettings.waterCPUSimulation");
                EditorGUILayout.Space();
            }
        }

        static internal void WaterSurfaceMiscellaneousSection(WaterSurfaceEditor serialized, Editor owner)
        {
            // Decal controls
            if (HDRenderPipeline.currentPipeline != null && HDRenderPipeline.currentPipeline.currentPlatformRenderPipelineSettings.supportDecals)
            {
                bool decalLayerEnabled = false;
                using (new IndentLevelScope())
                {
                    decalLayerEnabled = HDRenderPipeline.currentPipeline.currentPlatformRenderPipelineSettings.supportDecalLayers;
                    using (new EditorGUI.DisabledScope(!decalLayerEnabled))
                    {
                        EditorGUILayout.PropertyField(serialized.m_DecalLayerMask);
                    }
                }

                if (!decalLayerEnabled)
                {
                    HDEditorUtils.QualitySettingsHelpBox("Enable 'Decal Layers' in your HDRP Asset if you want to control which decals affect water surfaces. There is a performance cost of enabling this option.",
                        MessageType.Info, HDRenderPipelineUI.Expandable.Decal, "m_RenderPipelineSettings.supportDecalLayers");
                    EditorGUILayout.Space();
                }
            }

            if (HDRenderPipeline.currentPipeline != null)
            {
                bool lightLayersEnabled = HDRenderPipeline.currentPipeline.currentPlatformRenderPipelineSettings.supportLightLayers;
                using (new IndentLevelScope())
                {
                    using (new EditorGUI.DisabledScope(!lightLayersEnabled))
                    {
                        EditorGUILayout.PropertyField(serialized.m_LightLayerMask);
                    }
                }

                if (!lightLayersEnabled)
                {
                    HDEditorUtils.QualitySettingsHelpBox("Enable 'Light Layers' in your HDRP Asset if you want defined which lights affect water surfaces. There is a performance cost of enabling this option.",
                        MessageType.Info, HDRenderPipelineUI.Expandable.Lighting, "m_RenderPipelineSettings.supportLightLayers");
                    EditorGUILayout.Space();
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

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (currentAsset == null || !currentAsset.currentPlatformRenderPipelineSettings.supportWater)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("Enable the 'Water' system in your HDRP Asset to simulate and render water surfaces in your HDRP project.",
                    MessageType.Info, HDRenderPipelineUI.Expandable.Water, "m_RenderPipelineSettings.supportWater");
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
    }

    class WaterSurfaceUI
    {
        public static readonly CED.IDrawer Inspector;

        public static readonly string generalHeader = "General";
        public static readonly string simulationHeader = "Simulation";
        public static readonly string appearanceHeader = "Appearance";
        public static readonly string refractionHeader = "Refraction";
        public static readonly string scatteringHeader = "Scattering";
        public static readonly string miscellaneousHeader = "Miscellaneous";

        enum Expandable
        {
            General = 1 << 0,
            Simulation = 1 << 1,
            Appearance = 1 << 2,
            Miscellaneous = 1 << 3,
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
        static void ResetLight(MenuCommand menuCommand)
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
                CED.FoldoutGroup(appearanceHeader, Expandable.Appearance, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceAppearanceSection),
                CED.FoldoutGroup(miscellaneousHeader, Expandable.Miscellaneous, k_ExpandedState, WaterSurfaceEditor.WaterSurfaceMiscellaneousSection)
            );
        }
    }
}
