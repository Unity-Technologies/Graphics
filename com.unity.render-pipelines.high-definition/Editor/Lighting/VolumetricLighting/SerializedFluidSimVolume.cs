using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedFluidSimVolume
    {
        public SerializedProperty fluidSimParams;
        public SerializedProperty initialStateTexture;
        public SerializedProperty initialVectorField;
        public SerializedProperty vectorFieldSpeed;
        public SerializedProperty numVectorFields;
        public SerializedProperty loopTime;

        public SerializedProperty size;

        SerializedProperty positiveFade;
        SerializedProperty negativeFade;
        public SerializedProperty editorPositiveFade;
        public SerializedProperty editorNegativeFade;
        public SerializedProperty editorUniformFade;
        public SerializedProperty editorAdvancedFade;

        public SerializedProperty distanceFadeStart;
        public SerializedProperty distanceFadeEnd;

        SerializedObject m_SerializedObject;

        public SerializedFluidSimVolume(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            fluidSimParams = m_SerializedObject.FindProperty("parameters");
            initialStateTexture = fluidSimParams.FindPropertyRelative("initialStateTexture");
            initialVectorField = fluidSimParams.FindPropertyRelative("initialVectorField");
            vectorFieldSpeed = fluidSimParams.FindPropertyRelative("vectorFieldSpeed");
            numVectorFields = fluidSimParams.FindPropertyRelative("numVectorFields");
            loopTime = fluidSimParams.FindPropertyRelative("loopTime");

            size = fluidSimParams.FindPropertyRelative("size");

            positiveFade = fluidSimParams.FindPropertyRelative("positiveFade");
            negativeFade = fluidSimParams.FindPropertyRelative("negativeFade");

            editorPositiveFade = fluidSimParams.FindPropertyRelative("m_EditorPositiveFade");
            editorNegativeFade = fluidSimParams.FindPropertyRelative("m_EditorNegativeFade");
            editorUniformFade = fluidSimParams.FindPropertyRelative("m_EditorUniformFade");
            editorAdvancedFade = fluidSimParams.FindPropertyRelative("m_EditorAdvancedFade");

            distanceFadeStart = fluidSimParams.FindPropertyRelative("distanceFadeStart");
            distanceFadeEnd   = fluidSimParams.FindPropertyRelative("distanceFadeEnd");
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
