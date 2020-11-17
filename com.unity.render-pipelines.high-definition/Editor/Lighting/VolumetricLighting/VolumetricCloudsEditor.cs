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
        SerializedDataParameter m_Eccentricity;
        SerializedDataParameter m_SilverIntensity;
        SerializedDataParameter m_SilverSpread;
        SerializedDataParameter m_GlobalLightProbeDimmer;

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
            m_Eccentricity = Unpack(o.Find(x => x.eccentricity));
            m_SilverIntensity = Unpack(o.Find(x => x.silverIntensity));
            m_SilverSpread = Unpack(o.Find(x => x.silverSpread));
            m_GlobalLightProbeDimmer = Unpack(o.Find(x => x.globalLightProbeDimmer));
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
            PropertyField(m_Eccentricity);
            PropertyField(m_SilverIntensity);
            PropertyField(m_SilverSpread);
            PropertyField(m_GlobalLightProbeDimmer);
        }
    }
}
