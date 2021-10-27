using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Reflection;
using System.Linq;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(CustomPassVolume)), CanEditMultipleObjects]
    sealed class CustomPassVolumeEditor : Editor
    {
        ReorderableList m_CustomPassList;
        string m_ListName;
        CustomPassVolume m_Volume;
        MaterialEditor[] m_MaterialEditors = new MaterialEditor[0];
        int m_CustomPassMaterialsHash;
        bool m_SupportListMultiEditing;

        const string k_DefaultListName = "Custom Passes";

        static class Styles
        {
            public static readonly GUIContent isGlobal = new GUIContent("Mode", "A global volume is applied to the whole scene. A local volume is applied only when the camera position is inside the volume. A camera volume is applied only for the target camera.");
            public static readonly GUIContent fadeRadius = new GUIContent("Fade Radius", "Radius from where your effect will be rendered, the _FadeValue in shaders will be updated using this radius");
            public static readonly GUIContent injectionPoint = new GUIContent("Injection Point", "Where the pass is going to be executed in the pipeline.");
            public static readonly GUIContent priority = new GUIContent("Priority", "Determine the execution order when multiple Custom Pass Volumes overlap with the same injection point.");
            public static readonly GUIContent targetCamera = new GUIContent("Target Camera", "Determine on which camera this custom pass volume will be applied. If this property is null and the mode is set to camera, the volume is ignored.");
            public static readonly GUIContent[] modes = { new GUIContent("Global"), new GUIContent("Local"), new GUIContent("Camera") };
        }

        class SerializedPassVolume
        {
            public SerializedProperty isGlobal;
            public SerializedProperty useTargetCamera;
            public SerializedProperty targetCamera;
            public SerializedProperty fadeRadius;
            public SerializedProperty customPasses;
            public SerializedProperty injectionPoint;
            public SerializedProperty priority;
        }


        SerializedPassVolume m_SerializedPassVolume;

        void OnEnable()
        {
            m_Volume = target as CustomPassVolume;

            using (var o = new PropertyFetcher<CustomPassVolume>(serializedObject))
            {
                m_SerializedPassVolume = new SerializedPassVolume
                {
                    isGlobal = o.Find(x => x.isGlobal),
                    useTargetCamera = o.Find(x => x.useTargetCamera),
                    targetCamera = o.Find(x => x.m_TargetCamera),
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

            int GetMode() => m_SerializedPassVolume.useTargetCamera.boolValue ? 2 : (m_SerializedPassVolume.isGlobal.boolValue ? 0 : 1);
            void SetMode(int value)
            {
                m_SerializedPassVolume.isGlobal.boolValue = value == 0;
                m_SerializedPassVolume.useTargetCamera.boolValue = value == 2;
            }

            EditorGUI.BeginChangeCheck();
            {
                Rect isGlobalRect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(isGlobalRect, Styles.isGlobal, m_SerializedPassVolume.isGlobal);
                {
                    EditorGUI.BeginChangeCheck();
                    int selectedMode = EditorGUI.Popup(isGlobalRect, Styles.isGlobal, GetMode(), Styles.modes);
                    if (EditorGUI.EndChangeCheck())
                        SetMode(selectedMode);
                }
                EditorGUI.EndProperty();

                if (m_SerializedPassVolume.useTargetCamera.boolValue)
                {
                    EditorGUILayout.PropertyField(m_SerializedPassVolume.targetCamera, Styles.targetCamera);
                    EditorGUILayout.PropertyField(m_SerializedPassVolume.injectionPoint, Styles.injectionPoint);
                }
                else
                {
                    EditorGUILayout.PropertyField(m_SerializedPassVolume.injectionPoint, Styles.injectionPoint);
                    EditorGUILayout.PropertyField(m_SerializedPassVolume.priority, Styles.priority);
                    if (!m_SerializedPassVolume.isGlobal.boolValue)
                        EditorGUILayout.PropertyField(m_SerializedPassVolume.fadeRadius, Styles.fadeRadius);
                }
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        void DrawCustomPassReorderableList()
        {
            if (targets.OfType<CustomPassVolume>().Count() > 1)
            {
                EditorGUILayout.HelpBox("Custom Pass List UI is not supported with multi-selection", MessageType.Warning, true);
                return;
            }

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

            float customPassListHeight = m_CustomPassList.GetHeight();
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

            m_CustomPassList.drawHeaderCallback = (rect) =>
            {
                EditorGUI.LabelField(rect, k_DefaultListName, EditorStyles.largeLabel);
            };

            m_CustomPassList.multiSelect = false;
            m_CustomPassList.drawElementCallback = (rect, index, active, focused) =>
            {
                EditorGUI.BeginChangeCheck();

                passList.serializedObject.ApplyModifiedProperties();
                var customPass = passList.GetArrayElementAtIndex(index);
                customPass.managedReferenceValue = m_Volume.customPasses[index];
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

            m_CustomPassList.onAddCallback += (list) =>
            {
                var searchObject = ScriptableObject.CreateInstance<CustomPassListSearchWindow>();
                searchObject.Initialize(AddCustomPass);
                var windowPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                SearchWindow.Open(new SearchWindowContext(windowPosition), searchObject);
            };

            m_CustomPassList.onReorderCallback = (index) => ClearCustomPassCache();

            m_CustomPassList.onRemoveCallback = (list) =>
            {
                foreach (int index in list.selectedIndices)
                    passList.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                ClearCustomPassCache();
            };

            void AddCustomPass(Type customPassType)
            {
                foreach (CustomPassVolume volume in targets)
                {
                    Undo.RegisterCompleteObjectUndo(volume, "Add custom pass");

                    passList.serializedObject.ApplyModifiedProperties();
                    volume.AddPassOfType(customPassType);
                    UpdateMaterialEditors();
                    passList.serializedObject.Update();
                    // Notify the prefab that something have changed:
                    PrefabUtility.RecordPrefabInstancePropertyModifications(volume);
                }
            }
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
