using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class HDReflectionProbeEditor
    {
        Editor m_CubemapEditor;

        public override bool HasPreviewGUI()
        {
            if (targets.Length > 1)
                return false;  // We only handle one preview for reflection probes

            // Ensure valid cube map editor (if possible)
            Texture texture = GetTexture(this, target);
            if (m_CubemapEditor != null && m_CubemapEditor.target as Texture != texture)
            {
                DestroyImmediate(m_CubemapEditor);
                m_CubemapEditor = null;
            }
            if (ValidPreviewSetup() && m_CubemapEditor == null)
            {
                Editor editor = m_CubemapEditor;
                m_CubemapEditor = CreateEditor(GetTexture(this, target));
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
                GUILayout.Label("There is no Texture available for the Reflection Probe. Either use Baked and bake a Texture in, use Custom and assign a Texture, or enable Realtime.");
                GUI.color = prevColor;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            Texture tex = GetTexture(this, target);
            if (tex != null && targets.Length == 1)
                m_CubemapEditor.DrawPreview(position);
        }

        bool ValidPreviewSetup()
        {
            return GetTexture(this, target) != null;
        }

        static Texture GetTexture(HDReflectionProbeEditor e, Object target)
        {
            HDProbe probe = e.GetTarget(target);
            return probe.texture;
        }

        private void OnDestroy()
        {
            DestroyImmediate(m_CubemapEditor);
        }
    }
}
