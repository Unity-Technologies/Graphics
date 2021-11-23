using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(DepthOfField))]
    sealed class DepthOfFieldEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;

        SerializedDataParameter m_GaussianStart;
        SerializedDataParameter m_GaussianEnd;
        SerializedDataParameter m_GaussianMaxRadius;
        SerializedDataParameter m_HighQualitySampling;

        SerializedDataParameter m_FocusDistance;
        SerializedDataParameter m_FocalLength;
        SerializedDataParameter m_Aperture;
        SerializedDataParameter m_BladeCount;
        SerializedDataParameter m_BladeCurvature;
        SerializedDataParameter m_BladeRotation;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DepthOfField>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
            m_GaussianStart = Unpack(o.Find(x => x.gaussianStart));
            m_GaussianEnd = Unpack(o.Find(x => x.gaussianEnd));
            m_GaussianMaxRadius = Unpack(o.Find(x => x.gaussianMaxRadius));
            m_HighQualitySampling = Unpack(o.Find(x => x.highQualitySampling));

            m_FocusDistance = Unpack(o.Find(x => x.focusDistance));
            m_FocalLength = Unpack(o.Find(x => x.focalLength));
            m_Aperture = Unpack(o.Find(x => x.aperture));
            m_BladeCount = Unpack(o.Find(x => x.bladeCount));
            m_BladeCurvature = Unpack(o.Find(x => x.bladeCurvature));
            m_BladeRotation = Unpack(o.Find(x => x.bladeRotation));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);

            if (m_Mode.value.intValue == (int)DepthOfFieldMode.Gaussian)
            {
                PropertyField(m_GaussianStart, EditorGUIUtility.TrTextContent("Start"));
                PropertyField(m_GaussianEnd, EditorGUIUtility.TrTextContent("End"));
                PropertyField(m_GaussianMaxRadius, EditorGUIUtility.TrTextContent("Max Radius"));
                PropertyField(m_HighQualitySampling);
            }
            else if (m_Mode.value.intValue == (int)DepthOfFieldMode.Bokeh)
            {
                PropertyField(m_FocusDistance);
                PropertyField(m_FocalLength);
                PropertyField(m_Aperture);
                PropertyField(m_BladeCount);
                PropertyField(m_BladeCurvature);
                PropertyField(m_BladeRotation);
            }
        }
    }
}
