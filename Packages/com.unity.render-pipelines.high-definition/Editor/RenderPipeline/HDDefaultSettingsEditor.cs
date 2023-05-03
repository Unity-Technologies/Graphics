using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDRenderPipelineGlobalSettings))]
    [CanEditMultipleObjects]
    sealed class HDRenderPipelineGlobalSettingsEditor : Editor
    {
        [MenuItem("Assets/Create/Rendering/HDRP Global Settings Asset", priority = CoreUtils.Sections.section4 + 2)]
        internal static void CreateAsset()
        {
            RenderPipelineGlobalSettingsEndNameEditAction.CreateNew<HDRenderPipeline, HDRenderPipelineGlobalSettings>();
        }

        SerializedHDRenderPipelineGlobalSettings m_SerializedHDRenderPipelineGlobalSettings;

        internal bool largeLabelWidth = true;

        Editor m_DefaultVolumeProfileEditor;
        Editor m_LookDevVolumeProfileEditor;

        internal Editor GetDefaultVolumeProfileEditor(VolumeProfile asset)
        {
            CreateCachedEditor(asset, typeof(VolumeProfileEditor), ref m_DefaultVolumeProfileEditor);
            var editor = m_DefaultVolumeProfileEditor as VolumeProfileEditor;
            editor.componentList.SetIsGlobalDefaultVolumeProfile(true);
            return m_DefaultVolumeProfileEditor;
        }

        internal Editor GetLookDevDefaultVolumeProfileEditor(VolumeProfile lookDevAsset)
        {
            CreateCachedEditor(lookDevAsset, typeof(VolumeProfileEditor), ref m_LookDevVolumeProfileEditor);
            return m_LookDevVolumeProfileEditor;
        }

        void OnEnable()
        {
            m_SerializedHDRenderPipelineGlobalSettings = new SerializedHDRenderPipelineGlobalSettings(serializedObject);

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            CoreUtils.Destroy(m_DefaultVolumeProfileEditor);
            CoreUtils.Destroy(m_LookDevVolumeProfileEditor);

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            if (target is HDRenderPipelineGlobalSettings settings &&
                settings.volumeProfile != VolumeManager.instance.globalDefaultProfile)
            {
                var globalSettings = HDRenderPipelineGlobalSettings.instance;
                var defaultValuesAsset = globalSettings != null ? globalSettings.renderPipelineEditorResources.defaultSettingsVolumeProfile : null;
                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<HDRenderPipeline>(settings.volumeProfile, defaultValuesAsset);
            }
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
