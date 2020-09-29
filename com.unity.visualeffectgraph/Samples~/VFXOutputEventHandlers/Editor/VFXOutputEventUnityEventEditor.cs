using UnityEngine;
using UnityEngine.VFX.Utility;
namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventUnityEvent))]
    class VFXOutputEventUnityEventEditor : VFXOutputEventHandlerEditor
    {
        SerializedProperty m_OnEvent;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_OnEvent = serializedObject.FindProperty(nameof(VFXOutputEventUnityEvent.onEvent));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            DrawOutputEventProperties();
            EditorGUILayout.PropertyField(m_OnEvent);
            HelpBox("Attribute Usage", "VFX Attributes are not used for this Output Event Handler");
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
