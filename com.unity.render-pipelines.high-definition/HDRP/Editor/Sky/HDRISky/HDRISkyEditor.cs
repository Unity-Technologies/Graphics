using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDRISky))]
    public class HDRISkyEditor
        : SkySettingsEditor
    {
        SerializedDataParameter m_hdriSky;
        SerializedDataParameter m_Lux;
        SerializedDataParameter m_IntensityMode;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<HDRISky>(serializedObject);
            m_hdriSky = Unpack(o.Find(x => x.hdriSky));
            m_Lux = Unpack(o.Find(x => x.lux));
            m_IntensityMode = Unpack(o.Find(x => x.skyIntensityMode));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_hdriSky);

            EditorGUILayout.Space();
            
            PropertyField(m_IntensityMode);

            if (m_IntensityMode.value.enumValueIndex == (int)SkyIntensityMode.Lux)
            {
                // Hide EV and Multiplier fields
                m_ShowProperties.value.intValue &= ~(int)SkySettingsPropertyFlags.ShowMultiplierAndEV;
                PropertyField(m_Lux);
            }
            else
            {
                m_ShowProperties.value.intValue |= (int)SkySettingsPropertyFlags.ShowMultiplierAndEV;
            }

            base.CommonSkySettingsGUI();
        }
    }
}