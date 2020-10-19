using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    // TODO: handle retina / EditorGUIUtility.pixelsPerPoint
    [VolumeComponentEditor(typeof(ShadowsMidtonesHighlights))]
    sealed class ShadowsMidtonesHighlightsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Shadows;
        SerializedDataParameter m_Midtones;
        SerializedDataParameter m_Highlights;
        SerializedDataParameter m_ShadowsStart;
        SerializedDataParameter m_ShadowsEnd;
        SerializedDataParameter m_HighlightsStart;
        SerializedDataParameter m_HighlightsEnd;

        readonly TrackballUIDrawer m_TrackballUIDrawer = new TrackballUIDrawer();

        // Curve drawing utilities
        Rect m_CurveRect;
        Material m_Material;
        RenderTexture m_CurveTex;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ShadowsMidtonesHighlights>(serializedObject);

            m_Shadows         = Unpack(o.Find(x => x.shadows));
            m_Midtones        = Unpack(o.Find(x => x.midtones));
            m_Highlights      = Unpack(o.Find(x => x.highlights));
            m_ShadowsStart    = Unpack(o.Find(x => x.shadowsStart));
            m_ShadowsEnd      = Unpack(o.Find(x => x.shadowsEnd));
            m_HighlightsStart = Unpack(o.Find(x => x.highlightsStart));
            m_HighlightsEnd   = Unpack(o.Find(x => x.highlightsEnd));

            m_Material = new Material(Shader.Find("Hidden/HD PostProcessing/Editor/Shadows Midtones Highlights Curve"));
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                m_TrackballUIDrawer.OnGUI(m_Shadows.value, m_Shadows.overrideState, EditorGUIUtility.TrTextContent("Shadows"), GetWheelValue);
                GUILayout.Space(4f);
                m_TrackballUIDrawer.OnGUI(m_Midtones.value, m_Midtones.overrideState, EditorGUIUtility.TrTextContent("Midtones"), GetWheelValue);
                GUILayout.Space(4f);
                m_TrackballUIDrawer.OnGUI(m_Highlights.value, m_Highlights.overrideState, EditorGUIUtility.TrTextContent("Highlights"), GetWheelValue);
            }
            EditorGUILayout.Space();

            // Reserve GUI space
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                m_CurveRect = GUILayoutUtility.GetRect(128, 80);
            }

            if (Event.current.type == EventType.Repaint)
            {
                float alpha = GUI.enabled ? 1f : 0.4f;
                var limits = new Vector4(m_ShadowsStart.value.floatValue, m_ShadowsEnd.value.floatValue, m_HighlightsStart.value.floatValue, m_HighlightsEnd.value.floatValue);

                m_Material.SetVector(HDShaderIDs._ShaHiLimits, limits);
                m_Material.SetVector(HDShaderIDs._Variants, new Vector4(alpha, Mathf.Max(m_HighlightsEnd.value.floatValue, 1f), 0f, 0f));

                CheckCurveRT((int)m_CurveRect.width, (int)m_CurveRect.height);

                var oldRt = RenderTexture.active;
                Graphics.Blit(null, m_CurveTex, m_Material, EditorGUIUtility.isProSkin ? 0 : 1);
                RenderTexture.active = oldRt;

                GUI.DrawTexture(m_CurveRect, m_CurveTex);

                Handles.DrawSolidRectangleWithOutline(m_CurveRect, Color.clear, Color.white * 0.4f);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Shadow Limits", EditorStyles.miniLabel);
            PropertyField(m_ShadowsStart, EditorGUIUtility.TrTextContent("Start"));
            m_ShadowsStart.value.floatValue = Mathf.Min(m_ShadowsStart.value.floatValue, m_ShadowsEnd.value.floatValue);
            PropertyField(m_ShadowsEnd, EditorGUIUtility.TrTextContent("End"));
            m_ShadowsEnd.value.floatValue = Mathf.Max(m_ShadowsStart.value.floatValue, m_ShadowsEnd.value.floatValue);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Highlight Limits", EditorStyles.miniLabel);
            PropertyField(m_HighlightsStart, EditorGUIUtility.TrTextContent("Start"));
            m_HighlightsStart.value.floatValue = Mathf.Min(m_HighlightsStart.value.floatValue, m_HighlightsEnd.value.floatValue);
            PropertyField(m_HighlightsEnd, EditorGUIUtility.TrTextContent("End"));
            m_HighlightsEnd.value.floatValue = Mathf.Max(m_HighlightsStart.value.floatValue, m_HighlightsEnd.value.floatValue);
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

        Vector3 GetWheelValue(Vector4 v)
        {
            float w = v.w * (Mathf.Sign(v.w) < 0f ? 1f : 4f);
            return new Vector3(
                Mathf.Max(v.x + w, 0f),
                Mathf.Max(v.y + w, 0f),
                Mathf.Max(v.z + w, 0f)
            );
        }
    }
}
