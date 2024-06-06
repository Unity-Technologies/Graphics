using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class WaterSurfaceEditor : Editor
    {
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
        SerializedProperty m_MaximumHeightOverride;

        // Caustic parameters (Common)
        SerializedProperty m_Caustics;
        SerializedProperty m_CausticsBand;
        SerializedProperty m_CausticsVirtualPlaneDistance;
        SerializedProperty m_CausticsTilingFactor;
        SerializedProperty m_CausticsIntensity;
        SerializedProperty m_CausticsResolution;
        SerializedProperty m_CausticsPlaneBlendDistance;
        SerializedProperty m_CausticsDirectionalShadow;
        SerializedProperty m_CausticsDirectionalShadowDimmer;

        // Underwater
        SerializedProperty m_UnderWater;
        SerializedProperty m_VolumeBounds;
        SerializedProperty m_VolumeDepth;
        SerializedProperty m_VolumeHeight;
        SerializedProperty m_VolumePriority;
        SerializedProperty m_AbsorptionDistanceMultiplier;
        SerializedProperty m_UnderWaterRefraction;

        void OnEnableAppearance(PropertyFetcher<WaterSurface> o)
        {
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
            m_MaximumHeightOverride = o.Find(x => x.maximumHeightOverride);

            // Caustic parameters
            m_Caustics = o.Find(x => x.caustics);
            m_CausticsBand = o.Find(x => x.causticsBand);
            m_CausticsVirtualPlaneDistance = o.Find(x => x.virtualPlaneDistance);
            m_CausticsTilingFactor = o.Find(x => x.causticsTilingFactor);
            m_CausticsIntensity = o.Find(x => x.causticsIntensity);
            m_CausticsResolution = o.Find(x => x.causticsResolution);
            m_CausticsPlaneBlendDistance = o.Find(x => x.causticsPlaneBlendDistance);
            m_CausticsDirectionalShadow = o.Find(x => x.causticsDirectionalShadow);
            m_CausticsDirectionalShadowDimmer = o.Find(x => x.causticsDirectionalShadowDimmer);

            // Underwater
            m_UnderWater = o.Find(x => x.underWater);
            m_VolumeBounds = o.Find(x => x.volumeBounds);
            m_VolumeDepth = o.Find(x => x.volumeDepth);
            m_VolumeHeight = o.Find(x => x.volumeHeight);
            m_VolumePriority = o.Find(x => x.volumePrority);
            m_AbsorptionDistanceMultiplier = o.Find(x => x.absorptionDistanceMultiplier);
            m_UnderWaterRefraction = o.Find(x => x.underWaterRefraction);
        }

        static internal bool WaterBandHasAgitation(WaterSurfaceEditor serialized, Editor owner, int bandIndex)
        {
            WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);
            switch (surfaceType)
            {
                case WaterSurfaceType.OceanSeaLake:
                    return (bandIndex == 2 ? serialized.m_RipplesWindSpeed.floatValue : serialized.m_LargeWindSpeed.floatValue) > 0.0f;
                case WaterSurfaceType.River:
                    return (bandIndex == 1 ? serialized.m_RipplesWindSpeed.floatValue : serialized.m_LargeWindSpeed.floatValue) > 0.0f;
                case WaterSurfaceType.Pool:
                    return serialized.m_RipplesWindSpeed.floatValue > 0.0f;
            }
            return false;
        }

        internal static string GetWaterResourcesPath(MonoBehaviour component)
        {
            string sceneName = component.gameObject.scene.name;
            if (string.IsNullOrEmpty(sceneName))
                sceneName = "Untitled";

            string folderName = $"Assets/WaterResources/{sceneName}";
            CoreUtils.EnsureFolderTreeInAssetFilePath(folderName);
            return folderName;
        }

        internal static Material CreateNewWaterMaterialAndShader(MonoBehaviour component)
        {
            string directory = GetWaterResourcesPath(component);
            System.IO.Directory.CreateDirectory(directory);

            // Make sure they don't already exist
            var path = $"{directory}/{component.name}.shadergraph";
            if (AssetDatabase.AssetPathExists(path))
            {
                Debug.LogWarning($"A Water Shader or Material at {path} already exists.");
                return null;
            }

            var shader = GraphicsSettings.GetRenderPipelineSettings<WaterSystemRuntimeResources>().waterPS;
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(shader), path))
            {
                Debug.LogWarning($"Failed to copy the Water Shader Graph to {path}");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        static internal void MaterialFieldWithButton(GUIContent label, SerializedProperty prop, System.Func<Material> onClick)
        {
            const int k_NewFieldWidth = 70;

            var rect = EditorGUILayout.GetControlRect();
            rect.xMax -= k_NewFieldWidth + 2;

            var newFieldRect = rect;
            newFieldRect.x = rect.xMax + 2;
            newFieldRect.width = k_NewFieldWidth;
            if (GUI.Button(newFieldRect, "New", EditorStyles.miniButton))
            {
                var value = onClick();
                if (value != null)
                    prop.objectReferenceValue = value;
            }

            if (label != null)
                PropertyField(rect, prop, label);
            else
                PropertyField(rect, prop);
        }

        static void WaterCustomMaterialField(WaterSurfaceEditor serialized)
        {
            MaterialFieldWithButton(k_CustomMaterial, serialized.m_CustomMaterial, () => {
                WaterSurface ws = (serialized.target as WaterSurface);
                return CreateNewWaterMaterialAndShader(ws);
            });

            var material = serialized.m_CustomMaterial.objectReferenceValue as Material;
            if (material != null && !WaterSurface.IsWaterMaterial(material))
                EditorGUILayout.HelpBox("Water only work with a material using a shader created from the Water Master Node in ShaderGraph.", MessageType.Error);

            EditorGUILayout.Space();
        }

        static internal void WaterSurfaceAppearanceSection(WaterSurfaceEditor serialized, Editor owner)
        {
            // Handle the custom material field
            WaterCustomMaterialField(serialized);

            // Grab the type of the surface
            WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);

            EditorGUILayout.LabelField("Smoothness", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();
                Vector2 remap = new Vector2(serialized.m_EndSmoothness.floatValue, serialized.m_StartSmoothness.floatValue);
                EditorGUILayout.MinMaxSlider(k_SmoothnessRange, ref remap.x, ref remap.y, 0.0f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.m_EndSmoothness.floatValue = remap.x;
                    serialized.m_StartSmoothness.floatValue = remap.y;
                }

                // Fade range
                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SmoothnessFadeRange, k_SmoothnessFadeStart, serialized.m_SmoothnessFadeStart, k_SmoothnessFadeDistance, serialized.m_SmoothnessFadeDistance);
            }

            // Refraction section
            EditorGUILayout.LabelField("Refraction", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                CoreEditorUtils.ColorFieldLinear(serialized.m_RefractionColor, k_RefractionColor);
                EditorGUILayout.PropertyField(serialized.m_MaxRefractionDistance, k_MaxRefractionDistance);
                EditorGUILayout.PropertyField(serialized.m_AbsorptionDistance, k_AbsorptionDistance);
            }

            EditorGUILayout.LabelField("Scattering", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                CoreEditorUtils.ColorFieldLinear(serialized.m_ScatteringColor, k_ScatteringColor);
                EditorGUILayout.PropertyField(serialized.m_AmbientScattering, k_AmbientScattering);
                EditorGUILayout.PropertyField(serialized.m_HeightScattering, k_HeightScattering);
                EditorGUILayout.PropertyField(serialized.m_DisplacementScattering, k_DisplacementScattering);

                // Given the low amplitude of the pool waves, it doesn't make any sense to have the tip scattering term available to users
                if (surfaceType != WaterSurfaceType.Pool)
                    EditorGUILayout.PropertyField(serialized.m_DirectLightTipScattering, k_DirectLightTipScattering);
                EditorGUILayout.PropertyField(serialized.m_DirectLightBodyScattering, k_DirectLightBodyScattering);

                EditorGUILayout.PropertyField(serialized.m_MaximumHeightOverride);
            }

            // Caustics
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Caustics, k_Caustics);
            if (serialized.m_Caustics.boolValue)
            {
                using (new IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serialized.m_CausticsResolution);
                    int bandCount = WaterSystem.EvaluateBandCount(surfaceType, serialized.m_Ripples.boolValue);

                    if (bandCount != 1 && !serialized.m_SurfaceType.hasMultipleDifferentValues && !serialized.m_Ripples.hasMultipleDifferentValues)
                    {
                        int bandIdx = WaterSystem.SanitizeCausticsBand(serialized.m_CausticsBand.intValue, bandCount);

                        GUIContent label = null;
                        List<GUIContent> options = new();
                        List<int> values = new();
                        if (surfaceType == WaterSurfaceType.OceanSeaLake)
                        {
                            label = k_CausticsBandSwell;
                            options.Add(new GUIContent("Swell First Band"));
                            options.Add(new GUIContent("Swell Second Band"));
                            values.Add(0);
                            values.Add(1);
                        }
                        if (surfaceType == WaterSurfaceType.River)
                        {
                            label = k_CausticsBandAgitation;
                            options.Add(new GUIContent("Agitation"));
                            values.Add(0);
                            if (bandIdx == 1 && serialized.m_Ripples.boolValue)
                                bandIdx = 2;
                        }

                        if (serialized.m_Ripples.boolValue)
                        {
                            options.Add(new GUIContent("Ripples"));
                            values.Add(2);
                        }

                        EditorGUI.BeginChangeCheck();
                        int value = EditorGUILayout.IntPopup(label, bandIdx, options.ToArray(), values.ToArray());
                        if (EditorGUI.EndChangeCheck())
                            serialized.m_CausticsBand.intValue = value;
                    }

                    EditorGUILayout.PropertyField(serialized.m_CausticsVirtualPlaneDistance, k_CausticsVirtualPlaneDistance);
                    EditorGUILayout.PropertyField(serialized.m_CausticsTilingFactor, k_CausticsTilingFactor);

                    if (AdvancedProperties.BeginGroup())
                    {
                        EditorGUILayout.PropertyField(serialized.m_CausticsIntensity, k_CausticsInstensity);
                        EditorGUILayout.PropertyField(serialized.m_CausticsPlaneBlendDistance);
                        EditorGUILayout.PropertyField(serialized.m_CausticsDirectionalShadow, k_CausticsDirectionalShadow);

                        if (serialized.m_CausticsDirectionalShadow.boolValue)
                        {
                            using (new IndentLevelScope())
                                EditorGUILayout.PropertyField(serialized.m_CausticsDirectionalShadowDimmer, k_CausticsDirectionalShadowDimmer);
                        }
                    }
                    AdvancedProperties.EndGroup();

                    // Display an info box if the wind speed is null for the target band
                    if (!WaterBandHasAgitation(serialized, owner, WaterSystem.SanitizeCausticsBand(serialized.m_CausticsBand.intValue, bandCount)))
                    {
                        EditorGUILayout.HelpBox("The selected simulation band has currently a null wind speed and will not generate caustics.", MessageType.Info, wide: true);
                    }
                }
            }

            // Under Water Rendering
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_UnderWater, k_UnderWater);
            using (new IndentLevelScope())
            {
                if (serialized.m_UnderWater.boolValue)
                {
                    // Bounds data
                    if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex != WaterGeometryType.Infinite)
                    {
                        EditorGUILayout.PropertyField(serialized.m_VolumeBounds);
                        if (serialized.m_VolumeBounds.objectReferenceValue == null)
                        {
                            CoreEditorUtils.DrawFixMeBox(k_AddColliderMessage, MessageType.Warning, "Fix", () =>
                            {
                                WaterSurface ws = (serialized.target as WaterSurface);
                                var menu = new GenericMenu();
                                BoxCollider previousBC = ws.gameObject.GetComponent<BoxCollider>();
                                if (previousBC != null)
                                    menu.AddItem(k_UseBoxColliderPopup, false, () => ws.volumeBounds = previousBC);
                                menu.AddItem(k_AddBoxColliderPopup, false, () => { BoxCollider bc = ws.gameObject.AddComponent<BoxCollider>(); ws.volumeBounds = bc; bc.isTrigger = true;});
                                menu.ShowAsContext();
                            });
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serialized.m_VolumeDepth);
                        EditorGUILayout.PropertyField(serialized.m_VolumeHeight);
                    }

                    // Priority
                    EditorGUILayout.PropertyField(serialized.m_VolumePriority);

                    // View distance
                    EditorGUILayout.PropertyField(serialized.m_AbsorptionDistanceMultiplier);

                    // Refraction fallback
                    EditorGUILayout.PropertyField(serialized.m_UnderWaterRefraction, k_UnderWaterRefraction);
                }
            }
        }
    }
}
