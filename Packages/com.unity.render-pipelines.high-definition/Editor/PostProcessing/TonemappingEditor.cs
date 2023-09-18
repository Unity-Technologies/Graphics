using System.IO;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    // TODO: handle retina / EditorGUIUtility.pixelsPerPoint
    [CustomEditor(typeof(Tonemapping))]
    sealed class TonemappingEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_UseFullACES;
        SerializedDataParameter m_ToeStrength;
        SerializedDataParameter m_ToeLength;
        SerializedDataParameter m_ShoulderStrength;
        SerializedDataParameter m_ShoulderLength;
        SerializedDataParameter m_ShoulderAngle;
        SerializedDataParameter m_Gamma;
        SerializedDataParameter m_LutTexture;
        SerializedDataParameter m_LutContribution;

        // HDR Mode.
        SerializedDataParameter m_NeutralHDRRangeReductionMode;
        SerializedDataParameter m_HueShiftAmount;
        SerializedDataParameter m_HDRDetectPaperWhite;
        SerializedDataParameter m_HDRPaperwhite;
        SerializedDataParameter m_HDRDetectNitLimits;
        SerializedDataParameter m_HDRMinNits;
        SerializedDataParameter m_HDRMaxNits;
        SerializedDataParameter m_HDRAcesPreset;
        SerializedDataParameter m_HDRFallbackMode;

        public override bool hasAdditionalProperties => true;

        // Curve drawing utilities
        readonly HableCurve m_HableCurve = new HableCurve();
        Rect m_CurveRect;
        Material m_Material;
        RenderTexture m_CurveTex;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Tonemapping>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
            m_UseFullACES = Unpack(o.Find(x => x.useFullACES));
            m_ToeStrength = Unpack(o.Find(x => x.toeStrength));
            m_ToeLength = Unpack(o.Find(x => x.toeLength));
            m_ShoulderStrength = Unpack(o.Find(x => x.shoulderStrength));
            m_ShoulderLength = Unpack(o.Find(x => x.shoulderLength));
            m_ShoulderAngle = Unpack(o.Find(x => x.shoulderAngle));
            m_Gamma = Unpack(o.Find(x => x.gamma));
            m_LutTexture = Unpack(o.Find(x => x.lutTexture));
            m_LutContribution = Unpack(o.Find(x => x.lutContribution));

            m_NeutralHDRRangeReductionMode = Unpack(o.Find(x => x.neutralHDRRangeReductionMode));
            m_HueShiftAmount = Unpack(o.Find(x => x.hueShiftAmount));
            m_HDRDetectPaperWhite = Unpack(o.Find(x => x.detectPaperWhite));
            m_HDRPaperwhite = Unpack(o.Find(x => x.paperWhite));
            m_HDRDetectNitLimits = Unpack(o.Find(x => x.detectBrightnessLimits));
            m_HDRMinNits = Unpack(o.Find(x => x.minNits));
            m_HDRMaxNits = Unpack(o.Find(x => x.maxNits));
            m_HDRAcesPreset = Unpack(o.Find(x => x.acesPreset));
            m_HDRFallbackMode = Unpack(o.Find(x => x.fallbackMode));

            m_Material = new Material(Shader.Find("Hidden/HD PostProcessing/Editor/Custom Tonemapper Curve"));
        }

        public override void OnDisable()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;

            CoreUtils.Destroy(m_CurveTex);
            m_CurveTex = null;
        }

        internal bool HDROutputIsActive()
        {
            return SystemInfo.hdrDisplaySupportFlags.HasFlag(HDRDisplaySupportFlags.Supported) && HDROutputSettings.main.active;
        }


        public override void OnInspectorGUI()
        {
            bool hdrInPlayerSettings = UnityEditor.PlayerSettings.allowHDRDisplaySupport;

            PropertyField(m_Mode);

            // Draw a curve for the custom tonemapping mode to make it easier to tweak visually
            if (m_Mode.value.intValue == (int)TonemappingMode.Custom)
            {
                EditorGUILayout.Space();

                // Reserve GUI space
                m_CurveRect = GUILayoutUtility.GetRect(128, 80);
                m_CurveRect.xMin += EditorGUI.indentLevel * 15f;

                if (Event.current.type == EventType.Repaint)
                {
                    // Prepare curve data
                    float toeStrength = m_ToeStrength.value.floatValue;
                    float toeLength = m_ToeLength.value.floatValue;
                    float shoulderStrength = m_ShoulderStrength.value.floatValue;
                    float shoulderLength = m_ShoulderLength.value.floatValue;
                    float shoulderAngle = m_ShoulderAngle.value.floatValue;
                    float gamma = m_Gamma.value.floatValue;

                    m_HableCurve.Init(
                        toeStrength,
                        toeLength,
                        shoulderStrength,
                        shoulderLength,
                        shoulderAngle,
                        gamma
                    );

                    float alpha = GUI.enabled ? 1f : 0.5f;

                    m_Material.SetVector(HDShaderIDs._CustomToneCurve, m_HableCurve.uniforms.curve);
                    m_Material.SetVector(HDShaderIDs._ToeSegmentA, m_HableCurve.uniforms.toeSegmentA);
                    m_Material.SetVector(HDShaderIDs._ToeSegmentB, m_HableCurve.uniforms.toeSegmentB);
                    m_Material.SetVector(HDShaderIDs._MidSegmentA, m_HableCurve.uniforms.midSegmentA);
                    m_Material.SetVector(HDShaderIDs._MidSegmentB, m_HableCurve.uniforms.midSegmentB);
                    m_Material.SetVector(HDShaderIDs._ShoSegmentA, m_HableCurve.uniforms.shoSegmentA);
                    m_Material.SetVector(HDShaderIDs._ShoSegmentB, m_HableCurve.uniforms.shoSegmentB);
                    m_Material.SetVector(HDShaderIDs._Variants, new Vector4(alpha, m_HableCurve.whitePoint, 0f, 0f));

                    CheckCurveRT((int)m_CurveRect.width, (int)m_CurveRect.height);

                    var oldRt = RenderTexture.active;
                    Graphics.Blit(null, m_CurveTex, m_Material, EditorGUIUtility.isProSkin ? 0 : 1);
                    RenderTexture.active = oldRt;

                    GUI.DrawTexture(m_CurveRect, m_CurveTex);

                    Handles.DrawSolidRectangleWithOutline(m_CurveRect, Color.clear, Color.white * 0.4f);
                }

                PropertyField(m_ToeStrength);
                PropertyField(m_ToeLength);
                PropertyField(m_ShoulderStrength);
                PropertyField(m_ShoulderLength);
                PropertyField(m_ShoulderAngle);
                PropertyField(m_Gamma);
            }
            else if (m_Mode.value.intValue == (int)TonemappingMode.External)
            {
                PropertyField(m_LutTexture, EditorGUIUtility.TrTextContent("Lookup Texture"));

                var lut = m_LutTexture.value.objectReferenceValue;
                if (lut != null && !((Tonemapping)target).ValidateLUT())
                    EditorGUILayout.HelpBox("Invalid lookup texture. It must be a 3D Texture or a Render Texture and have the same size as set in the HDRP settings.", MessageType.Warning);

                PropertyField(m_LutContribution, EditorGUIUtility.TrTextContent("Contribution"));

                EditorGUILayout.HelpBox("Use \"Edit > Rendering > Render Selected HDRP Camera to Log EXR\" to export a log-encoded frame for external grading.", MessageType.Info);
            }
            else if (m_Mode.value.intValue == (int)TonemappingMode.ACES)
            {
                PropertyField(m_UseFullACES);
            }

            if (hdrInPlayerSettings && m_Mode.value.intValue != (int)TonemappingMode.None)
            {
                EditorGUILayout.LabelField("HDR Output");

                if (!HDROutputIsActive())
                {
                    EditorGUILayout.HelpBox("HDR is not currently active. Settings will take effect when a compatible device is found.", MessageType.Info);
                }

                int hdrTonemapMode = m_Mode.value.intValue;
                if (m_Mode.value.intValue == (int)TonemappingMode.Custom || hdrTonemapMode == (int)TonemappingMode.External)
                {
                    EditorGUILayout.HelpBox("The selected tonemapping mode is not supported in HDR Output mode. Select a fallback mode.", MessageType.Warning);
                    PropertyField(m_HDRFallbackMode);
                    hdrTonemapMode = (m_HDRFallbackMode.value.intValue == (int)FallbackHDRTonemap.ACES) ? (int)TonemappingMode.ACES :
                                     (m_HDRFallbackMode.value.intValue == (int)FallbackHDRTonemap.Neutral) ? (int)TonemappingMode.Neutral :
                                     (int)TonemappingMode.None;
                }

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

        void CheckCurveRT(int width, int height)
        {
            if (m_CurveTex == null || !m_CurveTex.IsCreated() || m_CurveTex.width != width || m_CurveTex.height != height)
            {
                CoreUtils.Destroy(m_CurveTex);
                m_CurveTex = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_SRGB);
                m_CurveTex.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }

    sealed class ExrExportMenu
    {
        [MenuItem("Edit/Rendering/Render Selected HDRP Camera to Log EXR %#&e", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.editMenuPriority + 1)]
        static void Export()
        {
            var camera = Selection.activeGameObject?.GetComponent<Camera>();

            if (camera == null)
            {
                Debug.LogError("Please select a camera before trying to export an EXR.");
                return;
            }

            var hdInstance = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (hdInstance == null)
            {
                Debug.LogError("No HDRenderPipeline set in GraphicsSettings.");
                return;
            }

            string outPath = EditorUtility.SaveFilePanel("Export EXR...", "", "Frame", "exr");

            if (string.IsNullOrEmpty(outPath))
                return;

            var w = camera.pixelWidth;
            var h = camera.pixelHeight;
            var texOut = new Texture2D(w, h, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);
            var target = RenderTexture.GetTemporary(w, h, 24, GraphicsFormat.R32G32B32A32_SFloat);
            var lastActive = RenderTexture.active;
            var lastTargetSet = camera.targetTexture;

            hdInstance.debugDisplaySettings.SetFullScreenDebugMode(FullScreenDebugMode.ColorLog);

            EditorUtility.DisplayProgressBar("Export EXR", "Rendering...", 0f);

            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = lastTargetSet;

            EditorUtility.DisplayProgressBar("Export EXR", "Reading...", 0.25f);

            hdInstance.debugDisplaySettings.SetFullScreenDebugMode(FullScreenDebugMode.None);

            RenderTexture.active = target;
            texOut.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            texOut.Apply();
            RenderTexture.active = lastActive;

            EditorUtility.DisplayProgressBar("Export EXR", "Encoding...", 0.5f);

            var bytes = texOut.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat | Texture2D.EXRFlags.CompressZIP);

            EditorUtility.DisplayProgressBar("Export EXR", "Saving...", 0.75f);

            File.WriteAllBytes(outPath, bytes);

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texOut);
        }
    }
}
