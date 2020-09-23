using UnityEngine;
using UnityEngine.VFX.Utility;
namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventUnityEvent))]
    class VFXOutputEventUnityEventEditor : VFXOutputEventHandlerEditor
    {
        SerializedProperty onEvent;

        protected override void OnEnable()
        {
            base.OnEnable();
            onEvent = serializedObject.FindProperty("onEvent");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(onEvent);

            // Help box
            HelpBox("Attribute Usage","VFX Attributes are not used for this Output Event Handler");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

        }
    }
}