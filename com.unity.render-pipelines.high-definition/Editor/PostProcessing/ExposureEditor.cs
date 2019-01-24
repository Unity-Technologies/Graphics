using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(Exposure))]
    sealed class ExposureEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_MeteringMode;
        SerializedDataParameter m_LuminanceSource;
        
        SerializedDataParameter m_FixedExposure;
        SerializedDataParameter m_Compensation;
        SerializedDataParameter m_LimitMin;
        SerializedDataParameter m_LimitMax;
        SerializedDataParameter m_CurveMap;
        
        SerializedDataParameter m_AdaptationMode;
        SerializedDataParameter m_AdaptationSpeedDarkToLight;
        SerializedDataParameter m_AdaptationSpeedLightToDark;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Exposure>(serializedObject);
            
            m_Mode = Unpack(o.Find(x => x.mode));
            m_MeteringMode = Unpack(o.Find(x => x.meteringMode));
            m_LuminanceSource = Unpack(o.Find(x => x.luminanceSource));
            
            m_FixedExposure = Unpack(o.Find(x => x.fixedExposure));
            m_Compensation = Unpack(o.Find(x => x.compensation));
            m_LimitMin = Unpack(o.Find(x => x.limitMin));
            m_LimitMax = Unpack(o.Find(x => x.limitMax));
            m_CurveMap = Unpack(o.Find(x => x.curveMap));
            
            m_AdaptationMode = Unpack(o.Find(x => x.adaptationMode));
            m_AdaptationSpeedDarkToLight = Unpack(o.Find(x => x.adaptationSpeedDarkToLight));
            m_AdaptationSpeedLightToDark = Unpack(o.Find(x => x.adaptationSpeedLightToDark));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);

            int mode = m_Mode.value.intValue;
            if (mode == (int)ExposureMode.UsePhysicalCamera)
            {
                PropertyField(m_Compensation);
            }
            else if (mode == (int)ExposureMode.Fixed)
            {
                PropertyField(m_FixedExposure);
            }
            else
            {
                EditorGUILayout.Space();

                PropertyField(m_MeteringMode);
                PropertyField(m_LuminanceSource);

                if (m_LuminanceSource.value.intValue == (int)LuminanceSource.LightingBuffer)
                    EditorGUILayout.HelpBox("Luminance source buffer isn't supported yet.", MessageType.Warning);

                if (mode == (int)ExposureMode.CurveMapping)
                    PropertyField(m_CurveMap);
                
                PropertyField(m_Compensation);
                PropertyField(m_LimitMin);
                PropertyField(m_LimitMax);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Adaptation", EditorStyles.miniLabel);

                PropertyField(m_AdaptationMode, EditorGUIUtility.TrTextContent("Mode"));

                if (m_AdaptationMode.value.intValue == (int)AdaptationMode.Progressive)
                {
                    PropertyField(m_AdaptationSpeedDarkToLight, EditorGUIUtility.TrTextContent("Speed Dark to Light"));
                    PropertyField(m_AdaptationSpeedLightToDark, EditorGUIUtility.TrTextContent("Speed Light to Dark"));
                }
            }
        }
    }
}
