using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedDensityVolume
    {
        public SerializedProperty densityParams;
        public SerializedProperty albedo;
        public SerializedProperty meanFreePath;

        public SerializedProperty volumeTexture;
        public SerializedProperty textureScroll;
        public SerializedProperty textureTile;

        public SerializedProperty size;

        SerializedProperty positiveFade;
        SerializedProperty negativeFade;
        public SerializedProperty editorPositiveFade;
        public SerializedProperty editorNegativeFade;
        public SerializedProperty editorUniformFade;
        public SerializedProperty editorAdvancedFade;
        public SerializedProperty invertFade;

        public SerializedProperty distanceFadeStart;
        public SerializedProperty distanceFadeEnd;

        SerializedObject m_SerializedObject;

        public SerializedDensityVolume(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            densityParams = m_SerializedObject.FindProperty("parameters");

            albedo = densityParams.FindPropertyRelative("albedo");
            meanFreePath = densityParams.FindPropertyRelative("meanFreePath");

            volumeTexture = densityParams.FindPropertyRelative("volumeMask");
            textureScroll = densityParams.FindPropertyRelative("textureScrollingSpeed");
            textureTile = densityParams.FindPropertyRelative("textureTiling");

            size = densityParams.FindPropertyRelative("size");

            positiveFade = densityParams.FindPropertyRelative("positiveFade");
            negativeFade = densityParams.FindPropertyRelative("negativeFade");

            editorPositiveFade = densityParams.FindPropertyRelative("m_EditorPositiveFade");
            editorNegativeFade = densityParams.FindPropertyRelative("m_EditorNegativeFade");
            editorUniformFade = densityParams.FindPropertyRelative("m_EditorUniformFade");
            editorAdvancedFade = densityParams.FindPropertyRelative("m_EditorAdvancedFade");

            invertFade = densityParams.FindPropertyRelative("invertFade");

            distanceFadeStart = densityParams.FindPropertyRelative("distanceFadeStart");
            distanceFadeEnd   = densityParams.FindPropertyRelative("distanceFadeEnd");
        }

        public void Apply()
        {
            if (editorAdvancedFade.boolValue)
            {
                positiveFade.vector3Value = editorPositiveFade.vector3Value;
                negativeFade.vector3Value = editorNegativeFade.vector3Value;
            }
            else
            {
                positiveFade.vector3Value = negativeFade.vector3Value = new Vector3(
                    size.vector3Value.x > 0.00001 ? 1f - ((size.vector3Value.x - editorUniformFade.floatValue) / size.vector3Value.x) : 0f,
                    size.vector3Value.y > 0.00001 ? 1f - ((size.vector3Value.y - editorUniformFade.floatValue) / size.vector3Value.y) : 0f,
                    size.vector3Value.z > 0.00001 ? 1f - ((size.vector3Value.z - editorUniformFade.floatValue) / size.vector3Value.z) : 0f
                );
            }
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
