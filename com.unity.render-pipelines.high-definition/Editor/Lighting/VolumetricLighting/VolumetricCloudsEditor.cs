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
        SerializedDataParameter m_LowestCloudAltitude;
        SerializedDataParameter m_HighestCloudAltitude;
        SerializedDataParameter m_NumPrimarySteps;
        SerializedDataParameter m_NumLightSteps;
        SerializedDataParameter m_CloudMap;
        SerializedDataParameter m_CloudLut;
        SerializedDataParameter m_ScatteringTint;
        SerializedDataParameter m_EccentricityF;
        SerializedDataParameter m_EccentricityB;
        SerializedDataParameter m_PhaseFunctionBlend;
        SerializedDataParameter m_PowderEffectIntensity;
        SerializedDataParameter m_MultiScattering;
        SerializedDataParameter m_DensityMultiplier;
        SerializedDataParameter m_GlobalLightProbeDimmer;
        SerializedDataParameter m_WindRotation;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VolumetricClouds>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_CloudDomeSize = Unpack(o.Find(x => x.cloudDomeSize));
            m_LowestCloudAltitude = Unpack(o.Find(x => x.lowestCloudAltitude));
            m_HighestCloudAltitude = Unpack(o.Find(x => x.highestCloudAltitude));
            m_NumPrimarySteps = Unpack(o.Find(x => x.numPrimarySteps));
            m_NumLightSteps = Unpack(o.Find(x => x.numLightSteps));
            m_CloudMap = Unpack(o.Find(x => x.cloudMap));
            m_CloudLut = Unpack(o.Find(x => x.cloudLut));
            m_ScatteringTint = Unpack(o.Find(x => x.scatteringTint));
            m_EccentricityF = Unpack(o.Find(x => x.eccentricityF));
            m_EccentricityB = Unpack(o.Find(x => x.eccentricityB));
            m_PhaseFunctionBlend = Unpack(o.Find(x => x.phaseFunctionBlend));
            m_PowderEffectIntensity = Unpack(o.Find(x => x.powderEffectIntensity));
            m_MultiScattering = Unpack(o.Find(x => x.multiScattering));
            m_DensityMultiplier = Unpack(o.Find(x => x.densityMultiplier));
            m_GlobalLightProbeDimmer = Unpack(o.Find(x => x.globalLightProbeDimmer));
            m_WindRotation = Unpack(o.Find(x => x.windRotation));
        }
        

        public override void OnInspectorGUI()
        {

            PropertyField(m_Enable);
            PropertyField(m_CloudDomeSize);
            PropertyField(m_LowestCloudAltitude);
            PropertyField(m_HighestCloudAltitude);
            PropertyField(m_NumPrimarySteps);
            PropertyField(m_NumLightSteps);
            PropertyField(m_CloudMap);
            PropertyField(m_CloudLut);
            PropertyField(m_ScatteringTint);
            PropertyField(m_EccentricityF);
            PropertyField(m_EccentricityB);
            PropertyField(m_PhaseFunctionBlend);
            PropertyField(m_PowderEffectIntensity);
            PropertyField(m_MultiScattering);
            PropertyField(m_DensityMultiplier);
            PropertyField(m_GlobalLightProbeDimmer);
            PropertyField(m_WindRotation);
        }
    }
}
