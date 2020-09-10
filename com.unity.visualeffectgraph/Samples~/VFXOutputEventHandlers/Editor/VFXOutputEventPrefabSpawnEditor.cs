using UnityEngine;
using UnityEngine.VFX.Utility;

namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventPrefabSpawn))]
    public class VFXOutputEventPrefabSpawnEditor : VFXOutputEventHandlerEditor
    {
        VFXOutputEventPrefabSpawn m_PrefabSpawnHandler;

        SerializedProperty m_InstanceCount;
        SerializedProperty m_PrefabToSpawn;
        SerializedProperty m_ParentInstances;
        SerializedProperty usePosition;
        SerializedProperty useAngle;
        SerializedProperty useScale;
        SerializedProperty useLifetime;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_PrefabSpawnHandler = serializedObject.targetObject as VFXOutputEventPrefabSpawn;

            m_InstanceCount = serializedObject.FindProperty("m_InstanceCount");
            m_PrefabToSpawn = serializedObject.FindProperty("m_PrefabToSpawn");
            m_ParentInstances = serializedObject.FindProperty("m_ParentInstances");
            usePosition = serializedObject.FindProperty("usePosition");
            useAngle = serializedObject.FindProperty("useAngle");
            useScale = serializedObject.FindProperty("useScale");
            useLifetime = serializedObject.FindProperty("useLifetime");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (m_ExecuteInEditor.boolValue)
            {
                EditorGUILayout.HelpBox($"While previewing Prefab Spawn in editor, some Attribute Handlers attached to prefabs cannot not be executed unless you are running in Play Mode.", MessageType.Info);
            }

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Prefab Instances", EditorStyles.boldLabel);

            using(new EditorGUI.IndentLevelScope(1))
            {
                using(new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(m_PrefabToSpawn);
                    using(new EditorGUI.DisabledGroupScope(m_PrefabToSpawn.objectReferenceValue == null))
                    {
                        if (GUILayout.Button("Reload", EditorStyles.miniButton, GUILayout.Width(64)))
                        {
                            m_PrefabSpawnHandler.ReloadPrefab();
                        }
                    }
                }
                EditorGUILayout.PropertyField(m_InstanceCount);
                EditorGUILayout.PropertyField(m_ParentInstances);
            }

            EditorGUILayout.LabelField("Event Attribute Usage", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(usePosition);
                EditorGUILayout.PropertyField(useAngle);
                EditorGUILayout.PropertyField(useScale);
                EditorGUILayout.PropertyField(useLifetime);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                m_PrefabSpawnHandler.ReloadPrefab();
            }

        }
    }
}