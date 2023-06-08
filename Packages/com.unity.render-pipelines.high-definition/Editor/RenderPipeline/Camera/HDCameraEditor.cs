using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(Camera))]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDCameraEditor : Editor
    {
        SerializedHDCamera m_SerializedCamera;

        void OnEnable()
        {
            m_SerializedCamera = new SerializedHDCamera(serializedObject);

            Undo.undoRedoPerformed += ReconstructReferenceToAdditionalDataSO;
        }

        void ReconstructReferenceToAdditionalDataSO()
        {
            OnDisable();
            OnEnable();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= ReconstructReferenceToAdditionalDataSO;
        }

        public override void OnInspectorGUI()
        {
            m_SerializedCamera.Update();

            if (HDEditorUtils.IsPresetEditor(this))
            {
                HDCameraUI.PresetInspector.Draw(m_SerializedCamera, this);
            }
            else
            {
                HDCameraUI.Inspector.Draw(m_SerializedCamera, this);
            }

            m_SerializedCamera.Apply();
        }
    }
}
