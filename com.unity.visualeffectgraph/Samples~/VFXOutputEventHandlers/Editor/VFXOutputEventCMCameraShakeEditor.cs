#if VFX_OUTPUTEVENT_CINEMACHINE_2_6_0_OR_NEWER
using UnityEngine;
using UnityEngine.VFX.Utility;
namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventCMCameraShake))]
    class VFXOutputEventCMCameraShakeEditor : VFXOutputEventHandlerEditor
    {
        VFXOutputEventCMCameraShake m_RigidbodyEventHandler;

        SerializedProperty cinemachineImpulseSource;
        SerializedProperty attributeSpace;

        protected override void OnEnable()
        {
            base.OnEnable();
            cinemachineImpulseSource = serializedObject.FindProperty("cinemachineImpulseSource");
            attributeSpace = serializedObject.FindProperty("attributeSpace");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(cinemachineImpulseSource);
            EditorGUILayout.PropertyField(attributeSpace);

            // Help box
            HelpBox("Attribute Usage", "- position : position of the camera impulse\n - velocity : impulse velocity");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

        }
    }
}
#endif