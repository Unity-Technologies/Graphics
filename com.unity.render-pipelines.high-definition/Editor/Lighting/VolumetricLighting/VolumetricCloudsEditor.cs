using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(VolumetricClouds))]
    class VolumetricCloudsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_EarthCurvature;
        SerializedDataParameter m_CloudTiling;
        SerializedDataParameter m_CloudOffset;
        SerializedDataParameter m_LowestCloudAltitude;
        SerializedDataParameter m_CloudThickness;
        SerializedDataParameter m_NumPrimarySteps;
        SerializedDataParameter m_NumLightSteps;
        SerializedDataParameter m_CloudControl;
        SerializedDataParameter m_CloudPreset;
        SerializedDataParameter m_CloudMap;
        SerializedDataParameter m_CloudLut;
        SerializedDataParameter m_ScatteringDirection;
        SerializedDataParameter m_ScatteringTint;
        SerializedDataParameter m_PowderEffectIntensity;
        SerializedDataParameter m_MultiScattering;
        SerializedDataParameter m_DensityMultiplier;
        SerializedDataParameter m_ShapeFactor;
        SerializedDataParameter m_ErosionFactor;
        SerializedDataParameter m_AmbientLightProbeDimmer;
        SerializedDataParameter m_GlobalWindSpeed;
        SerializedDataParameter m_Orientation;
        SerializedDataParameter m_CloudMapSpeedMultiplier;
        SerializedDataParameter m_ShapeSpeedMultiplier;
        SerializedDataParameter m_ErosionSpeedMultiplier;
        SerializedDataParameter m_TemporalAccumulationFactor;
        SerializedDataParameter m_Shadows;
        SerializedDataParameter m_ShadowResolution;
        SerializedDataParameter m_ShadowDistance;
        SerializedDataParameter m_ShadowPlaneHeightOffset;
        SerializedDataParameter m_ShadowOpacity;
        SerializedDataParameter m_ShadowOpacityFallback;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VolumetricClouds>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_EarthCurvature = Unpack(o.Find(x => x.earthCurvature));
            m_CloudTiling = Unpack(o.Find(x => x.cloudTiling));
            m_CloudOffset = Unpack(o.Find(x => x.cloudOffset));
            m_LowestCloudAltitude = Unpack(o.Find(x => x.lowestCloudAltitude));
            m_CloudThickness = Unpack(o.Find(x => x.cloudThickness));
            m_NumPrimarySteps = Unpack(o.Find(x => x.numPrimarySteps));
            m_NumLightSteps = Unpack(o.Find(x => x.numLightSteps));
            m_CloudControl = Unpack(o.Find(x => x.cloudControl));
            m_CloudPreset = Unpack(o.Find(x => x.cloudPreset));
            m_CloudMap = Unpack(o.Find(x => x.cloudMap));
            m_CloudLut = Unpack(o.Find(x => x.cloudLut));
            m_ScatteringDirection = Unpack(o.Find(x => x.scatteringDirection));
            m_ScatteringTint = Unpack(o.Find(x => x.scatteringTint));
            m_PowderEffectIntensity = Unpack(o.Find(x => x.powderEffectIntensity));
            m_MultiScattering = Unpack(o.Find(x => x.multiScattering));
            m_DensityMultiplier = Unpack(o.Find(x => x.densityMultiplier));
            m_ShapeFactor = Unpack(o.Find(x => x.shapeFactor));
            m_ErosionFactor = Unpack(o.Find(x => x.erosionFactor));
            m_AmbientLightProbeDimmer = Unpack(o.Find(x => x.ambientLightProbeDimmer));
            m_GlobalWindSpeed = Unpack(o.Find(x => x.globalWindSpeed));
            m_Orientation = Unpack(o.Find(x => x.orientation));
            m_CloudMapSpeedMultiplier = Unpack(o.Find(x => x.cloudMapSpeedMultiplier));
            m_ShapeSpeedMultiplier = Unpack(o.Find(x => x.shapeSpeedMultiplier));
            m_ErosionSpeedMultiplier = Unpack(o.Find(x => x.erosionSpeedMultiplier));
            m_TemporalAccumulationFactor = Unpack(o.Find(x => x.temporalAccumulationFactor));
            m_Shadows = Unpack(o.Find(x => x.shadows));
            m_ShadowResolution = Unpack(o.Find(x => x.shadowResolution));
            m_ShadowDistance = Unpack(o.Find(x => x.shadowDistance));
            m_ShadowPlaneHeightOffset = Unpack(o.Find(x => x.shadowPlaneHeightOffset));
            m_ShadowOpacity = Unpack(o.Find(x => x.shadowOpacity));
            m_ShadowOpacityFallback = Unpack(o.Find(x => x.shadowOpacityFallback));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Volumetric Clouds are only displayed up to the far plane of the used camera. Make sure to increase the far plane accordingly.", MessageType.Info);

            EditorGUILayout.LabelField("General", EditorStyles.miniLabel);
            PropertyField(m_Enable);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Shape", EditorStyles.miniLabel);
            PropertyField(m_CloudControl);
            VolumetricClouds.CloudControl controlMode = (VolumetricClouds.CloudControl)m_CloudControl.value.enumValueIndex;
            using (new HDEditorUtils.IndentScope())
            {
                bool needsIntendation = false;
                if (controlMode == VolumetricClouds.CloudControl.Advanced)
                {
                    PropertyField(m_CloudMap);
                    PropertyField(m_CloudTiling);
                    PropertyField(m_CloudOffset);
                }
                else if (controlMode == VolumetricClouds.CloudControl.Manual)
                {
                    PropertyField(m_CloudMap);
                    PropertyField(m_CloudLut);
                    PropertyField(m_CloudTiling);
                    PropertyField(m_CloudOffset);
                }
                else
                {
                    using (new HDEditorUtils.IndentScope())
                    {
                        needsIntendation = true;
                        PropertyField(m_CloudPreset);
                    }
                }

                VolumetricClouds.CloudPresets controlPreset = (VolumetricClouds.CloudPresets)m_CloudPreset.value.enumValueIndex;
                if ((controlMode != VolumetricClouds.CloudControl.Simple) || controlMode == VolumetricClouds.CloudControl.Simple && controlPreset == VolumetricClouds.CloudPresets.Custom)
                {
                    using (new HDEditorUtils.IndentScope(needsIntendation ? 16 : 0))
                    {
                        PropertyField(m_DensityMultiplier);
                        PropertyField(m_ShapeFactor);
                        PropertyField(m_ErosionFactor);
                    }
                }
            }

            PropertyField(m_EarthCurvature);
            PropertyField(m_LowestCloudAltitude);
            PropertyField(m_CloudThickness);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Wind", EditorStyles.miniLabel);
            PropertyField(m_GlobalWindSpeed);
            if (isInAdvancedMode)
            {
                using (new HDEditorUtils.IndentScope())
                {
                    PropertyField(m_Orientation);
                    PropertyField(m_CloudMapSpeedMultiplier);
                    PropertyField(m_ShapeSpeedMultiplier);
                    PropertyField(m_ErosionSpeedMultiplier);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quality", EditorStyles.miniLabel);
            {
                PropertyField(m_TemporalAccumulationFactor);
                PropertyField(m_NumPrimarySteps);
                PropertyField(m_NumLightSteps);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lighting", EditorStyles.miniLabel);
            {
                PropertyField(m_AmbientLightProbeDimmer);
                PropertyField(m_ScatteringDirection);
                PropertyField(m_ScatteringTint);
                PropertyField(m_PowderEffectIntensity);
                PropertyField(m_MultiScattering);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shadows", EditorStyles.miniLabel);
            {
                PropertyField(m_Shadows);
                using (new HDEditorUtils.IndentScope())
                {
                    PropertyField(m_ShadowResolution);
                    if (isInAdvancedMode)
                    {
                        PropertyField(m_ShadowOpacity);
                        PropertyField(m_ShadowDistance);
                        PropertyField(m_ShadowPlaneHeightOffset);
                        PropertyField(m_ShadowOpacityFallback);
                    }
                }
            }
        }
    }
}
