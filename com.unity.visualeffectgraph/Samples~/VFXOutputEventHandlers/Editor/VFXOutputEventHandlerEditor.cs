using UnityEngine;
using UnityEngine.VFX.Utility;

namespace UnityEditor.VFX.Utility
{
    public abstract class VFXOutputEventHandlerEditor : Editor
    {
        protected SerializedProperty m_ExecuteInEditor;
        protected VFXOutputEventHandler m_TargetHandler;
        protected virtual void OnEnable()
        {
            m_TargetHandler = serializedObject.targetObject as VFXOutputEventHandler;
            m_ExecuteInEditor = serializedObject.FindProperty("executeInEditor");
        }

        public override void OnInspectorGUI()
        {
            if(m_TargetHandler.canExecuteInEditor)
            {
                serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_ExecuteInEditor);
                if(EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
