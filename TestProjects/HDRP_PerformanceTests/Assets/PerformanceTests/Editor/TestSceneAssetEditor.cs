using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.SceneManagement;
using UnityEditor.Compilation;
using System;
using System.Linq;

[CustomEditor(typeof(TestSceneAsset))]
class TestSceneAssetEditor : Editor
{
    ReorderableList counterSceneList;
    ReorderableList counterHDAssets;
    ReorderableList memorySceneList;
    ReorderableList memoryHDAssets;
    ReorderableList buildSceneList;
    ReorderableList buildHDAssets;

    public void OnEnable()
    {
        counterSceneList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(TestSceneAsset.performanceCounterScenes)));
        counterHDAssets = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(TestSceneAsset.performanceCounterHDAssets)));
        InitSceneDataReorderableList(counterSceneList, "Scenes");
        InitHDAssetReorderableList(counterHDAssets, "HDRP Assets, keep to none for default HDRP asset");
        
        memorySceneList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(TestSceneAsset.memoryTestScenes)));
        memoryHDAssets = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(TestSceneAsset.memoryTestHDAssets)));
        InitSceneDataReorderableList(memorySceneList, "Scenes");
        InitHDAssetReorderableList(memoryHDAssets, "HDRP Assets, keep to none for default HDRP asset");
    
        buildSceneList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(TestSceneAsset.buildTestScenes)));
        buildHDAssets = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(TestSceneAsset.buildHDAssets)));
        InitSceneDataReorderableList(buildSceneList, "Scenes");
        InitHDAssetReorderableList(buildHDAssets, "HDRP Assets, keep to none for default HDRP asset");
    }

    void InitSceneDataReorderableList(ReorderableList list, string title)
    {
        list.drawHeaderCallback = (r) => EditorGUI.LabelField(r, title, EditorStyles.boldLabel);

        list.drawElementCallback = (rect, index, isActive, isFocused) => {
            EditorGUI.BeginChangeCheck();
            var elem = list.serializedProperty.GetArrayElementAtIndex(index);
            var sceneName = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.scene));
            var scenePath = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.scenePath));
            var enabled = elem.FindPropertyRelative(nameof(TestSceneAsset.SceneData.enabled));
            rect.height = EditorGUIUtility.singleLineHeight;

            // Scene field
            var sceneGUID = AssetDatabase.FindAssets($"t:Scene {sceneName.stringValue}", new [] {"Assets"}).FirstOrDefault();
            var sceneAsset = String.IsNullOrEmpty(sceneGUID) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneGUID));
            sceneAsset = EditorGUI.ObjectField(rect, "Test Scene", sceneAsset, typeof(SceneAsset), false) as SceneAsset;
            sceneName.stringValue = sceneAsset?.name;
            scenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);

            // Enabled field
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, enabled);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        };

        list.elementHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;

        list.onAddCallback = DefaultListAdd;
        list.onRemoveCallback = DefaultListDelete;
    }

    void InitHDAssetReorderableList(ReorderableList list, string title)
    {
        list.drawHeaderCallback = (r) => EditorGUI.LabelField(r, title, EditorStyles.boldLabel);

        list.drawElementCallback = (rect, index, isActive, isFocused) => {
            EditorGUI.BeginChangeCheck();
            var hdrpAsset = list.serializedProperty.GetArrayElementAtIndex(index);

            EditorGUI.PropertyField(rect, hdrpAsset, new GUIContent("HDRP Asset"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        };
        list.onAddCallback = DefaultListAdd;
        list.onRemoveCallback = DefaultListDelete;
    }

    void DefaultListAdd(ReorderableList list)
    {
        ReorderableList.defaultBehaviours.DoAddButton(list);
        serializedObject.ApplyModifiedProperties();
    }

    void DefaultListDelete(ReorderableList list)
    {
        list.serializedProperty.DeleteArrayElementAtIndex(list.index);
        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        if (Resources.Load(PerformanceTestUtils.testSceneResourcePath) == null)
            EditorGUILayout.HelpBox($"Test Scene Asset have been moved from it's expected location, please move it back to Resources/{PerformanceTestUtils.testSceneResourcePath}", MessageType.Error);

        EditorGUIUtility.labelWidth = 100;

        DrawTestBlock(counterSceneList, counterHDAssets, "Performance Counters Tests");
        DrawTestBlock(memorySceneList, memoryHDAssets, "Memory Tests");
        DrawTestBlock(buildSceneList, buildHDAssets, "Build Time Tests");

        EditorGUILayout.Space();

        if (GUILayout.Button("Refresh Test Runner List (can take up to ~20s)"))
            CompilationPipeline.RequestScriptCompilation();
    }

    void DrawTestBlock(ReorderableList sceneList, ReorderableList hdrpAssetList, string title)
    {
        var boxStyle = new GUIStyle("Window");
        boxStyle.fontStyle = FontStyle.Bold;
        boxStyle.fontSize = 15;
        boxStyle.margin = new RectOffset(0, 0, 20, 10);

        GUILayout.BeginHorizontal(title, boxStyle);
        {
            GUILayout.BeginVertical();
            EditorGUILayout.Space();
            sceneList.DoLayoutList();
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            EditorGUILayout.Space();
            hdrpAssetList.DoLayoutList();
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }
}
