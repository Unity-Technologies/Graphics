#if VFX_OUTPUTEVENT_AUDIO
using UnityEngine;
using UnityEngine.VFX.Utility;
namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventPlayAudio))]
    class VFXOutputEventPlayAudioEditor : VFXOutputEventHandlerEditor
    {
        VFXOutputEventPlayAudio m_PlayAudioHandler;

        SerializedProperty m_AudioSource;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_PlayAudioHandler = serializedObject.targetObject as VFXOutputEventPlayAudio;

            m_AudioSource = serializedObject.FindProperty("audioSource");

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_AudioSource);

            // Help box
            HelpBox("Attribute Usage", "VFX Attributes are not used for this Output Event Handler");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

        }
    }
}
#endif