using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(Tonemapping))]
    sealed class TonemappingEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;

        // HDR Mode.
        SerializedDataParameter m_NeutralHDRRangeReductionMode;
        SerializedDataParameter m_HueShiftAmount;
        SerializedDataParameter m_HDRDetectPaperWhite;
        SerializedDataParameter m_HDRPaperwhite;
        SerializedDataParameter m_HDRDetectNitLimits;
        SerializedDataParameter m_HDRMinNits;
        SerializedDataParameter m_HDRMaxNits;
        SerializedDataParameter m_HDRAcesPreset;

        public override bool hasAdditionalProperties => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Tonemapping>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
            m_NeutralHDRRangeReductionMode = Unpack(o.Find(x => x.neutralHDRRangeReductionMode));
            m_HueShiftAmount = Unpack(o.Find(x => x.hueShiftAmount));
            m_HDRDetectPaperWhite = Unpack(o.Find(x => x.detectPaperWhite));
            m_HDRPaperwhite = Unpack(o.Find(x => x.paperWhite));
            m_HDRDetectNitLimits = Unpack(o.Find(x => x.detectBrightnessLimits));
            m_HDRMinNits = Unpack(o.Find(x => x.minNits));
            m_HDRMaxNits = Unpack(o.Find(x => x.maxNits));
            m_HDRAcesPreset = Unpack(o.Find(x => x.acesPreset));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);

            // Display a warning if the user is trying to use a tonemap while rendering in LDR
            var asset = UniversalRenderPipeline.asset;
            if (asset != null && !asset.supportsHDR)
            {
                EditorGUILayout.HelpBox("Tonemapping should only be used when working with High Dynamic Range (HDR). Please enable HDR through the active Render Pipeline Asset.", MessageType.Warning);
                return;
            }

            if (PlayerSettings.useHDRDisplay && m_Mode.value.intValue != (int)TonemappingMode.None)
            {
                EditorGUILayout.LabelField("HDR Output");
                int hdrTonemapMode = m_Mode.value.intValue;

                if (hdrTonemapMode == (int)TonemappingMode.Neutral)
                {
                    PropertyField(m_NeutralHDRRangeReductionMode);
                    PropertyField(m_HueShiftAmount);

                    PropertyField(m_HDRDetectPaperWhite);
                    EditorGUI.indentLevel++;
                    using (new EditorGUI.DisabledScope(m_HDRDetectPaperWhite.value.boolValue))
                    {
                        PropertyField(m_HDRPaperwhite);
                    }
                    EditorGUI.indentLevel--;

                    PropertyField(m_HDRDetectNitLimits);
                    EditorGUI.indentLevel++;
                    using (new EditorGUI.DisabledScope(m_HDRDetectNitLimits.value.boolValue))
                    {
                        PropertyField(m_HDRMinNits);
                        PropertyField(m_HDRMaxNits);
                    }
                    EditorGUI.indentLevel--;
                }
                if (hdrTonemapMode == (int)TonemappingMode.ACES)
                {
                    PropertyField(m_HDRAcesPreset);

                    PropertyField(m_HDRDetectPaperWhite);
                    EditorGUI.indentLevel++;
                    using (new EditorGUI.DisabledScope(m_HDRDetectPaperWhite.value.boolValue))
                    {
                        PropertyField(m_HDRPaperwhite);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
