using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

namespace Unity.UI.Shaders.Sample.Editor
{
    [CustomEditor(typeof(CustomToggle))]
    public class CustomToggleEditor : ToggleEditor
    {
        SerializedProperty m_OnStateChangedProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_OnStateChangedProperty = serializedObject.FindProperty("onStateChanged");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_OnStateChangedProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
