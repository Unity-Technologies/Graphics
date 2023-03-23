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

        static Editor s_CachedDefaultVolumeProfileEditor;
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


        void OnEnable()
        {
            m_SerializedGlobalSettings = new SerializedUniversalRenderPipelineGlobalSettings(serializedObject);

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDisable()
        {
            if (s_CachedDefaultVolumeProfileEditor != null)
                UnityEngine.Object.DestroyImmediate(s_CachedDefaultVolumeProfileEditor);

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            if (target is UniversalRenderPipelineGlobalSettings settings &&
                settings.volumeProfile != VolumeManager.instance.globalDefaultProfile)
            {
                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile(settings.volumeProfile);
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
