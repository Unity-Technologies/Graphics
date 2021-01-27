using UnityEngine;
using UnityEngine.VFX.Utility;

namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventPrefabSpawn))]
    class VFXOutputEventPrefabSpawnEditor : VFXOutputEventHandlerEditor
    {
        VFXOutputEventPrefabSpawn m_PrefabSpawnHandler;

        SerializedProperty m_InstanceCount;
        SerializedProperty m_PrefabToSpawn;
        SerializedProperty m_ParentInstances;
        SerializedProperty m_UsePosition;
        SerializedProperty m_UseAngle;
        SerializedProperty m_UseScale;
        SerializedProperty m_UseLifetime;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_PrefabSpawnHandler = serializedObject.targetObject as VFXOutputEventPrefabSpawn;

            m_InstanceCount = serializedObject.FindProperty("m_InstanceCount");
            m_PrefabToSpawn = serializedObject.FindProperty("m_PrefabToSpawn");
            m_ParentInstances = serializedObject.FindProperty("m_ParentInstances");
            m_UsePosition = serializedObject.FindProperty(nameof(VFXOutputEventPrefabSpawn.usePosition));
            m_UseAngle = serializedObject.FindProperty(nameof(VFXOutputEventPrefabSpawn.useAngle));
            m_UseScale = serializedObject.FindProperty(nameof(VFXOutputEventPrefabSpawn.useScale));
            m_UseLifetime = serializedObject.FindProperty(nameof(VFXOutputEventPrefabSpawn.useLifetime));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawOutputEventProperties();

            if (m_ExecuteInEditor.boolValue)
                EditorGUILayout.HelpBox($"While previewing Prefab Spawn in editor, some Attribute Handlers attached to prefabs cannot not be executed unless you are running in Play Mode.", MessageType.Info);

            EditorGUILayout.LabelField("Prefab Instances", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(m_PrefabToSpawn);
                EditorGUILayout.PropertyField(m_InstanceCount);
                EditorGUILayout.PropertyField(m_ParentInstances);
            }

            EditorGUILayout.LabelField("Event Attribute Usage", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(m_UsePosition);
                EditorGUILayout.PropertyField(m_UseAngle);
                EditorGUILayout.PropertyField(m_UseScale);
                EditorGUILayout.PropertyField(m_UseLifetime);
            }

            HelpBox("Help", @"Spawns prefab from a managed pool of prefabs of given instance count. Event attributes can be caught in prefabs by using VFXOutputEventPrefabAttributeHandler scripts in the prefab.

Attribute Usage:
 - position : spawns prefab at given position
 - angle : spawns prefab at given angle
 - scale : spawns prefab at given scale
 - lifetime : destroys prefab after given lifetime
");

            if (EditorGUI.EndChangeCheck())
            {
                //m_PrefabToSpawn can't be a children of m_PrefabSpawnHandler.
                //Avoid infinite hierarchy recursion.
                if (m_PrefabToSpawn.objectReferenceValue != null)
                {
                    var prefab = m_PrefabToSpawn.objectReferenceValue as GameObject;
                    var self = m_PrefabSpawnHandler.gameObject;

                    if (prefab.transform.IsChildOf(self.transform))
                        m_PrefabToSpawn.objectReferenceValue = null;
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
