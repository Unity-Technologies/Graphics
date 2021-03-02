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

        SerializedDataParameter m_CenterAroundTarget;
        SerializedDataParameter m_ProceduralCenter;
        SerializedDataParameter m_ProceduralRadii;
        SerializedDataParameter m_ProceduralSoftness;
        SerializedDataParameter m_ProceduralMinIntensity;
        SerializedDataParameter m_ProceduralMaxIntensity;

        SerializedDataParameter m_TargetMidGray;

        private static LightUnitSliderUIDrawer k_LightUnitSlider;

        int m_RepaintsAfterChange = 0;
        int m_SettingsForDoubleRefreshHash = 0;
        static readonly string[] s_MidGrayNames = { "Grey 12.5%", "Grey 14.0%", "Grey 18.0%" };

        public override bool hasAdvancedMode => true;

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

            m_CenterAroundTarget = Unpack(o.Find(x => x.centerAroundExposureTarget));
            m_ProceduralCenter = Unpack(o.Find(x => x.proceduralCenter));
            m_ProceduralRadii = Unpack(o.Find(x => x.proceduralRadii));
            m_ProceduralSoftness = Unpack(o.Find(x => x.proceduralSoftness));
            m_ProceduralMinIntensity = Unpack(o.Find(x => x.maskMinIntensity));
            m_ProceduralMaxIntensity = Unpack(o.Find(x => x.maskMaxIntensity));

            m_TargetMidGray = Unpack(o.Find(x => x.targetMidGray));

            k_LightUnitSlider = new LightUnitSliderUIDrawer();
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
                DoExposurePropertyField(m_FixedExposure);
                PropertyField(m_Compensation);
            }
            else
            {
                EditorGUILayout.Space();

                PropertyField(m_MeteringMode);
                if(m_MeteringMode.value.intValue == (int)MeteringMode.MaskWeighted)
                    PropertyField(m_WeightTextureMask);

                if (m_MeteringMode.value.intValue == (int) MeteringMode.ProceduralMask)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Procedural Mask", EditorStyles.miniLabel);


                    PropertyField(m_CenterAroundTarget);

                    var centerLabel = EditorGUIUtility.TrTextContent("Center", "Sets the center of the procedural metering mask ([0,0] being bottom left of the screen and [1,1] top right of the screen)");
                    var centerValue = m_ProceduralCenter.value.vector2Value;

                    if (m_CenterAroundTarget.value.boolValue)
                    {
                        centerLabel = EditorGUIUtility.TrTextContent("Offset", "Sets an offset to the mask center");
                        m_ProceduralCenter.value.vector2Value = new Vector2(Mathf.Clamp(centerValue.x, -0.5f, 0.5f), Mathf.Clamp(centerValue.y, -0.5f, 0.5f));
                    }
                    else
                    {
                        m_ProceduralCenter.value.vector2Value = new Vector2(Mathf.Clamp01(centerValue.x), Mathf.Clamp01(centerValue.y));
                    }

                    PropertyField(m_ProceduralCenter, centerLabel);
                    var radiiValue = m_ProceduralRadii.value.vector2Value;
                    m_ProceduralRadii.value.vector2Value = new Vector2(Mathf.Clamp01(radiiValue.x), Mathf.Clamp01(radiiValue.y));
                    PropertyField(m_ProceduralRadii, EditorGUIUtility.TrTextContent("Radii", "Sets the radii of the procedural mask, in terms of fraction of the screen (i.e. 0.5 means a radius that stretch half of the screen)."));
                    PropertyField(m_ProceduralSoftness, EditorGUIUtility.TrTextContent("Softness", "Sets the softness of the mask, the higher the value the less influence is given to pixels at the edge of the mask"));

                    if (isInAdvancedMode)
                    {
                        PropertyField(m_ProceduralMinIntensity);
                        PropertyField(m_ProceduralMaxIntensity);
                    }

                    EditorGUILayout.Space();
                }

                // Temporary hiding the field since we don't support anything but color buffer for now.
                //PropertyField(m_LuminanceSource);

                //if (m_LuminanceSource.value.intValue == (int)LuminanceSource.LightingBuffer)
                //    EditorGUILayout.HelpBox("Luminance source buffer isn't supported yet.", MessageType.Warning);

                if (mode == (int)ExposureMode.CurveMapping)
                {
                    PropertyField(m_CurveMap);
                    PropertyField(m_CurveMin, EditorGUIUtility.TrTextContent("Limit Min"));
                    PropertyField(m_CurveMax, EditorGUIUtility.TrTextContent("Limit Max"));
                }
                else if (!(mode == (int)ExposureMode.AutomaticHistogram && m_HistogramCurveRemapping.value.boolValue))
                {
                    DoExposurePropertyField(m_LimitMin);
                    DoExposurePropertyField(m_LimitMax);
                }

                PropertyField(m_Compensation);

                if(mode == (int)ExposureMode.AutomaticHistogram)
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

                if (isInAdvancedMode)
                {
                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Override checkbox
                        DrawOverrideCheckbox(m_TargetMidGray);

                        // Property
                        using (new EditorGUI.DisabledScope(!m_TargetMidGray.overrideState.boolValue))
                        {
                            // Default unity field
                            m_TargetMidGray.value.intValue = EditorGUILayout.Popup(EditorGUIUtility.TrTextContent("Target Mid Grey", "Sets the desired Mid gray level used by the auto exposure (i.e. to what grey value the auto exposure system maps the average scene luminance)."),
                                                                                    m_TargetMidGray.value.intValue, s_MidGrayNames);
                        }
                    }
                }
            }

            // Since automatic exposure works on 2 frames (automatic exposure is computed from previous frame data), we need to trigger the scene repaint twice if
            // some of the changes that will lead to different results are changed.
            int automaticCurrSettingHash = m_LimitMin.value.floatValue.GetHashCode() +
                17 * m_LimitMax.value.floatValue.GetHashCode() +
                17 * m_Compensation.value.floatValue.GetHashCode();

            if (mode == (int)ExposureMode.Automatic || mode == (int)ExposureMode.AutomaticHistogram)
            {
                if (automaticCurrSettingHash != m_SettingsForDoubleRefreshHash)
                {
                    m_RepaintsAfterChange = 2;
                }
                else
                {
                    m_RepaintsAfterChange = Mathf.Max(0, m_RepaintsAfterChange - 1);
                }
                m_SettingsForDoubleRefreshHash = automaticCurrSettingHash;

                if (m_RepaintsAfterChange > 0)
                {
                    SceneView.RepaintAll();
                }
            }
        }

        // TODO: See if this can be refactored into a custom VolumeParameterDrawer
        void DoExposurePropertyField(SerializedDataParameter exposureProperty)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawOverrideCheckbox(exposureProperty);

                using (new EditorGUI.DisabledScope(!exposureProperty.overrideState.boolValue))
                    EditorGUILayout.LabelField(exposureProperty.displayName);
            }

            using (new EditorGUI.DisabledScope(!exposureProperty.overrideState.boolValue))
            {
                var xOffset = EditorGUIUtility.labelWidth + 22;
                var lineRect = EditorGUILayout.GetControlRect();
                lineRect.x += xOffset;
                lineRect.width -= xOffset;

                var sliderRect = lineRect;
                sliderRect.y -= EditorGUIUtility.singleLineHeight;
                k_LightUnitSlider.SetSerializedObject(serializedObject);
                k_LightUnitSlider.DrawExposureSlider(exposureProperty.value, sliderRect);

                // GUIContent.none disables horizontal scrolling, use TrTextContent and adjust the rect to make it work.
                lineRect.x -= EditorGUIUtility.labelWidth + 2;
                lineRect.width += EditorGUIUtility.labelWidth + 2;
                EditorGUI.PropertyField(lineRect, exposureProperty.value, EditorGUIUtility.TrTextContent(" "));
            }
        }
    }
}
