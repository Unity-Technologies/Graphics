using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(MotionBlur))]
    sealed class MotionBlurEditor : VolumeComponentWithQualityEditor
    {
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_SampleCount;

        SerializedDataParameter m_MaxVelocityInPixels;
        SerializedDataParameter m_MinVelInPixels;

        //  Advanced properties
        SerializedDataParameter m_CameraMVClampMode;
        SerializedDataParameter m_CameraTransClamp;
        SerializedDataParameter m_CameraRotClamp;
        SerializedDataParameter m_CameraFullClamp;

        SerializedDataParameter m_DepthCmpScale;
        SerializedDataParameter m_CameraMotionBlur;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<MotionBlur>(serializedObject);

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_SampleCount = Unpack(o.Find("m_SampleCount"));
            m_MinVelInPixels = Unpack(o.Find(x => x.minimumVelocity));
            m_MaxVelocityInPixels = Unpack(o.Find(x => x.maximumVelocity));
            m_CameraMVClampMode = Unpack(o.Find(x => x.specialCameraClampMode));
            m_CameraFullClamp = Unpack(o.Find(x => x.cameraVelocityClamp));
            m_CameraTransClamp = Unpack(o.Find(x => x.cameraTranslationVelocityClamp));
            m_CameraRotClamp = Unpack(o.Find(x => x.cameraRotationVelocityClamp));
            m_DepthCmpScale = Unpack(o.Find(x => x.depthComparisonExtent));
            m_CameraMotionBlur = Unpack(o.Find(x => x.cameraMotionBlur));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            bool advanced = isInAdvancedMode;

            PropertyField(m_Intensity);

            base.OnInspectorGUI();

            using (new HDEditorUtils.IndentScope())
            using (new QualityScope(this))
            {
                PropertyField(m_SampleCount);
            }

            PropertyField(m_MaxVelocityInPixels);
            PropertyField(m_MinVelInPixels);

            if (advanced)
            {
                PropertyField(m_DepthCmpScale);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Camera Velocity", EditorStyles.miniLabel);

                PropertyField(m_CameraMotionBlur);


                using (new EditorGUI.DisabledScope(!m_CameraMotionBlur.value.boolValue))
                {
                    PropertyField(m_CameraMVClampMode, EditorGUIUtility.TrTextContent("Camera Clamp Mode", "Determine if and how the component of the motion vectors coming from the camera is clamped in a special fashion."));

                    using (new HDEditorUtils.IndentScope())
                    {
                        var mode = m_CameraMVClampMode.value.intValue;
                        using (new EditorGUI.DisabledScope(!(mode == (int)CameraClampMode.Rotation || mode == (int)CameraClampMode.SeparateTranslationAndRotation)))
                        {
                            PropertyField(m_CameraRotClamp, EditorGUIUtility.TrTextContent("Rotation Clamp", "Sets the maximum length, as a fraction of the screen's full resolution, that the motion vectors resulting from Camera rotation can have." +
                                                                                                             " Only valid if Camera clamp mode is set to Rotation or Separate Translation And Rotation."));
                        }
                        using (new EditorGUI.DisabledScope(!(mode == (int)CameraClampMode.Translation || mode == (int)CameraClampMode.SeparateTranslationAndRotation)))
                        {
                            PropertyField(m_CameraTransClamp, EditorGUIUtility.TrTextContent("Translation Clamp", "Sets the maximum length, as a fraction of the screen's full resolution, that the motion vectors resulting from Camera translation can have." +
                                                                                                               " Only valid if Camera clamp mode is set to Translation or Separate Translation And Rotation."));

                        }
                        using (new EditorGUI.DisabledScope(mode != (int)CameraClampMode.FullCameraMotionVector))
                        {
                            PropertyField(m_CameraFullClamp, EditorGUIUtility.TrTextContent("Motion Vector Clamp", "Sets the maximum length, as a fraction of the screen's full resolution, that the motion vectors resulting from Camera movement can have." +
                                                                                                                 " Only valid if Camera clamp mode is set to Full Camera Motion Vector."));

                        }
                    }
                }



            }
        }
        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob settings = null)
        {
            if (settings == null)
                settings = new QualitySettingsBlob();

            settings.Save<int>(m_SampleCount);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            settings.TryLoad<int>(ref m_SampleCount);
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            CopySetting(ref m_SampleCount, settings.postProcessQualitySettings.MotionBlurSampleCount[level]);
        }
    }
}
