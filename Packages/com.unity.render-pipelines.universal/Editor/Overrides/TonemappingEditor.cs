using UnityEngine;
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
        SerializedDataParameter m_AgxContrast;
        SerializedDataParameter m_AgxMidGrey;

        static readonly GUIContent k_AgxContrast = L10n.TextContent("Contrast", "Contrast of the AgX tone curve. Higher deepens shadows and brightens highlights.", null, null);
        static readonly GUIContent k_AgxMidGrey = L10n.TextContent("Mid Grey", "The screen brightness that a mid-grey (18%) subject maps to. Raise to lighten the overall image.", null, null);

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
            m_AgxContrast = Unpack(o.Find(x => x.agxContrast));
            m_AgxMidGrey = Unpack(o.Find(x => x.agxMidGrey));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);

            // Display a warning if the user is trying to use a tonemap while rendering in LDR
            var asset = UniversalRenderPipeline.asset;
            int hdrTonemapMode = m_Mode.value.intValue;
            if (asset != null && !asset.supportsHDR && hdrTonemapMode != (int)TonemappingMode.None)
            {
                EditorGUILayout.HelpBox("Tonemapping should only be used when working with High Dynamic Range (HDR). Please enable HDR through the active Render Pipeline Asset.", MessageType.Warning);
                return;
            }

            // AgX tone-curve controls.
            if (hdrTonemapMode == (int)TonemappingMode.AgX)
            {
                PropertyField(m_AgxContrast, k_AgxContrast);
                PropertyField(m_AgxMidGrey, k_AgxMidGrey);
            }

            // HDR Output controls. Each mode surfaces only the luminance values it consumes on the HDR-output
            // path: Neutral/ACES their full range-reduction options; AgX just paper white + max nits (its display
            // mapping scales output by paper white and uses max nits for shoulder headroom — see the AgX branch in
            // LutBuilderHdr). AgX does not read min nits (its toe already resolves to 0 at input 0), so that field
            // is omitted for AgX rather than shown as a no-op.
            if (PlayerSettings.allowHDRDisplaySupport &&
                (hdrTonemapMode == (int)TonemappingMode.Neutral ||
                 hdrTonemapMode == (int)TonemappingMode.ACES ||
                 hdrTonemapMode == (int)TonemappingMode.AgX))
            {
                DrawHeader("HDR Output");

                if (hdrTonemapMode == (int)TonemappingMode.Neutral)
                {
                    PropertyField(m_NeutralHDRRangeReductionMode);
                    PropertyField(m_HueShiftAmount);

                    PropertyField(m_HDRDetectPaperWhite);
                    if (!m_HDRDetectPaperWhite.value.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        PropertyField(m_HDRPaperwhite);
                        EditorGUI.indentLevel--;
                    }

                    PropertyField(m_HDRDetectNitLimits);
                    if (!m_HDRDetectNitLimits.value.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        PropertyField(m_HDRMinNits);
                        PropertyField(m_HDRMaxNits);
                        EditorGUI.indentLevel--;
                    }
                }
                if (hdrTonemapMode == (int)TonemappingMode.ACES)
                {
                    PropertyField(m_HDRAcesPreset);

                    PropertyField(m_HDRDetectPaperWhite);
                    if (!m_HDRDetectPaperWhite.value.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        PropertyField(m_HDRPaperwhite);
                        EditorGUI.indentLevel--;
                    }
                }
                if (hdrTonemapMode == (int)TonemappingMode.AgX)
                {
                    PropertyField(m_HDRDetectPaperWhite);
                    if (!m_HDRDetectPaperWhite.value.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        PropertyField(m_HDRPaperwhite);
                        EditorGUI.indentLevel--;
                    }

                    // AgX consumes only max nits (shoulder headroom); min nits has no effect, so it is not shown.
                    PropertyField(m_HDRDetectNitLimits);
                    if (!m_HDRDetectNitLimits.value.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        PropertyField(m_HDRMaxNits);
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
    }
}
