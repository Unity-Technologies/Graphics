using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDAdditionalMeshRendererSettings))]
    internal class HDAdditionalMeshRendererSettingsEditor : Editor
    {
        SerializedHDAdditionalMeshRendererSettings m_SerializedAdditionalMeshRendererSettings;

        private void OnEnable()
        {
            m_SerializedAdditionalMeshRendererSettings = new SerializedHDAdditionalMeshRendererSettings(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            m_SerializedAdditionalMeshRendererSettings.Update();
            {
                HDAdditionalMeshRendererSettingsUI.Inspector.Draw(m_SerializedAdditionalMeshRendererSettings, this);
            }
            m_SerializedAdditionalMeshRendererSettings.Apply();
        }
    }

    internal class SerializedHDAdditionalMeshRendererSettings
    {
        public SerializedObject serializedObject { get; }

        public SerializedProperty enableHighQualityLineRendering;
        public SerializedProperty rendererGroup;
        public SerializedProperty rendererLODMode;
        public SerializedProperty rendererLODFixed;
        public SerializedProperty rendererLODCameraDistanceCurve;
        public SerializedProperty rendererLODScreenCoverageCurve;
        public SerializedProperty shadingSampleFraction;

        public SerializedHDAdditionalMeshRendererSettings(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            enableHighQualityLineRendering = serializedObject.Find((HDAdditionalMeshRendererSettings d) => d.enableHighQualityLineRendering);
            rendererLODMode = serializedObject.Find((HDAdditionalMeshRendererSettings d) => d.rendererLODMode);
            rendererLODFixed = serializedObject.Find((HDAdditionalMeshRendererSettings d) => d.rendererLODFixed);
            rendererGroup = serializedObject.Find((HDAdditionalMeshRendererSettings d) => d.rendererGroup);
            rendererLODCameraDistanceCurve = serializedObject.Find((HDAdditionalMeshRendererSettings d) => d.rendererLODCameraDistanceCurve);
            rendererLODScreenCoverageCurve = serializedObject.Find((HDAdditionalMeshRendererSettings d) => d.rendererLODScreenCoverageCurve);
            shadingSampleFraction = serializedObject.Find((HDAdditionalMeshRendererSettings d) => d.shadingSampleFraction);
        }

        public void Update() => serializedObject.Update();
        public void Apply() => serializedObject.ApplyModifiedProperties();
    }

}
