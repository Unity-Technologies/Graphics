using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // TODO: handle retina / EditorGUIUtility.pixelsPerPoint
    [VolumeComponentEditor(typeof(Tonemapping))]
    sealed class TonemappingEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_ToeStrength;
        SerializedDataParameter m_ToeLength;
        SerializedDataParameter m_ShoulderStrength;
        SerializedDataParameter m_ShoulderLength;
        SerializedDataParameter m_ShoulderAngle;
        SerializedDataParameter m_Gamma;

        // Curve drawing utilities
        readonly HableCurve m_HableCurve = new HableCurve();
        Rect m_CurveRect;
        Material m_Material;
        RenderTexture m_CurveTex;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Tonemapping>(serializedObject);
            
            m_Mode             = Unpack(o.Find(x => x.mode));
            m_ToeStrength      = Unpack(o.Find(x => x.toeStrength));
            m_ToeLength        = Unpack(o.Find(x => x.toeLength));
            m_ShoulderStrength = Unpack(o.Find(x => x.shoulderStrength));
            m_ShoulderLength   = Unpack(o.Find(x => x.shoulderLength));
            m_ShoulderAngle    = Unpack(o.Find(x => x.shoulderAngle));
            m_Gamma            = Unpack(o.Find(x => x.gamma));

            m_Material = new Material(Shader.Find("Hidden/HD PostProcessing/Editor/Custom Tonemapper Curve"));
        }

        public override void OnDisable()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;

            CoreUtils.Destroy(m_CurveTex);
            m_CurveTex = null;
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);

            // Draw a curve for the custom tonemapping mode to make it easier to tweak visually
            if (m_Mode.value.intValue == (int)TonemappingMode.Custom)
            {
                EditorGUILayout.Space();

                // Reserve GUI space
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(EditorGUI.indentLevel * 15f);
                    m_CurveRect = GUILayoutUtility.GetRect(128, 80);
                }

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
        }

        void CheckCurveRT(int width, int height)
        {
            if (m_CurveTex == null || !m_CurveTex.IsCreated() || m_CurveTex.width != width || m_CurveTex.height != height)
            {
                CoreUtils.Destroy(m_CurveTex);
                m_CurveTex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
                m_CurveTex.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
}
