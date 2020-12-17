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
        SerializedDataParameter m_CloudDomeSize;
        SerializedDataParameter m_CloudTiling;
        SerializedDataParameter m_LowestCloudAltitude;
        SerializedDataParameter m_HighestCloudAltitude;
        SerializedDataParameter m_NumPrimarySteps;
        SerializedDataParameter m_NumLightSteps;
        SerializedDataParameter m_CloudMap;
        SerializedDataParameter m_CloudLut;
        SerializedDataParameter m_ScatteringDirection;
        SerializedDataParameter m_PowderEffectIntensity;
        SerializedDataParameter m_MultiScattering;
        SerializedDataParameter m_DensityMultiplier;
        SerializedDataParameter m_DensityAmplifier;
        SerializedDataParameter m_ErosionFactor;
        SerializedDataParameter m_AmbientLightProbeDimmer;
        SerializedDataParameter m_GlobalWindSpeed;
        SerializedDataParameter m_WindRotation;
        SerializedDataParameter m_LargeCloudsWindSpeed;
        SerializedDataParameter m_MediumCloudsWindSpeed;
        SerializedDataParameter m_SmallCloudsWindSpeed;
        SerializedDataParameter m_TemporalAccumulationFactor;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VolumetricClouds>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_CloudDomeSize = Unpack(o.Find(x => x.cloudDomeSize));
            m_CloudTiling = Unpack(o.Find(x => x.cloudTiling));
            m_LowestCloudAltitude = Unpack(o.Find(x => x.lowestCloudAltitude));
            m_HighestCloudAltitude = Unpack(o.Find(x => x.highestCloudAltitude));
            m_NumPrimarySteps = Unpack(o.Find(x => x.numPrimarySteps));
            m_NumLightSteps = Unpack(o.Find(x => x.numLightSteps));
            m_CloudMap = Unpack(o.Find(x => x.cloudMap));
            m_CloudLut = Unpack(o.Find(x => x.cloudLut));
            m_ScatteringDirection = Unpack(o.Find(x => x.scatteringDirection));
            m_PowderEffectIntensity = Unpack(o.Find(x => x.powderEffectIntensity));
            m_MultiScattering = Unpack(o.Find(x => x.multiScattering));
            m_DensityMultiplier = Unpack(o.Find(x => x.densityMultiplier));
            m_DensityAmplifier = Unpack(o.Find(x => x.densityAmplifier));
            m_ErosionFactor = Unpack(o.Find(x => x.erosionFactor));
            m_AmbientLightProbeDimmer = Unpack(o.Find(x => x.ambientLightProbeDimmer));
            m_GlobalWindSpeed = Unpack(o.Find(x => x.globalWindSpeed));
            m_WindRotation = Unpack(o.Find(x => x.windRotation));
            m_LargeCloudsWindSpeed = Unpack(o.Find(x => x.largeCloudsWindSpeed));
            m_MediumCloudsWindSpeed = Unpack(o.Find(x => x.mediumCloudsWindSpeed));
            m_SmallCloudsWindSpeed = Unpack(o.Find(x => x.smallCloudsWindSpeed));
            m_TemporalAccumulationFactor = Unpack(o.Find(x => x.temporalAccumulationFactor));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("General", EditorStyles.miniLabel);
            PropertyField(m_Enable);

            EditorGUILayout.LabelField("Shape", EditorStyles.miniLabel);
            PropertyField(m_CloudDomeSize);
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
                    PropertyField(m_LargeCloudsWindSpeed);
                    PropertyField(m_MediumCloudsWindSpeed);
                    PropertyField(m_SmallCloudsWindSpeed);
                }
            }

            EditorGUILayout.LabelField("Density", EditorStyles.miniLabel);
            PropertyField(m_CloudMap);
            PropertyField(m_CloudLut);
            PropertyField(m_DensityMultiplier);
            if (isInAdvancedMode)
            {
                using (new HDEditorUtils.IndentScope())
                {
                    PropertyField(m_DensityAmplifier);
                    PropertyField(m_ErosionFactor);
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
                    PropertyField(m_PowderEffectIntensity);
                    PropertyField(m_MultiScattering);
                }

                EditorGUILayout.LabelField("Misc", EditorStyles.miniLabel);
                {
                    PropertyField(m_TemporalAccumulationFactor);
                }
            }
        }
    }
}
