using UnityEngine;
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
        SerializedProperty m_CausticsIntensity;
        SerializedProperty m_CausticsResolution;
        SerializedProperty m_CausticsPlaneBlendDistance;

        // Underwater
        SerializedProperty m_UnderWater;
        SerializedProperty m_VolumeBounds;
        SerializedProperty m_VolumeDepth;
        SerializedProperty m_VolumeHeight;
        SerializedProperty m_VolumePriority;
        SerializedProperty m_AbsorptionDistanceMultiplier;
        SerializedProperty m_ColorPyramidOffset;
        SerializedProperty m_UnderWaterScatteringColorMode;
        SerializedProperty m_UnderWaterScatteringColor;
        SerializedProperty m_UnderWaterAmbientProbeContribution;

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
            m_CausticsIntensity = o.Find(x => x.causticsIntensity);
            m_CausticsResolution = o.Find(x => x.causticsResolution);
            m_CausticsPlaneBlendDistance = o.Find(x => x.causticsPlaneBlendDistance);

            // Underwater
            m_UnderWater = o.Find(x => x.underWater);
            m_VolumeBounds = o.Find(x => x.volumeBounds);
            m_VolumeDepth = o.Find(x => x.volumeDepth);
            m_VolumeHeight = o.Find(x => x.volumeHeight);
            m_VolumePriority = o.Find(x => x.volumePrority);
            m_AbsorptionDistanceMultiplier = o.Find(x => x.absorptionDistanceMultiplier);
            m_ColorPyramidOffset = o.Find(x => x.colorPyramidOffset);
            m_UnderWaterScatteringColorMode = o.Find(x => x.underWaterScatteringColorMode);
            m_UnderWaterScatteringColor = o.Find(x => x.underWaterScatteringColor);
            m_UnderWaterAmbientProbeContribution = o.Find(x => x.underWaterAmbientProbeContribution);
        }

        // We pass colors to shader via constant buffers instead of Material.SetColor
        // So we have to apply gamma correction ourselves
        static internal void ColorFieldLinear(SerializedProperty property, GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect();
            BeginProperty(rect, label, property);

            BeginChangeCheck();
            var color = ColorField(rect, label, property.colorValue.gamma, true, false, false);
            if (EndChangeCheck())
                property.colorValue = color.linear;

            EndProperty();
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

        internal static Material CreateNewWaterMaterialAndShader(string sceneName, string surfaceName)
        {
            string folderName = "Assets/WaterResources/" + sceneName;
            // Make sure the folder exists
            if (!AssetDatabase.IsValidFolder("Assets/WaterResources"))
                AssetDatabase.CreateFolder("Assets", "WaterResources");
            if (!AssetDatabase.IsValidFolder(folderName))
                AssetDatabase.CreateFolder("Assets/WaterResources", sceneName);

            // Make sure they don't already exist
            var sgPath = folderName + "/" + surfaceName + ".shadergraph";
            // First check if the shader graph or the materials exist if they do we stop right away with a message.
            var sg = AssetDatabase.LoadAssetAtPath<Shader>(sgPath);
            if (sg != null)
            {
                Debug.LogWarning("A water shader or material has already been created in the " + folderName +" folder.");
                return null;
            }

            // Copy the shader graph
            var originalSG = HDRenderPipeline.currentAsset.renderPipelineResources.shaders.waterPS;
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(originalSG), sgPath))
            {
                Debug.LogWarning("Failed to copy the Water Shader Graph at: " + sgPath);
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Material>(sgPath);
        }

        static internal void WaterCustomMaterialField(WaterSurfaceEditor serialized, Editor owner)
        {
            int buttonWidth = 60;
            float indentOffset = EditorGUI.indentLevel * 15f;
            Rect lineRect = EditorGUILayout.GetControlRect();
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset - 3, lineRect.height);
            var fieldRect = new Rect(labelRect.xMax + 5, lineRect.y, lineRect.width - labelRect.width - buttonWidth - 5, lineRect.height);
            var buttonNewRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);

            // Display the label
            EditorGUI.PrefixLabel(labelRect, k_CustomMaterial);

            using (new EditorGUI.PropertyScope(fieldRect, GUIContent.none, serialized.m_CustomMaterial))
            {
                serialized.m_CustomMaterial.objectReferenceValue = (Material)EditorGUI.ObjectField(fieldRect, (Material)serialized.m_CustomMaterial.objectReferenceValue, typeof(Material), false);
            }

            if (GUI.Button(buttonNewRect, k_WaterNewLMaterialLabel, EditorStyles.miniButton))
            {
                WaterSurface ws = (serialized.target as WaterSurface);
                Material newMaterial = CreateNewWaterMaterialAndShader(ws.gameObject.scene.name, ws.name);
                if (newMaterial != null)
                    serialized.m_CustomMaterial.objectReferenceValue = newMaterial;
            }
            EditorGUILayout.Space();
        }

        static internal void WaterSurfaceAppearanceSection(WaterSurfaceEditor serialized, Editor owner)
        {
            // Handle the custom material field
            WaterCustomMaterialField(serialized, owner);

            // Grab the type of the surface
            WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);

            EditorGUILayout.LabelField("Smoothness", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                Vector2 remap = new Vector2(serialized.m_EndSmoothness.floatValue, serialized.m_StartSmoothness.floatValue);
                EditorGUILayout.MinMaxSlider(k_SmoothnessRange, ref remap.x, ref remap.y, 0.0f, 1.0f);
                serialized.m_EndSmoothness.floatValue = remap.x;
                serialized.m_StartSmoothness.floatValue = remap.y;

                // Fade range
                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SmoothnessFadeRange, k_SmoothnessFadeStart, serialized.m_SmoothnessFadeStart, k_SmoothnessFadeDistance, serialized.m_SmoothnessFadeDistance);
            }

            // Refraction section
            EditorGUILayout.LabelField("Refraction", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                ColorFieldLinear(serialized.m_RefractionColor, k_RefractionColor);
                serialized.m_MaxRefractionDistance.floatValue = EditorGUILayout.Slider(k_MaxRefractionDistance, serialized.m_MaxRefractionDistance.floatValue, 0.0f, 3.5f);
                serialized.m_AbsorptionDistance.floatValue = EditorGUILayout.Slider(k_AbsorptionDistance, serialized.m_AbsorptionDistance.floatValue, 0.0f, 100.0f);
            }

            EditorGUILayout.LabelField("Scattering", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                ColorFieldLinear(serialized.m_ScatteringColor, k_ScatteringColor);
                serialized.m_AmbientScattering.floatValue = EditorGUILayout.Slider(k_AmbientScattering, serialized.m_AmbientScattering.floatValue, 0.0f, 1.0f);
                serialized.m_HeightScattering.floatValue = EditorGUILayout.Slider(k_HeightScattering, serialized.m_HeightScattering.floatValue, 0.0f, 1.0f);
                serialized.m_DisplacementScattering.floatValue = EditorGUILayout.Slider(k_DisplacementScattering, serialized.m_DisplacementScattering.floatValue, 0.0f, 1.0f);

                // Given the low amplitude of the pool waves, it doesn't make any sense to have the tip scattering term available to users
                if (surfaceType != WaterSurfaceType.Pool)
                    serialized.m_DirectLightTipScattering.floatValue = EditorGUILayout.Slider(k_DirectLightTipScattering, serialized.m_DirectLightTipScattering.floatValue, 0.0f, 1.0f);
                serialized.m_DirectLightBodyScattering.floatValue = EditorGUILayout.Slider(k_DirectLightBodyScattering, serialized.m_DirectLightBodyScattering.floatValue, 0.0f, 1.0f);

                EditorGUILayout.PropertyField(serialized.m_MaximumHeightOverride);
                serialized.m_MaximumHeightOverride.floatValue = Mathf.Max(serialized.m_MaximumHeightOverride.floatValue, 0.0f);
            }

            // Caustics
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Caustics, k_Caustics);
            if (serialized.m_Caustics.boolValue)
            {
                using (new IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serialized.m_CausticsResolution);
                    int bandCount = HDRenderPipeline.EvaluateBandCount(surfaceType, serialized.m_Ripples.boolValue);

                    if (bandCount != 1)
                    {
                        switch (surfaceType)
                        {
                            case WaterSurfaceType.OceanSeaLake:
                            {
                                serialized.m_CausticsBand.intValue = EditorGUILayout.IntSlider(k_CausticsBandSwell, serialized.m_CausticsBand.intValue, 0, bandCount - 1);
                            }
                            break;
                            case WaterSurfaceType.River:
                                serialized.m_CausticsBand.intValue = EditorGUILayout.IntSlider(k_CausticsBandAgitation, serialized.m_CausticsBand.intValue, 0, bandCount - 1);
                            break;
                            default:
                                break;
                        }
                    }
                    else
                        serialized.m_CausticsBand.intValue = 0;

                    EditorGUILayout.PropertyField(serialized.m_CausticsVirtualPlaneDistance, k_CausticsVirtualPlaneDistance);
                    serialized.m_CausticsVirtualPlaneDistance.floatValue = Mathf.Max(serialized.m_CausticsVirtualPlaneDistance.floatValue, 0.001f);

                    if (WaterSurfaceUI.ShowAdditionalProperties())
                    {
                        EditorGUILayout.PropertyField(serialized.m_CausticsIntensity);
                        serialized.m_CausticsIntensity.floatValue = Mathf.Max(serialized.m_CausticsIntensity.floatValue, 0.0f);

                        EditorGUILayout.PropertyField(serialized.m_CausticsPlaneBlendDistance);
                        serialized.m_CausticsPlaneBlendDistance.floatValue = Mathf.Max(serialized.m_CausticsPlaneBlendDistance.floatValue, 0.0f);
                    }

                    // Display an info box if the wind speed is null for the target band
                    if (!WaterBandHasAgitation(serialized, owner, serialized.m_CausticsBand.intValue))
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
                        serialized.m_VolumeDepth.floatValue = Mathf.Max(serialized.m_VolumeDepth.floatValue, 0.0f);

                        EditorGUILayout.PropertyField(serialized.m_VolumeHeight);
                        serialized.m_VolumeHeight.floatValue = Mathf.Max(serialized.m_VolumeHeight.floatValue, 0.0f);
                    }

                    // Priority
                    EditorGUILayout.PropertyField(serialized.m_VolumePriority);
                    serialized.m_VolumePriority.intValue = serialized.m_VolumePriority.intValue > 0 ? serialized.m_VolumePriority.intValue : 0;

                    // View distance
                    EditorGUILayout.PropertyField(serialized.m_AbsorptionDistanceMultiplier);
                    serialized.m_AbsorptionDistanceMultiplier.floatValue = Mathf.Max(serialized.m_AbsorptionDistanceMultiplier.floatValue, 0.0f);

                    // Color pyramid offset
                    serialized.m_ColorPyramidOffset.intValue = EditorGUILayout.IntSlider(k_ColorPyramidOffset, serialized.m_ColorPyramidOffset.intValue, 0, 4);

                    // Scattering color for underwater
                    EditorGUILayout.PropertyField(serialized.m_UnderWaterScatteringColorMode, k_UnderWaterScatteringColorMode);
                    if ((WaterSurface.UnderWaterScatteringColorMode)serialized.m_UnderWaterScatteringColorMode.enumValueIndex == WaterSurface.UnderWaterScatteringColorMode.Custom)
                    {
                        using (new IndentLevelScope())
                            ColorFieldLinear(serialized.m_UnderWaterScatteringColor, k_UnderWaterScatteringColor);
                    }

                    // Ambient probe contribution
                    serialized.m_UnderWaterAmbientProbeContribution.floatValue = EditorGUILayout.Slider(k_UnderWaterAmbientProbeContribution, serialized.m_UnderWaterAmbientProbeContribution.floatValue, 0.0f, 1.0f);
                }
            }
        }
    }
}
