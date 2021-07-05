using System.Linq;
using UnityEditor.VFX.HDRP;
using UnityEditor.VFX.UI;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    sealed class HDRenderPipelineEditor : Editor
    {
        SerializedHDRenderPipelineAsset m_SerializedHDRenderPipeline;

        internal bool largeLabelWidth = true;
        internal bool needRefreshVfxWarnings = false;

        void OnEnable()
        {
            m_SerializedHDRenderPipeline = new SerializedHDRenderPipelineAsset(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedHDRenderPipeline;

            serialized.Update();

            // In the quality window use more space for the labels
            // [case 1253090] we also have to check if labelWidth was scaled already, to avoid scaling twice.
            // This can happen if we get a second Inspector.Draw before the first one returns (and labelwidth is reset).
            const float labelWidthThreshold = 150;
            if (!largeLabelWidth && (EditorGUIUtility.labelWidth <= labelWidthThreshold))
                EditorGUIUtility.labelWidth *= 2;
            HDRenderPipelineUI.Inspector.Draw(serialized, this);
            if (!largeLabelWidth && (EditorGUIUtility.labelWidth > labelWidthThreshold))
                EditorGUIUtility.labelWidth *= 0.5f;

            serialized.Apply();
            VFXHDRPSettingsUtility.RefreshVfxErrorsIfNeeded(ref needRefreshVfxWarnings);
        }
    }
}
