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

            // Help box
            HelpBox("Help", @"Spawns prefab from a managed pool of prefabs of given instance count. Event attributes can be caught in prefabs by using VFXOutputEventPrefabAttributeHandler scripts in the prefab.

Attribute Usage:
 - position : spawns prefab at given position
 - angle : spawns prefab at given angle
 - scale : spawns prefab at given scale
 - lifetime : destroys prefab after given lifetime
");

            if (EditorGUI.EndChangeCheck())
            {
                if(m_PrefabToSpawn.objectReferenceValue != null)
                {
                    GameObject prefab = m_PrefabToSpawn.objectReferenceValue as GameObject;
                    GameObject self = m_PrefabSpawnHandler.gameObject;

                    while(self != null)
                    {
                        if(self.transform == prefab.transform)
                        {
                            m_PrefabToSpawn.objectReferenceValue = null;
                        }

                        if (self.transform.parent != null)
                            self = self.transform.parent.gameObject;
                        else
                            self = null;
                    }
                }

                serializedObject.ApplyModifiedProperties();
                m_PrefabSpawnHandler.ReloadPrefab();
            }

        }
    }
}