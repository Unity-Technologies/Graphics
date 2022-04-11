using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class SerializedCompositionManager
    {
        public SerializedProperty compositionProfile;
        public SerializedProperty layerList;
        public SerializedProperty shaderProperties;
        public SerializedProperty displayNumber;
        public SerializedProperty compositionShader;
        public SerializedProperty outputCamera;

        public SerializedObject compositionProfileSO;
        public SerializedObject compositorSO;

        public SerializedCompositionManager(SerializedObject root)
        {
            compositorSO = root;
            {
                compositionProfile = compositorSO.FindProperty("m_CompositionProfile");
                displayNumber = compositorSO.FindProperty("m_OutputDisplay");
                compositionShader = compositorSO.FindProperty("m_Shader");
                outputCamera = compositorSO.FindProperty("m_OutputCamera");
                layerList = compositorSO.FindProperty("m_InputLayers");
            }

            // Work around to find property on scriptable object
            if (compositionProfile.objectReferenceValue)
            {
                compositionProfileSO = new SerializedObject(compositionProfile.objectReferenceValue);
                {
                    shaderProperties = compositionProfileSO.FindProperty("m_ShaderProperties");
                }
            }
        }

        public void Update()
        {
            compositionProfileSO?.Update();
            compositorSO.Update();
        }

        public void ApplyModifiedProperties()
        {
            compositionProfileSO?.ApplyModifiedProperties();
            compositorSO.ApplyModifiedProperties();
        }
    }
}
