using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniversalRenderPipelineGlobalSettings))]
    sealed class UniversalGlobalSettingsEditor : Editor
    {
        [MenuItem("Assets/Create/Rendering/URP Global Settings Asset", priority = CoreUtils.Sections.section4 + 2)]
        internal static void CreateAsset()
        {
            RenderPipelineGlobalSettingsEndNameEditAction.CreateNew<UniversalRenderPipeline, UniversalRenderPipelineGlobalSettings>();
        }

        SerializedUniversalRenderPipelineGlobalSettings m_SerializedGlobalSettings;

        Editor m_DefaultVolumeProfileEditor;
        Editor m_LookDevVolumeProfileEditor;

        internal Editor GetDefaultVolumeProfileEditor(VolumeProfile asset)
        {
            CreateCachedEditor(asset, typeof(VolumeProfileEditor), ref m_DefaultVolumeProfileEditor);
            var editor = m_DefaultVolumeProfileEditor as VolumeProfileEditor;
            editor.componentList.SetIsGlobalDefaultVolumeProfile(true);
            return m_DefaultVolumeProfileEditor;
        }

        void OnEnable()
        {
            m_SerializedGlobalSettings = new SerializedUniversalRenderPipelineGlobalSettings(serializedObject);

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDisable()
        {
            CoreUtils.Destroy(m_DefaultVolumeProfileEditor);

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            if (target is UniversalRenderPipelineGlobalSettings settings &&
                settings.volumeProfile != VolumeManager.instance.globalDefaultProfile)
            {
                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<UniversalRenderPipeline>(settings.volumeProfile);
            }
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedGlobalSettings;

            serialized.serializedObject.Update();

            // In the quality window use more space for the labels
            UniversalRenderPipelineGlobalSettingsUI.Inspector.Draw(serialized, this);

            serialized.serializedObject.ApplyModifiedProperties();
        }
    }
}
