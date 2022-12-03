using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDRenderPipelineGlobalSettings))]
    [CanEditMultipleObjects]
    sealed class HDRenderPipelineGlobalSettingsEditor : Editor
    {
        SerializedHDRenderPipelineGlobalSettings m_SerializedHDRenderPipelineGlobalSettings;

        internal bool largeLabelWidth = true;

        static Editor s_CachedDefaultVolumeProfileEditor;
        static Editor s_CachedLookDevVolumeProfileEditor;
        static int s_CurrentVolumeProfileInstanceID;

        static Type s_VolumeProfileEditorType = Type.GetType("UnityEditor.Rendering.VolumeProfileEditor");

        internal Editor GetDefaultVolumeProfileEditor(VolumeProfile asset)
        {
            // The state of the profile can change without the asset reference changing so in this case we need to reset the editor.
            if (s_CurrentVolumeProfileInstanceID != asset.GetInstanceID() && s_CachedDefaultVolumeProfileEditor != null)
            {
                s_CurrentVolumeProfileInstanceID = asset.GetInstanceID();
                UnityEngine.Object.DestroyImmediate(s_CachedDefaultVolumeProfileEditor);
                s_CachedDefaultVolumeProfileEditor = null;
            }

            Editor.CreateCachedEditor(asset, s_VolumeProfileEditorType, ref s_CachedDefaultVolumeProfileEditor);

            return s_CachedDefaultVolumeProfileEditor;
        }

        internal Editor GetLookDevDefaultVolumeProfileEditor(VolumeProfile lookDevAsset)
        {
            Editor.CreateCachedEditor(lookDevAsset, s_VolumeProfileEditorType, ref s_CachedLookDevVolumeProfileEditor);
            return s_CachedLookDevVolumeProfileEditor;
        }

        void OnEnable()
        {
            m_SerializedHDRenderPipelineGlobalSettings = new SerializedHDRenderPipelineGlobalSettings(serializedObject);
        }

        private void OnDisable()
        {
            if (s_CachedDefaultVolumeProfileEditor != null)
                UnityEngine.Object.DestroyImmediate(s_CachedDefaultVolumeProfileEditor);

            if (s_CachedLookDevVolumeProfileEditor != null)
                UnityEngine.Object.DestroyImmediate(s_CachedLookDevVolumeProfileEditor);
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedHDRenderPipelineGlobalSettings;

            serialized.serializedObject.Update();

            // In the quality window use more space for the labels
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 2;
            HDRenderPipelineGlobalSettingsUI.Inspector.Draw(serialized, this);
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 0.5f;

            serialized.serializedObject.ApplyModifiedProperties();
        }
    }
}
