using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        HDCubemapInspector m_CubemapEditor;

        public override bool HasPreviewGUI()
        {
            if (targets.Length > 1)
                return false;  // We only handle one preview for reflection probes

            // Ensure valid cube map editor (if possible)
            if (ValidPreviewSetup())
            {
                Editor editor = m_CubemapEditor;
                CreateCachedEditor(((ReflectionProbe)target).texture, typeof(HDCubemapInspector), ref editor);
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

            var p = target as ReflectionProbe;
            if (p != null && p.texture != null && targets.Length == 1)
                m_CubemapEditor.DrawPreview(position);

        }

        bool ValidPreviewSetup()
        {
            var p = target as ReflectionProbe;
            return p != null && p.texture != null;
        }
    }
}
