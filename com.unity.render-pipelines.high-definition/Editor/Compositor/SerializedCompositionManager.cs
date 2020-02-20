using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class SerializedCompositionManager
    {
        public SerializedProperty CompositionProfile;
        public SerializedProperty LayerList;
        public SerializedProperty ShaderProperties;
        public SerializedProperty DisplayNumber;
        public SerializedProperty CompositionShader;
        public SerializedProperty OutputCamera;

        public SerializedObject CompositionProfileSO;
        public SerializedObject CompositorSO;

        public SerializedCompositionManager(SerializedObject root)
        {
            CompositorSO = root;
            {
                CompositionProfile = CompositorSO.FindProperty("m_CompositionProfile");
                DisplayNumber = CompositorSO.FindProperty("m_OutputDisplay");
                CompositionShader = CompositorSO.FindProperty("m_Shader");
                OutputCamera = CompositorSO.FindProperty("m_OutputCamera");
            }

            LayerList = CompositorSO.FindProperty("m_InputLayers");

            // Work around to find property on scriptable object
            if (CompositionProfile.objectReferenceValue)
            {
                CompositionProfileSO = new SerializedObject(CompositionProfile.objectReferenceValue);
                {
                    ShaderProperties = CompositionProfileSO.FindProperty("m_ShaderProperties");
                }
            }
        }

        public void Update()
        {
            CompositionProfileSO.Update();
            CompositorSO.Update();
        }

        public void ApplyModifiedProperties()
        {
            CompositionProfileSO.ApplyModifiedProperties();
            CompositorSO.ApplyModifiedProperties();
        }
    }
}
