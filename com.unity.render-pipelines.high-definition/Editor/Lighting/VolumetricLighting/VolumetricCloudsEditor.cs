using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(VolumetricClouds))]
    class VolumetricCloudsEditor : VolumeComponentEditor
    {
        // General
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_LocalClouds;

        // Shape
        SerializedDataParameter m_CloudControl;

        SerializedDataParameter m_CloudPreset;
        SerializedDataParameter m_CustomDensityCurve;
        SerializedDataParameter m_CustomErosionCurve;
        SerializedDataParameter m_CustomAmbientOcclusionCurve;

        SerializedDataParameter m_CumulusMap;
        SerializedDataParameter m_CumulusMapMultiplier;
        SerializedDataParameter m_AltoStratusMap;
        SerializedDataParameter m_AltoStratusMapMultiplier;
        SerializedDataParameter m_CumulonimbusMap;
        SerializedDataParameter m_CumulonimbusMapMultiplier;
        SerializedDataParameter m_RainMap;
        SerializedDataParameter m_CloudMapResolution;

        SerializedDataParameter m_CloudMap;
        SerializedDataParameter m_CloudLut;

        SerializedDataParameter m_EarthCurvature;
        SerializedDataParameter m_CloudTiling;
        SerializedDataParameter m_CloudOffset;

        SerializedDataParameter m_LowestCloudAltitude;
        SerializedDataParameter m_CloudThickness;
        SerializedDataParameter m_FadeInMode;
        SerializedDataParameter m_FadeInStart;
        SerializedDataParameter m_FadeInDistance;

        SerializedDataParameter m_DensityMultiplier;
        SerializedDataParameter m_ShapeFactor;
        SerializedDataParameter m_ShapeScale;
        SerializedDataParameter m_ShapeOffset;
        SerializedDataParameter m_ErosionFactor;
        SerializedDataParameter m_ErosionScale;
        SerializedDataParameter m_ErosionNoiseType;

        // Lighting
        SerializedDataParameter m_ScatteringTint;
        SerializedDataParameter m_PowderEffectIntensity;
        SerializedDataParameter m_MultiScattering;
        SerializedDataParameter m_AmbientLightProbeDimmer;
        SerializedDataParameter m_SunLightDimmer;
        SerializedDataParameter m_ErosionOcclusion;

        // Wind
        SerializedDataParameter m_GlobalWindSpeed;
        SerializedDataParameter m_Orientation;
        SerializedDataParameter m_CloudMapSpeedMultiplier;
        SerializedDataParameter m_ShapeSpeedMultiplier;
        SerializedDataParameter m_ErosionSpeedMultiplier;
        SerializedDataParameter m_VerticalShapeWindSpeed;
        SerializedDataParameter m_VerticalErosionWindSpeed;
        SerializedDataParameter m_AltitudeDistortion;

        // Quality
        SerializedDataParameter m_TemporalAccumulationFactor;
        SerializedDataParameter m_GhostingReduction;
        SerializedDataParameter m_NumPrimarySteps;
        SerializedDataParameter m_NumLightSteps;

        // Shadows
        SerializedDataParameter m_Shadows;
        SerializedDataParameter m_ShadowResolution;
        SerializedDataParameter m_ShadowDistance;
        SerializedDataParameter m_ShadowPlaneHeightOffset;
        SerializedDataParameter m_ShadowOpacity;
        SerializedDataParameter m_ShadowOpacityFallback;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VolumetricClouds>(serializedObject);

            // General
            m_Enable = Unpack(o.Find(x => x.enable));
            m_LocalClouds = Unpack(o.Find(x => x.localClouds));

            // Shape
            m_CloudControl = Unpack(o.Find(x => x.cloudControl));

            m_CloudPreset = Unpack(o.Find(x => x.cloudPreset));
            m_CustomDensityCurve = Unpack(o.Find(x => x.customDensityCurve));
            m_CustomErosionCurve = Unpack(o.Find(x => x.customErosionCurve));
            m_CustomAmbientOcclusionCurve = Unpack(o.Find(x => x.customAmbientOcclusionCurve));

            m_CumulusMap = Unpack(o.Find(x => x.cumulusMap));
            m_CumulusMapMultiplier = Unpack(o.Find(x => x.cumulusMapMultiplier));
            m_AltoStratusMap = Unpack(o.Find(x => x.altoStratusMap));
            m_AltoStratusMapMultiplier = Unpack(o.Find(x => x.altoStratusMapMultiplier));
            m_CumulonimbusMap = Unpack(o.Find(x => x.cumulonimbusMap));
            m_CumulonimbusMapMultiplier = Unpack(o.Find(x => x.cumulonimbusMapMultiplier));
            m_RainMap = Unpack(o.Find(x => x.rainMap));
            m_CloudMapResolution = Unpack(o.Find(x => x.cloudMapResolution));

            m_CloudMap = Unpack(o.Find(x => x.cloudMap));
            m_CloudLut = Unpack(o.Find(x => x.cloudLut));

            m_EarthCurvature = Unpack(o.Find(x => x.earthCurvature));
            m_CloudTiling = Unpack(o.Find(x => x.cloudTiling));
            m_CloudOffset = Unpack(o.Find(x => x.cloudOffset));

            m_LowestCloudAltitude = Unpack(o.Find(x => x.lowestCloudAltitude));
            m_CloudThickness = Unpack(o.Find(x => x.cloudThickness));

            m_FadeInMode = Unpack(o.Find(x => x.fadeInMode));
            m_FadeInStart = Unpack(o.Find(x => x.fadeInStart));
            m_FadeInDistance = Unpack(o.Find(x => x.fadeInDistance));

            m_DensityMultiplier = Unpack(o.Find(x => x.densityMultiplier));
            m_ShapeFactor = Unpack(o.Find(x => x.shapeFactor));
            m_ShapeScale = Unpack(o.Find(x => x.shapeScale));
            m_ShapeOffset = Unpack(o.Find(x => x.shapeOffset));
            m_ErosionFactor = Unpack(o.Find(x => x.erosionFactor));
            m_ErosionScale = Unpack(o.Find(x => x.erosionScale));
            m_ErosionNoiseType = Unpack(o.Find(x => x.erosionNoiseType));

            // Lighting
            m_ScatteringTint = Unpack(o.Find(x => x.scatteringTint));
            m_PowderEffectIntensity = Unpack(o.Find(x => x.powderEffectIntensity));
            m_MultiScattering = Unpack(o.Find(x => x.multiScattering));
            m_AmbientLightProbeDimmer = Unpack(o.Find(x => x.ambientLightProbeDimmer));
            m_SunLightDimmer = Unpack(o.Find(x => x.sunLightDimmer));
            m_ErosionOcclusion = Unpack(o.Find(x => x.erosionOcclusion));

            // Wind
            m_GlobalWindSpeed = Unpack(o.Find(x => x.globalWindSpeed));
            m_Orientation = Unpack(o.Find(x => x.orientation));
            m_CloudMapSpeedMultiplier = Unpack(o.Find(x => x.cloudMapSpeedMultiplier));
            m_ShapeSpeedMultiplier = Unpack(o.Find(x => x.shapeSpeedMultiplier));
            m_ErosionSpeedMultiplier = Unpack(o.Find(x => x.erosionSpeedMultiplier));
            m_VerticalShapeWindSpeed = Unpack(o.Find(x => x.verticalShapeWindSpeed));
            m_VerticalErosionWindSpeed = Unpack(o.Find(x => x.verticalErosionWindSpeed));
            m_AltitudeDistortion = Unpack(o.Find(x => x.altitudeDistortion));

            // Quality
            m_TemporalAccumulationFactor = Unpack(o.Find(x => x.temporalAccumulationFactor));
            m_GhostingReduction = Unpack(o.Find(x => x.ghostingReduction));
            m_NumPrimarySteps = Unpack(o.Find(x => x.numPrimarySteps));
            m_NumLightSteps = Unpack(o.Find(x => x.numLightSteps));

            // Shadows
            m_Shadows = Unpack(o.Find(x => x.shadows));
            m_ShadowResolution = Unpack(o.Find(x => x.shadowResolution));
            m_ShadowDistance = Unpack(o.Find(x => x.shadowDistance));
            m_ShadowPlaneHeightOffset = Unpack(o.Find(x => x.shadowPlaneHeightOffset));
            m_ShadowOpacity = Unpack(o.Find(x => x.shadowOpacity));
            m_ShadowOpacityFallback = Unpack(o.Find(x => x.shadowOpacityFallback));
        }

        static public readonly GUIContent k_CloudMapTilingText = EditorGUIUtility.TrTextContent("Cloud Map Tiling", "Tiling (x,y) of the cloud map.");
        static public readonly GUIContent k_CloudMapOffsetText = EditorGUIUtility.TrTextContent("Cloud Map Offset", "Offset (x,y) of the cloud map.");
        static public readonly GUIContent k_GlobalHorizontalWindSpeedText = EditorGUIUtility.TrTextContent("Global Horizontal Wind Speed", "Sets the global horizontal wind speed in kilometers per hour.\nThis value can be relative to the Global Wind Speed defined in the Visual Environment.");

        public override void OnInspectorGUI()
        {
            // This whole editor has nothing to display if the SSR feature is not supported
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportVolumetricClouds ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Volumetric Clouds.", MessageType.Error, wide: true);
                return;
            }


            EditorGUILayout.LabelField("General", EditorStyles.miniLabel);
            PropertyField(m_Enable);
            PropertyField(m_LocalClouds);
            if (m_LocalClouds.value.boolValue)
                EditorGUILayout.HelpBox("Volumetric Clouds are only displayed up to the far plane of the used camera. Make sure to increase the far and near planes accordingly.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Shape", EditorStyles.miniLabel);
            PropertyField(m_CloudControl);
            VolumetricClouds.CloudControl controlMode = (VolumetricClouds.CloudControl)m_CloudControl.value.enumValueIndex;
            bool hasCloudMap = true;
            using (new IndentLevelScope())
            {
                bool needsIntendation = false;
                if (controlMode == VolumetricClouds.CloudControl.Advanced)
                {
                    PropertyField(m_CumulusMap);
                    PropertyField(m_CumulusMapMultiplier);
                    PropertyField(m_AltoStratusMap);
                    PropertyField(m_AltoStratusMapMultiplier);
                    PropertyField(m_CumulonimbusMap);
                    PropertyField(m_CumulonimbusMapMultiplier);
                    PropertyField(m_RainMap);
                    PropertyField(m_CloudMapResolution);
                    PropertyField(m_CloudTiling, k_CloudMapTilingText);
                    PropertyField(m_CloudOffset, k_CloudMapOffsetText);
                }
                else if (controlMode == VolumetricClouds.CloudControl.Manual)
                {
                    PropertyField(m_CloudMap);
                    PropertyField(m_CloudLut);
                    PropertyField(m_CloudTiling, k_CloudMapTilingText);
                    PropertyField(m_CloudOffset, k_CloudMapOffsetText);
                }
                else
                {
                    hasCloudMap = false;
                    needsIntendation = true;
                    PropertyField(m_CloudPreset);
                }

                VolumetricClouds.CloudPresets controlPreset = (VolumetricClouds.CloudPresets)m_CloudPreset.value.enumValueIndex;
                if ((controlMode != VolumetricClouds.CloudControl.Simple) || controlMode == VolumetricClouds.CloudControl.Simple && controlPreset == VolumetricClouds.CloudPresets.Custom)
                {
                    using (new IndentLevelScope(needsIntendation ? 16 : 0))
                    {
                        PropertyField(m_DensityMultiplier);
                        if (controlMode == VolumetricClouds.CloudControl.Simple)
                        {
                            PropertyField(m_CustomDensityCurve);
                        }
                        PropertyField(m_ShapeFactor);
                        PropertyField(m_ShapeScale);
                        PropertyField(m_ShapeOffset);
                        PropertyField(m_ErosionFactor);
                        PropertyField(m_ErosionScale);
                        PropertyField(m_ErosionNoiseType);
                        if (controlMode == VolumetricClouds.CloudControl.Simple)
                        {
                            PropertyField(m_CustomErosionCurve);
                            PropertyField(m_CustomAmbientOcclusionCurve);
                        }
                    }
                }
                else
                    PropertyField(m_ShapeOffset);
            }

            PropertyField(m_EarthCurvature);
            PropertyField(m_LowestCloudAltitude);
            PropertyField(m_CloudThickness);

            DrawHeader("Wind");
            PropertyField(m_GlobalWindSpeed, k_GlobalHorizontalWindSpeedText);
            if (showAdditionalProperties)
            {
                using (new IndentLevelScope())
                {
                    if (hasCloudMap)
                        PropertyField(m_CloudMapSpeedMultiplier);
                    PropertyField(m_ShapeSpeedMultiplier);
                    PropertyField(m_ErosionSpeedMultiplier);
                }
            }
            PropertyField(m_Orientation);
            using (new IndentLevelScope())
            {
                PropertyField(m_AltitudeDistortion);
            }

            PropertyField(m_VerticalShapeWindSpeed);
            PropertyField(m_VerticalErosionWindSpeed);

            DrawHeader("Lighting");
            {
                PropertyField(m_AmbientLightProbeDimmer);
                PropertyField(m_SunLightDimmer);
                PropertyField(m_ErosionOcclusion);
                PropertyField(m_ScatteringTint);
                PropertyField(m_PowderEffectIntensity);
                PropertyField(m_MultiScattering);
            }

            DrawHeader("Shadows");
            {
                PropertyField(m_Shadows);
                using (new IndentLevelScope())
                {
                    PropertyField(m_ShadowResolution);
                    PropertyField(m_ShadowOpacity);
                    PropertyField(m_ShadowDistance);
                    PropertyField(m_ShadowPlaneHeightOffset);
                    PropertyField(m_ShadowOpacityFallback);
                }
            }

            DrawHeader("Quality");
            {
                PropertyField(m_TemporalAccumulationFactor);
                PropertyField(m_GhostingReduction);
                PropertyField(m_NumPrimarySteps);
                PropertyField(m_NumLightSteps);
                PropertyField(m_FadeInMode);
                using (new IndentLevelScope())
                {
                    if ((VolumetricClouds.CloudFadeInMode)m_FadeInMode.value.enumValueIndex == (VolumetricClouds.CloudFadeInMode.Manual))
                    {
                        PropertyField(m_FadeInStart);
                        PropertyField(m_FadeInDistance);
                    }
                }
            }
        }
    }
}
