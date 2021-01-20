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
        SerializedDataParameter m_EarthRadiusMultiplier;
        SerializedDataParameter m_CloudTiling;
        SerializedDataParameter m_LowestCloudAltitude;
        SerializedDataParameter m_HighestCloudAltitude;
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
        SerializedDataParameter m_WindRotation;
        SerializedDataParameter m_CloudMapWindSpeedMultiplier;
        SerializedDataParameter m_ShapeWindSpeedMultiplier;
        SerializedDataParameter m_ErosionWindSpeedMultiplier;
        SerializedDataParameter m_TemporalAccumulationFactor;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VolumetricClouds>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_EarthRadiusMultiplier = Unpack(o.Find(x => x.earthRadiusMultiplier));
            m_CloudTiling = Unpack(o.Find(x => x.cloudTiling));
            m_LowestCloudAltitude = Unpack(o.Find(x => x.lowestCloudAltitude));
            m_HighestCloudAltitude = Unpack(o.Find(x => x.highestCloudAltitude));
            m_NumPrimarySteps = Unpack(o.Find(x => x.numPrimarySteps));
            m_NumLightSteps = Unpack(o.Find(x => x.numLightSteps));
            m_CloudControl = Unpack(o.Find(x => x.cloudControl));
            m_CloudPreset = Unpack(o.Find(x => x.cloudPresets));
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
            m_WindRotation = Unpack(o.Find(x => x.windRotation));
            m_CloudMapWindSpeedMultiplier = Unpack(o.Find(x => x.cloudMapWindSpeedMultiplier));
            m_ShapeWindSpeedMultiplier = Unpack(o.Find(x => x.shapeWindSpeedMultiplier));
            m_ErosionWindSpeedMultiplier = Unpack(o.Find(x => x.erosionWindSpeedMultiplier));
            m_TemporalAccumulationFactor = Unpack(o.Find(x => x.temporalAccumulationFactor));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("General", EditorStyles.miniLabel);
            PropertyField(m_Enable);

            EditorGUILayout.LabelField("Accumulation", EditorStyles.miniLabel);
            {
                PropertyField(m_TemporalAccumulationFactor);
            }

            EditorGUILayout.LabelField("Shape", EditorStyles.miniLabel);
            PropertyField(m_EarthRadiusMultiplier);
            if (isInAdvancedMode)
            {
                using (new HDEditorUtils.IndentScope())
                {
                    PropertyField(m_CloudTiling);
                }
            }
            PropertyField(m_LowestCloudAltitude);
            PropertyField(m_HighestCloudAltitude);

            EditorGUILayout.LabelField("Wind", EditorStyles.miniLabel);
            PropertyField(m_GlobalWindSpeed);
            if (isInAdvancedMode)
            {
                using (new HDEditorUtils.IndentScope())
                {
                    PropertyField(m_WindRotation);
                    PropertyField(m_CloudMapWindSpeedMultiplier);
                    PropertyField(m_ShapeWindSpeedMultiplier);
                    PropertyField(m_ErosionWindSpeedMultiplier);
                }
            }

            EditorGUILayout.LabelField("Density", EditorStyles.miniLabel);
            PropertyField(m_CloudControl);

            using (new HDEditorUtils.IndentScope())
            {
                VolumetricClouds.CloudControl controlMode = (VolumetricClouds.CloudControl)m_CloudControl.value.enumValueIndex;

                bool needsIntendation = false;
                if (controlMode == VolumetricClouds.CloudControl.Custom)
                {
                    PropertyField(m_CloudMap);
                }
                else if (controlMode == VolumetricClouds.CloudControl.Manual)
                {
                    PropertyField(m_CloudMap);
                    PropertyField(m_CloudLut);
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
                if ((controlMode != VolumetricClouds.CloudControl.Simple) || controlMode == VolumetricClouds.CloudControl.Simple && controlPreset == VolumetricClouds.CloudPresets.Manual)
                {
                    using (new HDEditorUtils.IndentScope(needsIntendation ? 16 : 0))
                    {
                        PropertyField(m_DensityMultiplier);

                        if (isInAdvancedMode)
                        {
                            using (new HDEditorUtils.IndentScope())
                            {
                                PropertyField(m_ShapeFactor);
                                PropertyField(m_ErosionFactor);
                            }
                        }
                    }
                }
            }

            if (isInAdvancedMode)
            {
                PropertyField(m_AmbientLightProbeDimmer);
                EditorGUILayout.LabelField("Quality", EditorStyles.miniLabel);
                {
                    PropertyField(m_NumPrimarySteps);
                    PropertyField(m_NumLightSteps);
                }

                EditorGUILayout.LabelField("Lighting", EditorStyles.miniLabel);
                {
                    PropertyField(m_ScatteringDirection);
                    PropertyField(m_ScatteringTint);
                    PropertyField(m_PowderEffectIntensity);
                    PropertyField(m_MultiScattering);
                }
            }
        }
    }
}
