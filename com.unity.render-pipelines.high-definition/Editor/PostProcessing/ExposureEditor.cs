using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
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
        SerializedDataParameter m_CurveMin;
        SerializedDataParameter m_CurveMax;

        SerializedDataParameter m_AdaptationMode;
        SerializedDataParameter m_AdaptationSpeedDarkToLight;
        SerializedDataParameter m_AdaptationSpeedLightToDark;

        SerializedDataParameter m_WeightTextureMask;

        SerializedDataParameter m_HistogramPercentages;
        SerializedDataParameter m_HistogramCurveRemapping;

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
            m_CurveMin = Unpack(o.Find(x => x.limitMinCurveMap));
            m_CurveMax = Unpack(o.Find(x => x.limitMaxCurveMap));

            m_AdaptationMode = Unpack(o.Find(x => x.adaptationMode));
            m_AdaptationSpeedDarkToLight = Unpack(o.Find(x => x.adaptationSpeedDarkToLight));
            m_AdaptationSpeedLightToDark = Unpack(o.Find(x => x.adaptationSpeedLightToDark));

            m_WeightTextureMask = Unpack(o.Find(x => x.weightTextureMask));

            m_HistogramPercentages = Unpack(o.Find(x => x.histogramPercentages));
            m_HistogramCurveRemapping = Unpack(o.Find(x => x.histogramUseCurveRemapping));

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
                PropertyField(m_Compensation);
            }
            else
            {
                EditorGUILayout.Space();

                PropertyField(m_MeteringMode);
                if(m_MeteringMode.value.intValue == (int)MeteringMode.MaskWeighted)
                    PropertyField(m_WeightTextureMask);

                // Temporary hiding the field since we don't support anything but color buffer for now.
                //PropertyField(m_LuminanceSource);

                //if (m_LuminanceSource.value.intValue == (int)LuminanceSource.LightingBuffer)
                //    EditorGUILayout.HelpBox("Luminance source buffer isn't supported yet.", MessageType.Warning);

                if (mode == (int) ExposureMode.CurveMapping)
                {
                    PropertyField(m_CurveMap);
                    PropertyField(m_CurveMin, EditorGUIUtility.TrTextContent("Limit Min", "Specifies a curve that determines for each current exposure value (x-value) what minimum value is allowed to auto-adaptation (y-axis)."));
                    PropertyField(m_CurveMax, EditorGUIUtility.TrTextContent("Limit Max", "Specifies a curve that determines for each current exposure value (x-value) what maximum value is allowed to auto-adaptation (y-axis)."));
                }
                else if(!(mode == (int)ExposureMode.AutomaticHistogram && m_HistogramCurveRemapping.value.boolValue))
                {
                    PropertyField(m_LimitMin);
                    PropertyField(m_LimitMax);
                }

                PropertyField(m_Compensation);

                if (mode == (int)ExposureMode.AutomaticHistogram)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Histogram", EditorStyles.miniLabel);
                    PropertyField(m_HistogramPercentages);
                    PropertyField(m_HistogramCurveRemapping, EditorGUIUtility.TrTextContent("Use Curve Remapping"));
                    if (m_HistogramCurveRemapping.value.boolValue)
                    {
                        PropertyField(m_CurveMap);
                        PropertyField(m_CurveMin, EditorGUIUtility.TrTextContent("Limit Min"));
                        PropertyField(m_CurveMax, EditorGUIUtility.TrTextContent("Limit Max"));
                    }
                }

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
