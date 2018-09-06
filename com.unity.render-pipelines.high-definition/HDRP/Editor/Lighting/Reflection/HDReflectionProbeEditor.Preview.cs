using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDReflectionProbeEditor
    {
        HDCubemapInspector m_CubemapEditor;

        public override bool HasPreviewGUI()
        {
            if (targets.Length > 1)
                return false;  // We only handle one preview for reflection probes

            // Ensure valid cube map editor (if possible)
            Texture texture = GetTexture();
            if (m_CubemapEditor != null && m_CubemapEditor.target as Texture != texture)
            {
                DestroyImmediate(m_CubemapEditor);
                m_CubemapEditor = null;
            }
            if (ValidPreviewSetup() && m_CubemapEditor == null)
            {
                Editor editor = m_CubemapEditor;
                CreateCachedEditor(GetTexture(), typeof(HDCubemapInspector), ref editor);
                m_CubemapEditor = editor as HDCubemapInspector;
            }

            // If having one probe selected we always want preview (to prevent preview window from popping)
            return true;
        }

        public override void OnPreviewSettings()
        {
            if (!ValidPreviewSetup()
                || m_CubemapEditor == null)
                return;

            m_CubemapEditor.OnPreviewSettings();
        }

        public override void OnPreviewGUI(Rect position, GUIStyle style)
        {
            if (!ValidPreviewSetup()
                || m_CubemapEditor == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Color prevColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.5f);
                GUILayout.Label("Reflection Probe not baked yet");
                GUI.color = prevColor;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }
            
            Texture tex = GetTexture();
            if (tex != null && targets.Length == 1)
                m_CubemapEditor.DrawPreview(position);
        }

        bool ValidPreviewSetup()
        {
            return GetTexture() != null;
        }

        Texture GetTexture()
        {
            HDProbe additional = GetTarget(target);
            if (additional != null && additional.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
            {
                return additional.realtimeTexture;
            }
            else
            {
                var p = target as ReflectionProbe;
                if (p != null)
                    return p.texture;
            }
            return null;
        }

        private void OnDestroy()
        {
            DestroyImmediate(m_CubemapEditor);
        }
    }
}
