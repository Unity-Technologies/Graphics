using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Reflection;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(CustomPassVolume))]
    sealed class CustomPassVolumeEditor : Editor
    {
        ReorderableList         m_CustomPassList;
        string                  m_ListName;
        CustomPassVolume        m_Volume;
        MaterialEditor[]        m_MaterialEditors = new MaterialEditor[0];
        int                     m_CustomPassMaterialsHash;

        const string            k_DefaultListName = "Custom Passes";

        static class Styles
        {
            public static readonly GUIContent isGlobal = new GUIContent("Mode", "A global volume is applied to the whole scene.");
            public static readonly GUIContent fadeRadius = new GUIContent("Fade Radius", "Radius from where your effect will be rendered, the _FadeValue in shaders will be updated using this radius");
            public static readonly GUIContent injectionPoint = new GUIContent("Injection Point", "Where the pass is going to be executed in the pipeline.");
            public static readonly GUIContent priority = new GUIContent("Priority", "Determine the execution order when multiple Custom Pass Volumes overlap with the same injection point.");
            public static readonly GUIContent[] modes = { new GUIContent("Global"), new GUIContent("Local") };
        }

        class SerializedPassVolume
        {
            public SerializedProperty   isGlobal;
            public SerializedProperty   fadeRadius;
            public SerializedProperty   customPasses;
            public SerializedProperty   injectionPoint;
            public SerializedProperty   priority;
        }


        SerializedPassVolume    m_SerializedPassVolume;

        void OnEnable()
        {
            m_Volume = target as CustomPassVolume;

            using (var o = new PropertyFetcher<CustomPassVolume>(serializedObject))
            {
                m_SerializedPassVolume = new SerializedPassVolume
                {
                    isGlobal = o.Find(x => x.isGlobal),
                    injectionPoint = o.Find(x => x.injectionPoint),
                    customPasses = o.Find(x => x.customPasses),
                    fadeRadius = o.Find(x => x.fadeRadius),
                    priority = o.Find(x => x.priority),
                };
            }

            CreateReorderableList(m_SerializedPassVolume.customPasses);

            UpdateMaterialEditors();
        }

        public override void OnInspectorGUI()
        {
            DrawSettingsGUI();
            DrawCustomPassReorderableList();
            DrawMaterialsGUI();
        }

        List<Material> GatherCustomPassesMaterials()
            => m_Volume.customPasses.Where(p => p != null).SelectMany(p => p.RegisterMaterialForInspector()).Where(m => m != null).ToList();

        void UpdateMaterialEditors()
        {
            var materials = GatherCustomPassesMaterials();

            // Destroy all material editors:
            foreach (var materialEditor in m_MaterialEditors)
                CoreUtils.Destroy(materialEditor);

            m_MaterialEditors = new MaterialEditor[materials.Count];
            for (int i = 0; i < materials.Count; i++)
                m_MaterialEditors[i] = CreateEditor(materials[i], typeof(MaterialEditor)) as MaterialEditor;
        }

        void DrawMaterialsGUI()
        {
            int materialsHash = GatherCustomPassesMaterials().Aggregate(0, (c, m) => c += m.GetHashCode());

            if (materialsHash != m_CustomPassMaterialsHash)
                UpdateMaterialEditors();

            // Draw the material inspectors:
            foreach (var materialEditor in m_MaterialEditors)
            {
                materialEditor.DrawHeader();
                materialEditor.OnInspectorGUI();
            }

            m_CustomPassMaterialsHash = materialsHash;
        }

        Dictionary<CustomPass, CustomPassDrawer> customPassDrawers = new Dictionary<CustomPass, CustomPassDrawer>();
        CustomPassDrawer GetCustomPassDrawer(SerializedProperty pass, CustomPass reference, int listIndex)
        {
            CustomPassDrawer drawer;

            if (customPassDrawers.TryGetValue(reference, out drawer))
                return drawer;

            var customPass = m_Volume.customPasses[listIndex];

            if (customPass == null)
                return null;

            foreach (var drawerType in TypeCache.GetTypesWithAttribute(typeof(CustomPassDrawerAttribute)))
            {
                var attr = drawerType.GetCustomAttributes(typeof(CustomPassDrawerAttribute), true)[0] as CustomPassDrawerAttribute;
                if (attr.targetPassType == customPass.GetType())
                {
                    drawer = Activator.CreateInstance(drawerType) as CustomPassDrawer;
                    drawer.SetPass(customPass);
                    break;
                }
                if (attr.targetPassType.IsAssignableFrom(customPass.GetType()))
                {
                    drawer = Activator.CreateInstance(drawerType) as CustomPassDrawer;
                    drawer.SetPass(customPass);
                }
            }

            customPassDrawers[reference] = drawer;

            return drawer;
        }

        void DrawSettingsGUI()
        {
            serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();
            {
                Rect isGlobalRect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(isGlobalRect, Styles.isGlobal, m_SerializedPassVolume.isGlobal);
                {
                    m_SerializedPassVolume.isGlobal.boolValue = EditorGUI.Popup(isGlobalRect, Styles.isGlobal, m_SerializedPassVolume.isGlobal.boolValue ? 0 : 1, Styles.modes) == 0;
                }
                EditorGUI.EndProperty();
                EditorGUILayout.PropertyField(m_SerializedPassVolume.injectionPoint, Styles.injectionPoint);
                EditorGUILayout.PropertyField(m_SerializedPassVolume.priority, Styles.priority);
                if (!m_SerializedPassVolume.isGlobal.boolValue)
                    EditorGUILayout.PropertyField(m_SerializedPassVolume.fadeRadius, Styles.fadeRadius);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        void DrawCustomPassReorderableList()
        {
            // Sanitize list:
            for (int i = 0; i < m_SerializedPassVolume.customPasses.arraySize; i++)
            {
                if (m_SerializedPassVolume.customPasses.GetArrayElementAtIndex(i) == null)
                {
                    m_SerializedPassVolume.customPasses.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    i++;
                }
            }

            float customPassListHeight =  m_CustomPassList.GetHeight();
            var customPassRect = EditorGUILayout.GetControlRect(false, customPassListHeight);
            EditorGUI.BeginProperty(customPassRect, GUIContent.none, m_SerializedPassVolume.customPasses);
            {
                EditorGUILayout.BeginVertical();
                m_CustomPassList.DoList(customPassRect);
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndProperty();
        }

        void CreateReorderableList(SerializedProperty passList)
        {
            m_CustomPassList = new ReorderableList(passList.serializedObject, passList);

            m_CustomPassList.drawHeaderCallback = (rect) => {
                EditorGUI.LabelField(rect, k_DefaultListName, EditorStyles.largeLabel);
            };

            m_CustomPassList.drawElementCallback = (rect, index, active, focused) => {
                EditorGUI.BeginChangeCheck();
                
                passList.serializedObject.ApplyModifiedProperties();
                var customPass = passList.GetArrayElementAtIndex(index);
                var drawer = GetCustomPassDrawer(customPass, m_Volume.customPasses[index], index);
                if (drawer != null)
                    drawer.OnGUI(rect, customPass, null);
                else
                    EditorGUI.PropertyField(rect, customPass);
                if (EditorGUI.EndChangeCheck())
                    customPass.serializedObject.ApplyModifiedProperties();
            };

            m_CustomPassList.elementHeightCallback = (index) =>
            {
                passList.serializedObject.ApplyModifiedProperties();
                var customPass = passList.GetArrayElementAtIndex(index);
                var drawer = GetCustomPassDrawer(customPass, m_Volume.customPasses[index], index);
                if (drawer != null)
                    return drawer.GetPropertyHeight(customPass, null);
                else
                    return EditorGUI.GetPropertyHeight(customPass, null);
            };

            m_CustomPassList.onAddCallback += (list) => {
                Undo.RegisterCompleteObjectUndo(target, "Add custom pass");

                var menu = new GenericMenu();
                foreach (var customPassType in TypeCache.GetTypesDerivedFrom<CustomPass>())
                {
                    if (customPassType.IsAbstract)
                        continue;

                    menu.AddItem(new GUIContent(customPassType.Name), false, () => {
                        passList.serializedObject.ApplyModifiedProperties();
                        m_Volume.AddPassOfType(customPassType);
                        UpdateMaterialEditors();
                        passList.serializedObject.Update();
                        // Notify the prefab that something have changed:
                        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                   });
                }
                menu.ShowAsContext();
            };

            m_CustomPassList.onReorderCallback = (index) => ClearCustomPassCache();

            m_CustomPassList.onRemoveCallback = (list) =>
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                ClearCustomPassCache();
            };
        }

        void ClearCustomPassCache()
        {
            // When custom passes are re-ordered, a topological change happens in the SerializedProperties
            // So we have to rebuild all the drawers that were storing SerializedProperties.
            customPassDrawers.Clear();
        }

        float GetCustomPassEditorHeight(SerializedProperty pass) => EditorGUIUtility.singleLineHeight;
    }
}