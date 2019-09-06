using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(DepthOfField))]
    sealed class DepthOfFieldEditor : VolumeComponentWithQualityEditor
    {
        SerializedDataParameter m_FocusMode;

        // Physical mode
        SerializedDataParameter m_FocusDistance;

        // Manual mode
        SerializedDataParameter m_NearFocusStart;
        SerializedDataParameter m_NearFocusEnd;
        SerializedDataParameter m_FarFocusStart;
        SerializedDataParameter m_FarFocusEnd;

        // Shared settings
        SerializedDataParameter m_NearSampleCount;
        SerializedDataParameter m_NearMaxBlur;
        SerializedDataParameter m_FarSampleCount;
        SerializedDataParameter m_FarMaxBlur;

        // Advanced settings
        SerializedDataParameter m_HighQualityFiltering;
        SerializedDataParameter m_Resolution;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<DepthOfField>(serializedObject);

            m_FocusMode = Unpack(o.Find(x => x.focusMode));

            m_FocusDistance = Unpack(o.Find(x => x.focusDistance));

            m_NearFocusStart = Unpack(o.Find(x => x.nearFocusStart));
            m_NearFocusEnd = Unpack(o.Find(x => x.nearFocusEnd));
            m_FarFocusStart = Unpack(o.Find(x => x.farFocusStart));
            m_FarFocusEnd = Unpack(o.Find(x => x.farFocusEnd));

            m_NearSampleCount = Unpack(o.Find("m_NearSampleCount"));
            m_NearMaxBlur = Unpack(o.Find("m_NearMaxBlur"));
            m_FarSampleCount = Unpack(o.Find("m_FarSampleCount"));
            m_FarMaxBlur = Unpack(o.Find("m_FarMaxBlur"));

            m_HighQualityFiltering = Unpack(o.Find("m_HighQualityFiltering"));
            m_Resolution = Unpack(o.Find("m_Resolution"));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_FocusMode);

            int mode = m_FocusMode.value.intValue;
            if (mode == (int)DepthOfFieldMode.Off)
                return;

            base.OnInspectorGUI();

            bool advanced = isInAdvancedMode;

            if (mode == (int)DepthOfFieldMode.UsePhysicalCamera)
            {
                PropertyField(m_FocusDistance);

                if (advanced)
                {
                    GUI.enabled = useCustomValue;
                    EditorGUILayout.LabelField("Near Blur", EditorStyles.miniLabel);
                    PropertyField(m_NearSampleCount, EditorGUIUtility.TrTextContent("Sample Count"));
                    PropertyField(m_NearMaxBlur, EditorGUIUtility.TrTextContent("Max Radius"));

                    EditorGUILayout.LabelField("Far Blur", EditorStyles.miniLabel);
                    PropertyField(m_FarSampleCount, EditorGUIUtility.TrTextContent("Sample Count"));
                    PropertyField(m_FarMaxBlur, EditorGUIUtility.TrTextContent("Max Radius"));
                    GUI.enabled = true;
                }
            }
            else if (mode == (int)DepthOfFieldMode.Manual)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Near Blur", EditorStyles.miniLabel);
                PropertyField(m_NearFocusStart, EditorGUIUtility.TrTextContent("Start"));
                PropertyField(m_NearFocusEnd, EditorGUIUtility.TrTextContent("End"));

                if (advanced)
                {
                    GUI.enabled = useCustomValue;
                    PropertyField(m_NearSampleCount, EditorGUIUtility.TrTextContent("Sample Count"));
                    PropertyField(m_NearMaxBlur, EditorGUIUtility.TrTextContent("Max Radius"));
                    GUI.enabled = true;
                }

                EditorGUILayout.LabelField("Far Blur", EditorStyles.miniLabel);
                PropertyField(m_FarFocusStart, EditorGUIUtility.TrTextContent("Start"));
                PropertyField(m_FarFocusEnd, EditorGUIUtility.TrTextContent("End"));

                if (advanced)
                {
                    GUI.enabled = useCustomValue;
                    PropertyField(m_FarSampleCount, EditorGUIUtility.TrTextContent("Sample Count"));
                    PropertyField(m_FarMaxBlur, EditorGUIUtility.TrTextContent("Max Radius"));
                    GUI.enabled = true;
                }
            }

            if (advanced)
            {
                GUI.enabled = useCustomValue;
                EditorGUILayout.LabelField("Advanced Tweaks", EditorStyles.miniLabel);
                PropertyField(m_Resolution);
                PropertyField(m_HighQualityFiltering);
                GUI.enabled = true;
            }
        }
    }
}
