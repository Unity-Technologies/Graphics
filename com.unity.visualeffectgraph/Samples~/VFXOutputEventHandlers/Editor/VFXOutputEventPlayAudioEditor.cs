#if VFX_OUTPUTEVENT_AUDIO
using UnityEngine;
using UnityEngine.VFX.Utility;
namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventPlayAudio))]
    class VFXOutputEventPlayAudioEditor : VFXOutputEventHandlerEditor
    {
        SerializedProperty m_AudioSource;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_AudioSource = serializedObject.FindProperty(nameof(VFXOutputEventPlayAudio.audioSource));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_AudioSource);
            HelpBox("Attribute Usage", "VFX Attributes are not used for this Output Event Handler");

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
